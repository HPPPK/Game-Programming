/*
 * File: TurnManager.cs
 *
 * Purpose:
 * This script controls the current single-player sandbox turn state. It gives
 * the player a small AP budget each turn and limits basic card actions so the
 * project has a playable planning -> enemy wave -> next turn loop.
 *
 * Current rules:
 * - There is only one human player.
 * - Each turn starts with maxAP.
 * - Drawing a card costs 1 AP immediately after the draw succeeds.
 * - Playing a card costs 1 AP only after the card action is confirmed.
 * - Canceling a targeting card restores the card and does not spend AP.
 * - EndTurn() can trigger the current WaveManager, waits for the wave to finish,
 *   then starts the next turn.
 *
 * Inspector setup:
 * - maxAP controls how much AP each turn starts with.
 * - waveManager is optional. If assigned, EndTurn() starts a wave.
 */
using UnityEngine;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    [Header("Action Points")]
    public int maxAP = 2;
    public int currentAP;

    [Header("Turn Limits")]
    public bool hasDrawnThisTurn;
    public bool hasPlayedCardThisTurn;
    public bool hasEndedThisTurn;

    [Header("Optional Wave")]
    public WaveManager waveManager;

    private bool isResolvingTurn = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartTurn();
    }

    public void StartTurn()
    {
        currentAP = maxAP;
        hasDrawnThisTurn = false;
        hasPlayedCardThisTurn = false;
        hasEndedThisTurn = false;

        Debug.Log("Start Turn. AP = " + currentAP + " / " + maxAP);
    }

    public void EndTurn()
    {
        if (hasEndedThisTurn || isResolvingTurn)
        {
            Debug.Log("End Turn already used this turn.");
            return;
        }

        hasEndedThisTurn = true;
        isResolvingTurn = true;
        Debug.Log("End Turn.");

        StartCoroutine(EndTurnRoutine());
    }

    IEnumerator EndTurnRoutine()
    {
        if (waveManager != null)
        {
            waveManager.StartWave();

            while (waveManager.IsSpawning)
            {
                yield return null;
            }
        }

        isResolvingTurn = false;
        StartTurn();
    }

    public bool CanDrawCard()
    {
        return currentAP > 0 && !hasDrawnThisTurn && !hasEndedThisTurn && !isResolvingTurn;
    }

    public bool CanPlayCard()
    {
        return currentAP > 0 && !hasPlayedCardThisTurn && !hasEndedThisTurn && !isResolvingTurn;
    }

    public bool SpendAP(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentAP < amount)
        {
            Debug.LogWarning("Not enough AP. Current AP = " + currentAP + ", required AP = " + amount);
            return false;
        }

        currentAP -= amount;
        Debug.Log("Spent " + amount + " AP. AP = " + currentAP + " / " + maxAP);
        return true;
    }

    public void OnCardDrawn()
    {
        hasDrawnThisTurn = true;
    }

    public void OnCardConfirmed()
    {
        if (hasPlayedCardThisTurn)
        {
            return;
        }

        if (!SpendAP(1))
        {
            return;
        }

        hasPlayedCardThisTurn = true;
    }

    public void OnCardCanceled()
    {
        Debug.Log("Card action canceled. AP unchanged.");
    }
}
