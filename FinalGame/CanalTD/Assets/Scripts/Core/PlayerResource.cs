/*
 * File: PlayerResource.cs
 *
 * Purpose:
 * Holds one player's gameplay resources: player ID, display name, gold, score,
 * card count, PlayerHand reference, and temporary status effects.
 *
 * Notes:
 * UI panels read from this component so player resource display stays tied to
 * the actual player/castle data.
 */
using System.Collections.Generic;
using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    [Header("Player")]
    public int playerId = 0;
    public string displayName = "Player";

    [Header("Resource")]
    public int money = 12;
    public int score = 0;
    public int cardCount = 0;

    [Header("Cards")]
    public PlayerHand playerHand;
    public List<GameObject> handCards = new List<GameObject>();

    [Header("Status Effects")]
    public bool disruptedNextTurn = false;

    [Header("UI")]
    public PresentTheNumberUI presentTheNumberUI;
    public PlayerManager playerManager;
    public PlayerStatusPanelUI statusPanel;

    private void Awake()
    {
        EnsurePlayerHand();
    }

    private void Start()
    {
        EnsurePlayerHand();
        RefreshUI();
    }

    public bool CanAfford(int cost)
    {
        return money >= cost;
    }


    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return "Player " + playerId;
    }

    public bool SpendMoney(int cost)
    {
        if (money < cost)
        {
            return false;
        }

        money -= cost;
        RefreshUI();
        return true;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        RefreshUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        RefreshUI();
    }

    public void SetCardCount(int count)
    {
        cardCount = Mathf.Max(0, count);
        RefreshUI();
    }

    public int GetHandCardCount()
    {
        EnsurePlayerHand();
        return playerHand != null ? playerHand.GetCardCount() : cardCount;
    }

    public void AddCardToHand(GameObject cardPrefab)
    {
        if (cardPrefab == null)
        {
            return;
        }

        EnsurePlayerHand();

        if (playerHand != null)
        {
            playerHand.AddCard(cardPrefab);
        }

        SetCardCount(GetHandCardCount());
    }

    public bool RemoveCardFromHand(GameObject cardPrefab)
    {
        EnsurePlayerHand();

        if (playerHand == null || cardPrefab == null)
        {
            return false;
        }

        bool removed = playerHand.RemoveCard(cardPrefab);

        if (removed)
        {
            SetCardCount(GetHandCardCount());
        }

        return removed;
    }

    public void SyncCardCountFromHand()
    {
        SetCardCount(GetHandCardCount());
    }

    public PlayerHand GetPlayerHand()
    {
        EnsurePlayerHand();
        return playerHand;
    }

    public void AddKillReward(int goldReward, int scoreReward)
    {
        money += goldReward;
        score += scoreReward;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (playerManager != null)
        {
            playerManager.RefreshPlayerUI(playerId);
        }
        else if (presentTheNumberUI != null)
        {
            presentTheNumberUI.SetNumbers(money, cardCount);
        }

        if (statusPanel != null)
        {
            statusPanel.Refresh();
        }
    }

    private void EnsurePlayerHand()
    {
        if (playerHand == null)
        {
            playerHand = GetComponent<PlayerHand>();
        }

        if (playerHand == null)
        {
            playerHand = gameObject.AddComponent<PlayerHand>();
            Debug.Log("Added missing PlayerHand to " + gameObject.name + ".");
        }

        playerHand.playerId = playerId;

        if (handCards != null && handCards.Count > 0 && playerHand.GetCardCount() == 0)
        {
            playerHand.CopyFrom(handCards);
        }

        handCards = playerHand.GetCards();
    }
}
