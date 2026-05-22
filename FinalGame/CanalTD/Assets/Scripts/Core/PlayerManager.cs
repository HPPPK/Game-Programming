/*
 * File: PlayerManager.cs
 *
 * Purpose:
 * Owns player lookup and current-player state. It connects player IDs to
 * PlayerResource, PlayerHand, PlayerVisualConfig, and UI refresh behavior.
 *
 * Notes:
 * Player ID is used for game logic. PlayerResource.displayName is used for
 * visible UI text.
 */
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class PlayerVisualConfig
{
    public int playerId;
    public Color playerColor = Color.white;
    public GameObject castleReference;
    public Sprite towerSprite;
    public GameObject towerPrefab;
    public Color ownedLandColor = Color.white;
    public Sprite ownedLandSprite;
}

public class PlayerManager : MonoBehaviour
{
    [Header("Players")]
    public List<PlayerResource> players = new List<PlayerResource>();
    public int currentPlayerId = 0;

    [Header("Player Visuals")]
    public List<PlayerVisualConfig> playerVisualConfigs = new List<PlayerVisualConfig>();

    [Header("UI")]
    public PresentTheNumberUI presentTheNumberUI;
    public MonoBehaviour toastMessage;

    public event System.Action<int> OnCurrentPlayerChanged;

    private void Start()
    {
        ConnectPlayerResources();
        RefreshCurrentPlayerUI();
    }

    public int GetCurrentPlayerId()
    {
        return currentPlayerId;
    }

    public PlayerResource GetCurrentPlayerResource()
    {
        foreach (PlayerResource player in players)
        {
            if (player != null && player.playerId == currentPlayerId)
            {
                return player;
            }
        }

        return null;
    }

    public string GetPlayerDisplayName(int playerId)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player != null)
        {
            return player.GetDisplayName();
        }

        return "Player " + playerId;
    }

    public PlayerHand GetCurrentPlayerHand()
    {
        return GetPlayerHand(currentPlayerId);
    }

    public PlayerHand GetPlayerHand(int playerId)
    {
        foreach (PlayerResource player in players)
        {
            if (player != null && player.playerId == playerId)
            {
                PlayerHand hand = player.GetPlayerHand();

                if (hand == null)
                {
                    Debug.LogWarning("Player " + playerId + " is missing PlayerHand.");
                }

                return hand;
            }
        }

        Debug.LogWarning("No PlayerResource found for player " + playerId + ".");
        return null;
    }

    public List<PlayerResource> GetOtherPlayers(int currentPlayerId)
    {
        List<PlayerResource> otherPlayers = new List<PlayerResource>();

        if (players == null)
        {
            return otherPlayers;
        }

        foreach (PlayerResource player in players)
        {
            if (player != null && player.playerId != currentPlayerId)
            {
                otherPlayers.Add(player);
            }
        }

        return otherPlayers;
    }

    public void SetCurrentPlayer(int playerId)
    {
        currentPlayerId = playerId;
        OnCurrentPlayerChanged?.Invoke(currentPlayerId);
        RefreshCurrentPlayerUI();
        ShowToast(GetPlayerDisplayName(currentPlayerId) + "'s turn started.");
    }

    public void AdvanceToNextPlayer()
    {
        if (players == null || players.Count == 0)
        {
            SetCurrentPlayer(currentPlayerId);
            return;
        }

        int currentIndex = -1;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].playerId == currentPlayerId)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = currentIndex >= 0 ? currentIndex + 1 : 0;

        for (int checkedCount = 0; checkedCount < players.Count; checkedCount++)
        {
            int wrappedIndex = (nextIndex + checkedCount) % players.Count;

            if (players[wrappedIndex] != null)
            {
                SetCurrentPlayer(players[wrappedIndex].playerId);
                return;
            }
        }

        SetCurrentPlayer(currentPlayerId);
    }

    public bool IsLastPlayer()
    {
        if (players == null || players.Count == 0)
        {
            return true;
        }

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i] != null)
            {
                return players[i].playerId == currentPlayerId;
            }
        }

        return true;
    }

    public void ResetToFirstPlayer()
    {
        if (players == null || players.Count == 0)
        {
            SetCurrentPlayer(0);
            return;
        }

        foreach (PlayerResource player in players)
        {
            if (player != null)
            {
                SetCurrentPlayer(player.playerId);
                return;
            }
        }

        SetCurrentPlayer(0);
    }

    public void RefreshCurrentPlayerUI()
    {
        RefreshPlayerUI(currentPlayerId);
    }

    public void RefreshPlayerUI(int playerId)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player == null)
        {
            return;
        }

        if (player.playerId == currentPlayerId && presentTheNumberUI != null)
        {
            presentTheNumberUI.SetNumbers(player.money, player.cardCount);
        }

        if (player.statusPanel != null)
        {
            player.statusPanel.Refresh();
        }
    }

    public void RefreshAllPlayerStatusPanels()
    {
        foreach (PlayerResource player in players)
        {
            if (player != null && player.statusPanel != null)
            {
                player.statusPanel.Refresh();
            }
        }
    }

    public PlayerVisualConfig GetVisualConfig(int playerId)
    {
        foreach (PlayerVisualConfig config in playerVisualConfigs)
        {
            if (config != null && config.playerId == playerId)
            {
                return config;
            }
        }

        return null;
    }

    public Color GetPlayerColor(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.playerColor : Color.white;
    }

    public GameObject GetTowerPrefabForPlayer(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.towerPrefab : null;
    }

    public Sprite GetTowerSpriteForPlayer(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.towerSprite : null;
    }

    public Color GetOwnedLandColor(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.ownedLandColor : Color.white;
    }

    public Sprite GetOwnedLandSprite(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.ownedLandSprite : null;
    }

    private void ConnectPlayerResources()
    {
        foreach (PlayerResource player in players)
        {
            if (player == null)
            {
                continue;
            }

            if (player.playerManager == null)
            {
                player.playerManager = this;
            }

            player.GetPlayerHand();
            player.SyncCardCountFromHand();
        }
    }

    private PlayerResource GetPlayerResource(int playerId)
    {
        foreach (PlayerResource player in players)
        {
            if (player != null && player.playerId == playerId)
            {
                return player;
            }
        }

        return null;
    }

    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
            return;
        }

        CardDrawManager cardManager = FindObjectOfType<CardDrawManager>();

        if (cardManager != null)
        {
            cardManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }

    private bool TryCallToastMethod(string message)
    {
        if (toastMessage == null)
        {
            return false;
        }

        string[] methodNames =
        {
            "ShowMessage",
            "Show",
            "ShowToast",
            "Display",
            "ShowWarningMessage"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = toastMessage.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null
            );

            if (method == null)
            {
                continue;
            }

            method.Invoke(toastMessage, new object[] { message });
            return true;
        }

        return false;
    }
}
