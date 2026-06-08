/*
 * File: PresentTheNumberUI.cs
 *
 * Purpose:
 * Implements PresentTheNumberUI for the ui layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PresentTheNumberUI within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PresentTheNumberUI in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using TMPro;
using UnityEngine;

public class PresentTheNumberUI : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI cardText;

    [Header("Initial Values")]
    public int currentMoney = 0;
    public int currentCardCount = 0;

    private void Start()
    {
        RefreshUI();
    }

    public void SetMoney(int money)
    {
        currentMoney = money;
        RefreshUI();
    }

    public void SetCardCount(int cardCount)
    {
        currentCardCount = cardCount;
        RefreshUI();
    }

    public void SetNumbers(int money, int cardCount)
    {
        currentMoney = money;
        currentCardCount = cardCount;
        RefreshUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        RefreshUI();
    }

    public void AddCardCount(int amount)
    {
        currentCardCount += amount;

        if (currentCardCount < 0)
        {
            currentCardCount = 0;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (moneyText != null)
        {
            moneyText.text = currentMoney.ToString();
        }

        if (cardText != null)
        {
            cardText.text = currentCardCount.ToString();
        }
    }
}
