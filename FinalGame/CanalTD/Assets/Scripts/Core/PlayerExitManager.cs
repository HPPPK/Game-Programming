/*
 * File: PlayerExitManager.cs
 *
 * Purpose:
 * Implements PlayerExitManager for the core layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PlayerExitManager within the core system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PlayerExitManager in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

public class PlayerExitManager : MonoBehaviour
{
    private const string OnlinePhotonMode = "OnlinePhotonPUN2";
    [Header("Managers")]
    public PlayerManager playerManager;
    public TurnManager turnManager;
    public GamePhaseManager gamePhaseManager;
    public AIPrototypeTurnManager aiPrototypeTurnManager;
    public CardDrawManager cardDrawManager;
    public PhotonOnlineGameSceneManager photonOnlineGameSceneManager;

    [Header("Scenes")]
    public string resultSceneName = "ResultScene";
    public string homeSceneName = "HomeScene";

    [Header("Exit Confirmation UI")]
    public GameObject confirmExitPanel;
    public Button confirmExitButton;
    public Button cancelExitButton;

    [Header("Future Online Placeholder")]
    [Tooltip("Safe placeholder until a real online mode exists. Leave false for current project scenes.")]
    public bool forceOnlineMode = false;
    [Tooltip("Future local online player id. Kept simple until networking is added.")]
    public int localOnlinePlayerId = 0;

    private const string LocalFourPlayerMode = "Local4Player";
    private const string OnlineAIPrototypeMode = "OnlineAIPrototype";

    private void Awake()
    {
        if (playerManager == null)
        {
            playerManager = FindObjectOfType<PlayerManager>();
        }

        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }

        if (gamePhaseManager == null)
        {
            gamePhaseManager = FindObjectOfType<GamePhaseManager>();
        }

        if (aiPrototypeTurnManager == null)
        {
            aiPrototypeTurnManager = FindObjectOfType<AIPrototypeTurnManager>();
        }

        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        if (photonOnlineGameSceneManager == null)
        {
            photonOnlineGameSceneManager = FindObjectOfType<PhotonOnlineGameSceneManager>();
        }

        if (confirmExitButton != null)
        {
            confirmExitButton.onClick.RemoveAllListeners();
            confirmExitButton.onClick.AddListener(ConfirmExit);
        }

        if (cancelExitButton != null)
        {
            cancelExitButton.onClick.RemoveAllListeners();
            cancelExitButton.onClick.AddListener(CancelExit);
        }

        if (confirmExitPanel != null)
        {
            confirmExitPanel.SetActive(false);
        }
    }

    // Unity Button entrypoint for both GameScene and GameScene_AIPrototype.
    public void OnClickExitGame()
    {
        Debug.Log("EXIT BUTTON CLICKED");

        if (confirmExitPanel != null)
        {
            confirmExitPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Confirm exit panel is not assigned. Exiting immediately as fallback.");
            ExitCurrentMatch();
        }
    }

    public void ConfirmExit()
    {
        Debug.Log("EXIT CONFIRMED");

        string sceneName = SceneManager.GetActiveScene().name;
        bool inPhotonRoom = IsInPhotonRoom();
        bool photonOnlineActive = PhotonOnlineGameSceneManager.IsOnlinePhotonGameSceneActive();
        PhotonOnlineGameSceneManager activeOnlineManager = GetPhotonOnlineGameSceneManager();
        Debug.Log(
            "ConfirmExit | scene = " + sceneName +
            ", PhotonNetwork.InRoom = " + inPhotonRoom +
            ", PhotonOnlineGameSceneManager.IsOnlinePhotonGameSceneActive() = " + photonOnlineActive +
            ", saved GameMode = " + PlayerPrefs.GetString("GameMode", "") +
            ", onlineManager exists = " + (activeOnlineManager != null) +
            ", hasBootstrappedOnlineMatch = " + (activeOnlineManager != null && activeOnlineManager.HasEverBootstrappedOnlineMatch) +
            ", isLocalLeavingMatch = " + (activeOnlineManager != null && activeOnlineManager.IsLocalLeavingMatch) +
            ", hasDetachedFromOnlineMatch = " + (activeOnlineManager != null && activeOnlineManager.HasDetachedFromOnlineMatch)
        );

        if (confirmExitPanel != null)
        {
            confirmExitPanel.SetActive(false);
        }

        ExitCurrentMatch();
    }

    public void CancelExit()
    {
        Debug.Log("EXIT CANCELLED");

        if (confirmExitPanel != null)
        {
            confirmExitPanel.SetActive(false);
        }
    }

    public void ExitCurrentMatch()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string savedGameMode = PlayerPrefs.GetString("GameMode", "");
        bool isAIPrototypeScene = sceneName == "GameScene_AIPrototype";
        bool isMainGameScene = sceneName == "GameScene";
        PhotonOnlineGameSceneManager activeOnlineManager = GetPhotonOnlineGameSceneManager();
        // Treat the current GameScene as local unless a future online flag explicitly overrides it.
        bool isLocalGameScene = sceneName == "GameScene" &&
            (string.IsNullOrEmpty(savedGameMode) || savedGameMode == LocalFourPlayerMode);
        bool isOnlineMode = IsOnlineGameSceneExitPath(sceneName, activeOnlineManager);

        Debug.Log(
            "ExitCurrentMatch mode check | scene = " + sceneName +
            ", saved GameMode = " + savedGameMode +
            ", PhotonNetwork.InRoom = " + IsInPhotonRoom() +
            ", onlineManager exists = " + (activeOnlineManager != null) +
            ", hasBootstrappedOnlineMatch = " + (activeOnlineManager != null && activeOnlineManager.HasEverBootstrappedOnlineMatch) +
            ", isLocalLeavingMatch = " + (activeOnlineManager != null && activeOnlineManager.IsLocalLeavingMatch) +
            ", hasDetachedFromOnlineMatch = " + (activeOnlineManager != null && activeOnlineManager.HasDetachedFromOnlineMatch) +
            ", forceOnlineMode = " + forceOnlineMode +
            ", photonOnlineActive = " + isOnlineMode
        );

        if (isOnlineMode)
        {
            Debug.Log("ConfirmExit path = Online");
            Debug.Log("Current mode: Online Photon GameScene");

            if (activeOnlineManager != null)
            {
                activeOnlineManager.homeSceneName = homeSceneName;
                activeOnlineManager.RequestLocalExitToHome();
            }
            else
            {
                Debug.Log("Online exit detected but already left Photon room. Loading HomeScene.");
                SceneManager.LoadScene(homeSceneName);
            }
            return;
        }

        if (isAIPrototypeScene || isLocalGameScene || savedGameMode == OnlineAIPrototypeMode)
        {
            Debug.Log("ConfirmExit path = LocalAI");
            Debug.Log("Current mode: Local / AI");
            ExitLocalOrAIMatch();
            return;
        }

        Debug.Log("ConfirmExit path = LocalAI");
        Debug.Log("Current mode unknown. Falling back to local/AI exit flow.");
        ExitLocalOrAIMatch();
    }

    public void ExitLocalOrAIMatch()
    {
        Debug.Log("Match ended locally via Exit Game.");

        List<PlayerResultEntry> results = BuildCurrentResultEntries();
        SortResultsByScore(results);
        AssignRanks(results);
        GameResultData.SetResults(results);

        if (string.IsNullOrEmpty(resultSceneName))
        {
            Debug.LogWarning("Result scene name is empty. ExitLocalOrAIMatch could not continue.");
            return;
        }

        SceneManager.LoadScene(resultSceneName);
    }

    public void ExitOnlinePlayer(int playerId)
    {
        Debug.Log("Online player exited: playerId = " + playerId);
        PhotonOnlineGameSceneManager activeOnlineManager = GetPhotonOnlineGameSceneManager();
        bool isMainGameScene = SceneManager.GetActiveScene().name == "GameScene";

        if (isMainGameScene &&
            activeOnlineManager != null &&
            activeOnlineManager.HasLiveOnlineMatchSession())
        {
            activeOnlineManager.homeSceneName = homeSceneName;
            activeOnlineManager.RequestLocalExitToHome();
            return;
        }

        Debug.LogWarning("ExitOnlinePlayer was called without an active local Photon online player. No ResultScene fallback will be used.");
    }

    private PhotonOnlineGameSceneManager GetPhotonOnlineGameSceneManager()
    {
        if (photonOnlineGameSceneManager == null)
        {
            photonOnlineGameSceneManager = FindObjectOfType<PhotonOnlineGameSceneManager>();
        }

        return photonOnlineGameSceneManager;
    }

    private bool IsOnlineGameSceneExitPath(string sceneName, PhotonOnlineGameSceneManager activeOnlineManager)
    {
        if (sceneName != "GameScene")
        {
            return false;
        }

        string savedGameMode = PlayerPrefs.GetString("GameMode", string.Empty);

        if (savedGameMode == OnlinePhotonMode)
        {
            return true;
        }

        if (activeOnlineManager != null &&
            (activeOnlineManager.WasOnlineMatchScene() || activeOnlineManager.HasLiveOnlineMatchSession()))
        {
            return true;
        }

        return IsInPhotonRoom();
    }

    private bool IsInPhotonRoom()
    {
#if PHOTON_UNITY_NETWORKING
        return PhotonNetwork.InRoom && !PhotonNetwork.OfflineMode;
#else
        return false;
#endif
    }

    public void CleanupPlayerState(int playerId)
    {
        Debug.Log("Cleaning player state for player " + playerId + ".");

        PlayerResource player = playerManager != null ? playerManager.GetPlayerResource(playerId) : null;

        if (player == null)
        {
            Debug.LogWarning("CleanupPlayerState skipped because player " + playerId + " was not found.");
            return;
        }

        bool exitingCurrentPlayer = playerManager != null && playerManager.GetCurrentPlayerId() == playerId;

        if (exitingCurrentPlayer && cardDrawManager != null)
        {
            // Cancel local pending card state before clearing owned gameplay state.
            cardDrawManager.CancelPendingCard();
        }

        player.ClearPendingDisrupt();

        if (cardDrawManager != null)
        {
            cardDrawManager.ReturnPlayerHandToDeck(playerId);
        }
        else
        {
            PlayerHand hand = player.GetPlayerHand();

            if (hand != null)
            {
                hand.Clear();
                player.SyncCardCountFromHand();
                Debug.LogWarning("CardDrawManager missing. Cleared hand for player " + playerId + " without returning cards to deck.");
            }
        }

        CleanupOwnedBuildAreasAndTowers(playerId);
        CleanupOwnedShockTraps(playerId);

        // TODO: Gates do not currently track ownership for lock state, so owned locks
        // cannot be released precisely here. Land ownership cleanup still releases control.
        Debug.LogWarning("TODO: Gate lock ownership is not tracked yet. No direct gate unlock cleanup was applied for player " + playerId + ".");
        Debug.LogWarning("TODO: Temporary boost ownership is not tracked separately yet. Player-owned towers were removed, but foreign boosted towers are not explicitly reset.");

        player.MarkEliminated();
        player.RefreshUI();

        if (playerManager != null)
        {
            playerManager.RefreshAllPlayerStatusPanels();
        }

        // Stop here if the match no longer has any active players. The caller decides
        // which scene to load, and we avoid restarting local turn flow unnecessarily.
        if (AreAllPlayersInactiveOrEliminated())
        {
            Debug.Log("All players inactive after cleaning player " + playerId + ".");
            return;
        }

        if (aiPrototypeTurnManager != null && aiPrototypeTurnManager.isActiveAndEnabled)
        {
            aiPrototypeTurnManager.HandlePlayerExit(playerId);
            return;
        }

        if (exitingCurrentPlayer && gamePhaseManager != null && gamePhaseManager.IsPlayerPhase() && playerManager != null)
        {
            playerManager.AdvanceToNextPlayer();

            if (turnManager != null)
            {
                turnManager.currentPlayerId = playerManager.GetCurrentPlayerId();
            }

            gamePhaseManager.StartPlayerPhase();
            return;
        }

        if (playerManager != null)
        {
            playerManager.RefreshCurrentPlayerUI();
        }
    }

    public bool AreAllPlayersInactiveOrEliminated()
    {
        if (aiPrototypeTurnManager != null && aiPrototypeTurnManager.isActiveAndEnabled)
        {
            return aiPrototypeTurnManager.AreAllPlayersInactiveOrEliminated();
        }

        if (playerManager == null || playerManager.players == null)
        {
            return true;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            if (player.playerType == PlayerType.Empty)
            {
                continue;
            }

            if (!player.isEliminated)
            {
                return false;
            }
        }

        Debug.Log("All players inactive.");
        return true;
    }

    private void CleanupOwnedBuildAreasAndTowers(int playerId)
    {
        TowerBuildArea[] buildAreas = FindObjectsOfType<TowerBuildArea>();

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea == null)
            {
                continue;
            }

            if (buildArea.frozenByPlayerId == playerId)
            {
                buildArea.ClearFreeze();
            }

            if (buildArea.inactiveForPlayerId == playerId)
            {
                buildArea.ClearActivationDelay();
            }

            if (buildArea.ownerPlayerId == playerId || buildArea.towerOwnerPlayerId == playerId)
            {
                buildArea.ResetForEliminatedPlayer(playerId);
                buildArea.RefreshOwnershipVisual(playerManager);
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

    private void CleanupOwnedShockTraps(int playerId)
    {
        ShockTrap[] traps = FindObjectsOfType<ShockTrap>();

        foreach (ShockTrap trap in traps)
        {
            if (trap != null && trap.ownerPlayerId == playerId)
            {
                Destroy(trap.gameObject);
            }
        }
    }

    private int GetLocalOnlinePlayerId()
    {
        if (localOnlinePlayerId >= 0)
        {
            return localOnlinePlayerId;
        }

        if (playerManager != null)
        {
            foreach (PlayerResource player in playerManager.players)
            {
                if (player != null && player.playerType == PlayerType.Human && !player.isEliminated)
                {
                    return player.playerId;
                }
            }

            return playerManager.GetCurrentPlayerId();
        }

        return 0;
    }

    private List<PlayerResultEntry> BuildCurrentResultEntries()
    {
        List<PlayerResultEntry> results = new List<PlayerResultEntry>();

        if (playerManager == null || playerManager.players == null)
        {
            return results;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null || player.playerType == PlayerType.Empty)
            {
                continue;
            }

            CastleBase castle = GetCastleForPlayer(player);
            int castleHp = castle != null ? castle.currentHP : 0;

            results.Add(new PlayerResultEntry(
                player.playerId,
                player.GetDisplayName(),
                player.score,
                player.money,
                castleHp,
                player.isEliminated
            ));
        }

        return results;
    }

    private CastleBase GetCastleForPlayer(PlayerResource player)
    {
        if (player == null)
        {
            return null;
        }

        CastleBase castle = player.GetComponent<CastleBase>();

        if (castle != null)
        {
            return castle;
        }

        if (player.statusPanel != null && player.statusPanel.linkedCastle != null)
        {
            return player.statusPanel.linkedCastle;
        }

        PlayerVisualConfig visualConfig = playerManager != null
            ? playerManager.GetVisualConfig(player.playerId)
            : null;

        if (visualConfig != null && visualConfig.castleReference != null)
        {
            return visualConfig.castleReference.GetComponent<CastleBase>();
        }

        return null;
    }

    private void SortResultsByScore(List<PlayerResultEntry> results)
    {
        results.Sort((a, b) =>
        {
            if (a.isEliminated != b.isEliminated)
            {
                return a.isEliminated ? 1 : -1;
            }

            if (a.isEliminated && b.isEliminated)
            {
                return a.playerId.CompareTo(b.playerId);
            }

            int scoreCompare = b.score.CompareTo(a.score);

            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            int hpCompare = b.castleHp.CompareTo(a.castleHp);

            if (hpCompare != 0)
            {
                return hpCompare;
            }

            int moneyCompare = b.money.CompareTo(a.money);

            if (moneyCompare != 0)
            {
                return moneyCompare;
            }

            return a.playerId.CompareTo(b.playerId);
        });
    }

    private void AssignRanks(List<PlayerResultEntry> results)
    {
        int activeRank = 1;
        int eliminatedRank = results != null ? results.Count : 0;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].isEliminated)
            {
                results[i].rank = eliminatedRank;
            }
            else
            {
                results[i].rank = activeRank;
                activeRank += 1;
            }
        }
    }
}
