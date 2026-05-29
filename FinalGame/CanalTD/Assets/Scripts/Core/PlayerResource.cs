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
    public PlayerType playerType = PlayerType.Human;

    [Header("Resource")]
    public int money = 12;
    public int score = 0;
    public int cardCount = 0;

    [Header("Cards")]
    public PlayerHand playerHand;
    public List<GameObject> handCards = new List<GameObject>();

    [Header("Status Effects")]
    public bool disruptedNextTurn = false;
    public int disruptedTurnsRemaining = 0;
    public bool isEliminated = false;

    [Header("UI")]
    public PresentTheNumberUI presentTheNumberUI;
    public PlayerManager playerManager;
    public PlayerStatusPanelUI statusPanel;

    private void Awake()
    {
        LoadSetupFromPlayerPrefs();
        EnsurePlayerHand();
    }

    private void Start()
    {
        LoadSetupFromPlayerPrefs();
        EnsurePlayerHand();
        RefreshUI();
    }

    public bool CanAfford(int cost)
    {
        return !isEliminated && money >= cost;
    }

    public int GetMoney()
    {
        return money;
    }

    public int GetScore()
    {
        return score;
    }

    public int GetCardCount()
    {
        return GetHandCardCount();
    }

    public bool IsEliminated()
    {
        return isEliminated;
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return "Player " + playerId;
    }

    // Loads a ModeSelectScene name and AI flag for this player, while preserving Inspector names when no saved name exists.
    public void LoadSetupFromPlayerPrefs()
    {
        string key = "PlayerName_" + playerId;

        if (PlayerPrefs.HasKey(key))
        {
            string savedName = PlayerPrefs.GetString(key);

            if (!string.IsNullOrWhiteSpace(savedName))
            {
                displayName = savedName.Trim();
            }
        }

        string mode = PlayerPrefs.GetString("GameMode", "");
        bool isPrototypeMode = mode == "OnlineAIPrototype";

        if (!isPrototypeMode)
        {
            playerType = PlayerType.Human;
            return;
        }

        // Controller type comes only from room setup data:
        // PlayerIsAI_i == 1 means AI, PlayerIsAI_i == 0 with a saved name means Human.
        bool hasName = PlayerPrefs.HasKey(key) && !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(key));
        bool isAI = PlayerPrefs.GetInt("PlayerIsAI_" + playerId, 0) == 1;
        playerType = !hasName ? PlayerType.Empty : isAI ? PlayerType.AI : PlayerType.Human;
    }

    // Kept for older callers that only need display-name loading.
    public void LoadDisplayNameFromPlayerPrefs()
    {
        LoadSetupFromPlayerPrefs();
    }

    public bool HasPendingDisrupt()
    {
        return disruptedNextTurn || disruptedTurnsRemaining > 0;
    }

    public void ApplyDisruptNextTurn()
    {
        disruptedNextTurn = true;
        disruptedTurnsRemaining = Mathf.Max(disruptedTurnsRemaining, 1);
    }

    public void ClearPendingDisrupt()
    {
        disruptedNextTurn = false;
        disruptedTurnsRemaining = 0;
    }

    public void ConsumeDisruptForThisTurn()
    {
        if (disruptedTurnsRemaining > 0)
        {
            disruptedTurnsRemaining -= 1;
        }

        disruptedNextTurn = disruptedTurnsRemaining > 0;
    }

    public bool SpendMoney(int cost)
    {
        if (isEliminated)
        {
            return false;
        }

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
        if (isEliminated)
        {
            return;
        }

        money += amount;
        RefreshUI();
    }

    public void AddScore(int amount)
    {
        if (isEliminated)
        {
            return;
        }

        score += amount;
        RefreshUI();
    }

    public void SetCardCount(int count)
    {
        if (isEliminated)
        {
            return;
        }

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
        if (isEliminated)
        {
            return;
        }

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
        if (isEliminated)
        {
            return false;
        }

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
        if (isEliminated)
        {
            return;
        }

        money += goldReward;
        score += scoreReward;
        RefreshUI();
    }

    public void MarkEliminated()
    {
        isEliminated = true;
        ClearPendingDisrupt();
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
