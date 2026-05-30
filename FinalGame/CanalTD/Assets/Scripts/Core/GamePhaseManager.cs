/*
 * File: GamePhaseManager.cs
 *
 * Purpose:
 * Coordinates the high-level match flow: PlayerPhase turns, WavePhase, round
 * progression, final wave detection, and result scene loading.
 *
 * Notes:
 * Other systems notify this manager when turns or waves finish. It also tells
 * towers, traps, build areas, and card systems when phase changes require state
 * resets.
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

    private void Start()
    {
        DealInitialHandsOnce();
        StartPlayerPhase();
    }

    public bool IsPlayerPhase()
    {
        return currentPhase == GamePhase.PlayerPhase;
    }

    public bool IsWavePhase()
    {
        return currentPhase == GamePhase.WavePhase;
    }

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

    private void UpdateTurnIndicator()
    {
        if (CurrentTurnIndicatorManager.Instance != null)
        {
            int currentPlayerId = turnManager != null ? turnManager.currentPlayerId : 0;
            CurrentTurnIndicatorManager.Instance.UpdateCurrentTurnIndicator(currentPlayerId);
        }
    }

    public void StartWavePhase()
    {
        currentPhase = GamePhase.WavePhase;

        if (waveStartCoroutine != null)
        {
            return;
        }

        waveStartCoroutine = StartCoroutine(StartWaveAfterIncomingToast());
    }

    private IEnumerator StartWaveAfterIncomingToast()
    {
        if (waveManager != null)
        {
            waveManager.ShowWaveIncoming(currentWaveIndex);
        }
        else
        {
            ShowToast("Wave " + currentWaveIndex + " incoming!");
        }

        yield return new WaitForSeconds(waveIncomingDelay);

        NotifyTowersWaveStarted();
        NotifyShockTrapsWaveStarted();

        if (waveManager != null)
        {
            waveManager.StartWave();
        }

        waveStartCoroutine = null;
    }

    public void OnEndTurnButtonClicked()
    {
        ITurnSource activeTurnSource = TurnSourceResolver.GetActiveTurnSource(turnManager);

        if (activeTurnSource != null && !object.ReferenceEquals(activeTurnSource, turnManager))
        {
            activeTurnSource.EndCurrentTurn();
            return;
        }

        if (!IsPlayerPhase())
        {
            ShowToast("Enemy wave is running.");
            return;
        }

        if (playerManager != null && !playerManager.IsLastPlayer())
        {
            playerManager.AdvanceToNextPlayer();
            playerTurnToastAlreadyShown = true;
            StartPlayerPhase();
            return;
        }

        StartWavePhase();
    }

    public void OnWaveFinished()
    {
        NotifyTowersWaveEnded();
        NotifyShockTrapsWaveEnded();

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

    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
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

    private CardDrawManager GetCardDrawManager()
    {
        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        return cardDrawManager;
    }

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