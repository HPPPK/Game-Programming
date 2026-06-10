/*
 * File: GamePhaseManager.cs
 *
 * Purpose:
 * Implements GamePhaseManager for the core layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for GamePhaseManager within the core system.
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
 * - Verify GamePhaseManager in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GamePhase
{
    PlayerPhase,
    WavePhase
}

public class GamePhaseManager : MonoBehaviour
{
    [Header("Phase Settings")]
    public GamePhase currentPhase = GamePhase.PlayerPhase;
    public int currentRound = 1;
    public int currentWaveIndex = 1;
    public int maxWaves = 10;
    public string resultSceneName = "ResultScene";

    [Header("Managers")]
    public PlayerManager playerManager;
    public TurnManager turnManager;
    public WaveManager waveManager;
    public CardDrawManager cardDrawManager;
    public MonoBehaviour toastMessage;
    public float waveIncomingDelay = 2f;

    [Header("Card Start Rules")]
    public int initialCardsPerPlayer = 2;

    private bool playerTurnToastAlreadyShown = false;
    private bool initialHandsDealt = false;
    private Coroutine waveStartCoroutine;
    private PhotonOnlineGameSceneManager photonOnlineGameSceneManager;

    /// <summary>
    /// Sets up game phase manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        photonOnlineGameSceneManager = FindObjectOfType<PhotonOnlineGameSceneManager>();

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext())
        {
            Debug.Log("GamePhaseManager using Photon online GameScene flow.");
            return;
        }

        DealInitialHandsOnce();
        StartPlayerPhase();
    }

    /// <summary>
    /// Checks the current state to decide whether player phase is true.
    /// </summary>
    public bool IsPlayerPhase()
    {
        return currentPhase == GamePhase.PlayerPhase;
    }

    /// <summary>
    /// Checks the current state to decide whether wave phase is true.
    /// </summary>
    public bool IsWavePhase()
    {
        return currentPhase == GamePhase.WavePhase;
    }

    /// <summary>
    /// Starts player phase and enables its related gameplay flow.
    /// </summary>
    public void StartPlayerPhase()
    {
        currentPhase = GamePhase.PlayerPhase;

        if (playerManager != null && turnManager != null)
        {
            turnManager.currentPlayerId = playerManager.GetCurrentPlayerId();
        }

        PlayerResource activePlayer = playerManager != null ? playerManager.GetCurrentPlayerResource() : null;
        bool disruptedThisTurn = activePlayer != null && activePlayer.HasPendingDisrupt();

        if (turnManager != null)
        {
            turnManager.StartTurn(disruptedThisTurn);
        }

        if (disruptedThisTurn && activePlayer != null)
        {
            activePlayer.ConsumeDisruptForThisTurn();
            ShowToast(activePlayer.GetDisplayName() + " is disrupted this turn.");
        }

        ActivatePendingTakeoverLand();

        if (playerManager != null)
        {
            if (playerTurnToastAlreadyShown)
            {
                playerManager.RefreshCurrentPlayerUI();
            }
            else
            {
                playerManager.SetCurrentPlayer(playerManager.GetCurrentPlayerId());
            }

            playerTurnToastAlreadyShown = false;
        }
        else
        {
            int activePlayerId = turnManager != null ? turnManager.currentPlayerId : 0;
            ShowToast("Player " + activePlayerId + " turn started.");
        }

        UpdateTurnIndicator();
    }

    /// <summary>
    /// Updates turn indicator so the display or cached state matches current gameplay data.
    /// </summary>
    private void UpdateTurnIndicator()
    {
        if (CurrentTurnIndicatorManager.Instance != null)
        {
            int currentPlayerId = turnManager != null ? turnManager.currentPlayerId : 0;
            CurrentTurnIndicatorManager.Instance.UpdateCurrentTurnIndicator(currentPlayerId);
        }
    }

    /// <summary>
    /// Starts wave phase and enables its related gameplay flow.
    /// </summary>
    public void StartWavePhase()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.WaveStarted, null))
        {
            return;
        }

        currentPhase = GamePhase.WavePhase;
        TutorialManager.Instance?.NotifyTutorialWaveStarted();

        if (waveStartCoroutine != null)
        {
            return;
        }

        waveStartCoroutine = StartCoroutine(StartWaveAfterIncomingToast());
    }

     /// <summary>
     /// Starts wave after incoming toast and enables its related gameplay flow.
     /// </summary>
     private IEnumerator StartWaveAfterIncomingToast()
     {
         BuildTowerManager buildTowerManager = BuildTowerManager.Instance != null
             ? BuildTowerManager.Instance
             : FindObjectOfType<BuildTowerManager>();
         buildTowerManager?.HideBuildInteractionUI();
         cardDrawManager?.CancelPendingCard();

         if (waveManager != null)
         {
             waveManager.ShowWaveIncoming(currentWaveIndex);
         }
         else
         {
             ShowToast("Wave " + currentWaveIndex + " incoming!");
         }

         /// <summary>
         /// Handles wait for seconds for game phase manager.
         /// </summary>
         yield return new WaitForSeconds(waveIncomingDelay);

         // Hide turn markers when wave starts
         if (CurrentTurnIndicatorManager.Instance != null)
         {
             CurrentTurnIndicatorManager.Instance.HideAllMarkers();
         }

         NotifyTowersWaveStarted();
         NotifyShockTrapsWaveStarted();

         if (waveManager != null)
         {
             waveManager.StartWave();
         }

         waveStartCoroutine = null;
     }

    /// <summary>
    /// Responds to on end turn button clicked and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnEndTurnButtonClicked()
    {
        if (TutorialActionGate.BlockIfNotAllowed(TutorialActionType.EndTurnClicked, null))
        {
            return;
        }

        if (photonOnlineGameSceneManager == null)
        {
            photonOnlineGameSceneManager = FindObjectOfType<PhotonOnlineGameSceneManager>();
        }

        if (photonOnlineGameSceneManager != null && photonOnlineGameSceneManager.IsOnlineModeActive)
        {
            TutorialManager.Instance?.NotifyEndTurnClicked();
            photonOnlineGameSceneManager.HandleEndTurnButtonClicked();
            return;
        }

        ITurnSource activeTurnSource = TurnSourceResolver.GetActiveTurnSource(turnManager);

        if (activeTurnSource != null && !object.ReferenceEquals(activeTurnSource, turnManager))
        {
            TutorialManager.Instance?.NotifyEndTurnClicked();
            activeTurnSource.EndCurrentTurn();
            return;
        }

        if (!IsPlayerPhase())
        {
            ShowToast(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("Enemy wave is running.")
                : "Enemy wave is running.");
            return;
        }

        if (playerManager != null && !playerManager.IsLastPlayer())
        {
            TutorialManager.Instance?.NotifyEndTurnClicked();
            playerManager.AdvanceToNextPlayer();
            playerTurnToastAlreadyShown = true;
            StartPlayerPhase();
            return;
        }

        TutorialManager.Instance?.NotifyEndTurnClicked();
        StartWavePhase();
    }

    /// <summary>
    /// Responds to on wave finished and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnWaveFinished()
    {
        NotifyTowersWaveEnded();
        NotifyShockTrapsWaveEnded();
        TutorialManager.Instance?.NotifyTutorialWaveCompleted();

        if (currentWaveIndex >= maxWaves)
        {
            EndGame();
            return;
        }

        currentRound += 1;
        currentWaveIndex += 1;
        if (waveManager != null) waveManager.RefreshWaveCounterUI();

        if (playerManager != null)
        {
            playerManager.ResetToFirstPlayer();
            playerTurnToastAlreadyShown = true;
        }

        StartPlayerPhase();
    }

    /// <summary>
    /// Handles end game for this gameplay system.
    /// </summary>
    private void EndGame()
    {
        List<PlayerResultEntry> results = BuildResultEntries();
        SortResultsByScore(results);
        AssignRanks(results);
        GameResultData.SetResults(results);

        if (string.IsNullOrEmpty(resultSceneName))
        {
            Debug.LogWarning("Result scene name is empty.");
            return;
        }

        SceneManager.LoadScene(resultSceneName);
    }

    /// <summary>
    /// Builds result entries from configured scene objects and runtime state.
    /// </summary>
    private List<PlayerResultEntry> BuildResultEntries()
    {
        List<PlayerResultEntry> results = new List<PlayerResultEntry>();

        if (playerManager == null || playerManager.players == null)
        {
            return results;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            CastleBase castle = GetCastleForPlayer(player);
            string displayName = player.GetDisplayName();
            int castleHp = castle != null ? castle.currentHP : 0;

            results.Add(new PlayerResultEntry(
                player.playerId,
                displayName,
                player.score,
                player.money,
                castleHp,
                player.isEliminated
            ));
        }

        return results;
    }

    /// <summary>
    /// Returns castle for player from the current scene or gameplay state.
    /// </summary>
    private CastleBase GetCastleForPlayer(PlayerResource player)
    {
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

    /// <summary>
    /// Handles sort results by score for this gameplay system.
    /// </summary>
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

    /// <summary>
    /// Handles assign ranks for this gameplay system.
    /// </summary>
    private void AssignRanks(List<PlayerResultEntry> results)
    {
        int activeRank = 1;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].isEliminated)
            {
                results[i].rank = 4;
            }
            else
            {
                results[i].rank = activeRank;
                activeRank += 1;
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

    /// <summary>
    /// Deals initial hands once to players and refreshes their hand state.
    /// </summary>
    private void DealInitialHandsOnce()
    {
        if (initialHandsDealt)
        {
            return;
        }

        CardDrawManager manager = GetCardDrawManager();

        if (manager != null)
        {
            manager.DealInitialHands(initialCardsPerPlayer);
            initialHandsDealt = true;
        }
    }

    // Online GameScene bootstrap reuses the same initial hand setup as local/AI.
    /// <summary>
    /// Ensures initial hands dealt exists or is initialized before the flow continues.
    /// </summary>
    public void EnsureInitialHandsDealt()
    {
        DealInitialHandsOnce();
    }

    /// <summary>
    /// Returns card draw manager used by card handling or target selection.
    /// </summary>
    private CardDrawManager GetCardDrawManager()
    {
        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        return cardDrawManager;
    }

    /// <summary>
    /// Activates pending takeover land once its pending condition is satisfied.
    /// </summary>
    private void ActivatePendingTakeoverLand()
    {
        if (playerManager == null)
        {
            return;
        }

        int activePlayerId = playerManager.GetCurrentPlayerId();
        TowerBuildArea[] buildAreas = FindObjectsOfType<TowerBuildArea>();

        foreach (TowerBuildArea buildArea in buildAreas)
        {
            if (buildArea != null)
            {
                buildArea.ActivateForPlayerIfPending(activePlayerId);
                buildArea.ClearFreezeIfPendingForPlayer(activePlayerId);
            }
        }
    }

    /// <summary>
    /// Notifies connected systems that towers wave started occurred.
    /// </summary>
    private void NotifyTowersWaveStarted()
    {
        CannonTower[] towers = FindObjectsOfType<CannonTower>();

        foreach (CannonTower tower in towers)
        {
            if (tower != null)
            {
                tower.OnWaveStarted();
            }
        }
    }

    /// <summary>
    /// Notifies connected systems that towers wave ended occurred.
    /// </summary>
    private void NotifyTowersWaveEnded()
    {
        CannonTower[] towers = FindObjectsOfType<CannonTower>();

        foreach (CannonTower tower in towers)
        {
            if (tower != null)
            {
                tower.OnWaveEnded();
            }
        }
    }

    /// <summary>
    /// Notifies connected systems that shock traps wave started occurred.
    /// </summary>
    private void NotifyShockTrapsWaveStarted()
    {
        ShockTrap[] traps = FindObjectsOfType<ShockTrap>();

        foreach (ShockTrap trap in traps)
        {
            if (trap != null)
            {
                trap.OnWaveStarted();
            }
        }
    }

    /// <summary>
    /// Notifies connected systems that shock traps wave ended occurred.
    /// </summary>
    private void NotifyShockTrapsWaveEnded()
    {
        ShockTrap[] traps = FindObjectsOfType<ShockTrap>();

        foreach (ShockTrap trap in traps)
        {
            if (trap != null)
            {
                trap.OnWaveEnded();
            }
        }
    }
}
