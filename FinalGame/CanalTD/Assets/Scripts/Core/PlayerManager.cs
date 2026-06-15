/*
 * File: PlayerManager.cs
 *
 * Purpose:
 * Implements PlayerManager for the core layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PlayerManager within the core system.
 * - Coordinate related objects, state changes, and cross-system communication.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PlayerManager in the scene or prefab where it is used and confirm the main happy path still works.
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
    public Sprite shockTrapSprite;
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

    /// <summary>
    /// Sets up player manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        ConnectPlayerResources();
        RefreshCurrentPlayerUI();
    }

    /// <summary>
    /// Returns current player ID from the current scene or gameplay state.
    /// </summary>
    public int GetCurrentPlayerId()
    {
        return currentPlayerId;
    }

    /// <summary>
    /// Returns current player resource from the current scene or gameplay state.
    /// </summary>
    public PlayerResource GetCurrentPlayerResource()
    {
        foreach (PlayerResource player in players)
        {
            if (player != null && player.playerId == currentPlayerId && !player.isEliminated)
            {
                return player;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns player display name from the current scene or gameplay state.
    /// </summary>
    public string GetPlayerDisplayName(int playerId)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player != null)
        {
            return player.GetDisplayName();
        }

        return "Player " + playerId;
    }

    /// <summary>
    /// Returns current player hand used by card handling or target selection.
    /// </summary>
    public PlayerHand GetCurrentPlayerHand()
    {
        /// <summary>
        /// Returns player hand needed by this gameplay system.
        /// </summary>
        return GetPlayerHand(currentPlayerId);
    }

    /// <summary>
    /// Returns player hand used by card handling or target selection.
    /// </summary>
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

    /// <summary>
    /// Returns other players from the current scene or gameplay state.
    /// </summary>
    public List<PlayerResource> GetOtherPlayers(int currentPlayerId)
    {
        List<PlayerResource> otherPlayers = new List<PlayerResource>();

        if (players == null)
        {
            return otherPlayers;
        }

        foreach (PlayerResource player in players)
        {
            if (player != null && player.playerId != currentPlayerId && !player.isEliminated)
            {
                otherPlayers.Add(player);
            }
        }

        return otherPlayers;
    }

    /// <summary>
    /// Sets current player and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetCurrentPlayer(int playerId)
    {
        SetCurrentPlayer(playerId, true);
    }

    /// <summary>
    /// Sets current player and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetCurrentPlayer(int playerId, bool showTurnToast)
    {
        PlayerResource requestedPlayer = GetPlayerResource(playerId);

        if (requestedPlayer != null && requestedPlayer.isEliminated)
        {
            PlayerResource nextActivePlayer = GetFirstActivePlayer();

            if (nextActivePlayer == null)
            {
                return;
            }

            playerId = nextActivePlayer.playerId;
        }

        currentPlayerId = playerId;
        OnCurrentPlayerChanged?.Invoke(currentPlayerId);
        RefreshCurrentPlayerUI();

        if (showTurnToast)
        {
            ShowToast(GetPlayerDisplayName(currentPlayerId) + "'s turn started.");
        }
    }

    /// <summary>
    /// Advances to next player to the next turn, wave, player, or tutorial step.
    /// </summary>
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

            if (players[wrappedIndex] != null && !players[wrappedIndex].isEliminated)
            {
                SetCurrentPlayer(players[wrappedIndex].playerId);
                return;
            }
        }

        SetCurrentPlayer(currentPlayerId);
    }

    /// <summary>
    /// Checks the current state to decide whether last player is true.
    /// </summary>
    public bool IsLastPlayer()
    {
        if (players == null || players.Count == 0)
        {
            return true;
        }

        for (int i = players.Count - 1; i >= 0; i--)
        {
            if (players[i] != null && !players[i].isEliminated)
            {
                return players[i].playerId == currentPlayerId;
            }
        }

        return true;
    }

    /// <summary>
    /// Resets to first player for a new turn, wave, player, scene, or match state.
    /// </summary>
    public void ResetToFirstPlayer()
    {
        if (players == null || players.Count == 0)
        {
            SetCurrentPlayer(0);
            return;
        }

        foreach (PlayerResource player in players)
        {
            if (player != null && !player.isEliminated)
            {
                SetCurrentPlayer(player.playerId);
                return;
            }
        }

        SetCurrentPlayer(0);
    }

    /// <summary>
    /// Refreshes current player UI from the latest gameplay data.
    /// </summary>
    public void RefreshCurrentPlayerUI()
    {
        PlayerResource presentNumberPlayer = GetPresentNumberTargetResource();

        if (presentNumberPlayer != null && presentTheNumberUI != null)
        {
            presentTheNumberUI.SetNumbers(presentNumberPlayer.money, presentNumberPlayer.cardCount);
        }

        RefreshPlayerUI(currentPlayerId);
    }

    /// <summary>
    /// Refreshes player UI from the latest gameplay data.
    /// </summary>
    public void RefreshPlayerUI(int playerId)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player == null)
        {
            return;
        }

        PlayerResource presentNumberPlayer = GetPresentNumberTargetResource();

        if (presentTheNumberUI != null &&
            presentNumberPlayer != null &&
            player.playerId == presentNumberPlayer.playerId)
        {
            presentTheNumberUI.SetNumbers(player.money, player.cardCount);
        }

        if (player.statusPanel != null)
        {
            player.statusPanel.Refresh();
        }
    }

    /// <summary>
    /// Refreshes all player status panels from the latest gameplay data.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether player eliminated is true.
    /// </summary>
    public bool IsPlayerEliminated(int playerId)
    {
        PlayerResource player = GetPlayerResource(playerId);
        return player != null && player.isEliminated;
    }

    /// <summary>
    /// Handles eliminate player for this gameplay system.
    /// </summary>
    public void EliminatePlayer(int playerId)
    {
        PlayerResource player = GetPlayerResource(playerId);

        if (player == null || player.isEliminated)
        {
            return;
        }

        PlayerExitManager exitManager = FindObjectOfType<PlayerExitManager>();

        if (exitManager != null)
        {
            if (exitManager.playerManager == null)
            {
                exitManager.playerManager = this;
            }

            exitManager.CleanupPlayerOwnedRuntimeState(playerId);
        }
        else
        {
            player.MarkEliminated();
            ClearPlayerLandAndTowers(playerId);
        }

        RefreshAllPlayerStatusPanels();
        RefreshCurrentPlayerUI();
        ShowToast(player.GetDisplayName() + " has been eliminated.");
    }

    /// <summary>
    /// Returns visual config from the current scene or gameplay state.
    /// </summary>
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

    /// <summary>
    /// Returns player color from the current scene or gameplay state.
    /// </summary>
    public Color GetPlayerColor(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.playerColor : Color.white;
    }

    // Existing color source for tower prefabs. BuildTowerManager uses this as
    // the fallback for every tower type until type-specific colored prefabs exist.
    /// <summary>
    /// Returns tower prefab for player used by land, tower, cost, or build decisions.
    /// </summary>
    public GameObject GetTowerPrefabForPlayer(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.towerPrefab : null;
    }

    // Optional sprite fallback for generic tower prefabs that are not already
    // colored per player.
    /// <summary>
    /// Returns tower sprite for player used by land, tower, cost, or build decisions.
    /// </summary>
    public Sprite GetTowerSpriteForPlayer(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.towerSprite : null;
    }

    /// <summary>
    /// Returns shock trap sprite for player from the current scene or gameplay state.
    /// </summary>
    public Sprite GetShockTrapSpriteForPlayer(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.shockTrapSprite : null;
    }

    /// <summary>
    /// Returns owned land color used by land, tower, cost, or build decisions.
    /// </summary>
    public Color GetOwnedLandColor(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.ownedLandColor : Color.white;
    }

    /// <summary>
    /// Returns owned land sprite used by land, tower, cost, or build decisions.
    /// </summary>
    public Sprite GetOwnedLandSprite(int playerId)
    {
        PlayerVisualConfig config = GetVisualConfig(playerId);
        return config != null ? config.ownedLandSprite : null;
    }

    /// <summary>
    /// Handles connect player resources for this gameplay system.
    /// </summary>
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

    /// <summary>
    /// Returns player resource from the current scene or gameplay state.
    /// </summary>
    public PlayerResource GetPlayerResource(int playerId)
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

    /// <summary>
    /// Returns first active player from the current scene or gameplay state.
    /// </summary>
    private PlayerResource GetFirstActivePlayer()
    {
        foreach (PlayerResource player in players)
        {
            if (player != null && !player.isEliminated)
            {
                return player;
            }
        }

        return null;
    }

    // Local mode keeps PresentTheNumber synced with the active player, but AI
    // mode and online mode should pin it to the local visible hand owner.
    /// <summary>
    /// Returns present number target resource used by card handling or target selection.
    /// </summary>
    private PlayerResource GetPresentNumberTargetResource()
    {
        if (players == null || players.Count == 0)
        {
            return null;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineGameSceneManager.Instance != null &&
            PhotonOnlineGameSceneManager.Instance.LocalPlayerId >= 0)
        {
            PlayerResource onlineLocalPlayer = GetPlayerResource(PhotonOnlineGameSceneManager.Instance.LocalPlayerId);

            if (onlineLocalPlayer != null && !onlineLocalPlayer.isEliminated)
            {
                return onlineLocalPlayer;
            }
        }

        if (TurnSourceResolver.IsAIPrototypeActive())
        {
            foreach (PlayerResource player in players)
            {
                if (player != null &&
                    player.playerType == PlayerType.Human &&
                    !player.isEliminated)
                {
                    return player;
                }
            }
        }

        /// <summary>
        /// Returns current player resource needed by this gameplay system.
        /// </summary>
        return GetCurrentPlayerResource();
    }

    /// <summary>
    /// Clears player land and towers and removes its temporary gameplay or visual effect.
    /// </summary>
    private void ClearPlayerLandAndTowers(int playerId)
    {
        TowerBuildArea[] buildAreas = FindObjectsOfType<TowerBuildArea>();

        foreach (TowerBuildArea area in buildAreas)
        {
            if (area != null)
            {
                area.ResetForEliminatedPlayer(playerId);
            }
        }

        CannonTower[] towers = FindObjectsOfType<CannonTower>();

        foreach (CannonTower tower in towers)
        {
            if (tower != null && tower.ownerPlayerId == playerId)
            {
                Destroy(tower.gameObject);
            }
        }
    }

    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Attempts to call toast method and returns false if rules, resources, or references block it.
    /// </summary>
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
