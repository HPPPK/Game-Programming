/*
 * File: AIPrototypeTurnManager.cs
 *
 * Purpose:
 * Implements AIPrototypeTurnManager for the core layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Core gameplay managers, player objects, turn systems, or shared scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for AIPrototypeTurnManager within the core system.
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
 * - Verify AIPrototypeTurnManager in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AIPrototypeTurnManager : MonoBehaviour, ITurnSource
{
    public static AIPrototypeTurnManager ActiveInstance { get; private set; }

    [Header("Players")]
    public PlayerManager playerManager;
    public List<PlayerResource> activePlayers = new List<PlayerResource>();
    public int currentActivePlayerIndex = 0;

    [Header("Managers")]
    public TurnManager turnManager;
    public CardDrawManager cardDrawManager;
    public WaveManager waveManager;
    public GamePhaseManager legacyGamePhaseManager;
    public GateOwnershipManager gateOwnershipManager;
    public BuildTowerManager buildTowerManager;
    public MonoBehaviour toastMessage;

    [Header("AI Difficulty")]
    public AIDifficulty[] aiDifficultiesByPlayerId = new AIDifficulty[4];
    public float easyCardUseChance = 0.2f;
    public float mediumCardUseChance = 0.55f;
    public float hardCardUseChance = 0.85f;

    [Header("AI Timing")]
    public float aiThinkDelay = 1.2f;
    public float aiActionMessageDelay = 1.0f;
    public float aiTurnEndDelay = 1.2f;

    [Header("AI Build Settings")]
    public int cannonTowerCost = 6;
    public GameObject[] towerPrefabsByPlayerId = new GameObject[4];
    public Transform towersParent;

    [Header("Shock Trap AI")]
    public Transform routeNodesParent;
    public GameObject shockTrapPrefab;

    [Header("UI")]
    public CanvasGroup normalGameplayUI;

    [Header("Wave")]
    public int currentRound = 1;
    public int currentWaveIndex = 1;
    public int maxWaves = 10;
    public string resultSceneName = "ResultScene";

    private bool isHumanTurnWaiting = false;
    private bool isWaveRunning = false;
    private readonly Dictionary<int, int> negativeCardTargetCountsThisRound = new Dictionary<int, int>();
    private readonly Dictionary<int, int> lastNegativeCardTargetRound = new Dictionary<int, int>();

    public int CurrentPlayerId
    {
        get { return playerManager != null ? playerManager.GetCurrentPlayerId() : -1; }
    }

    public bool IsCurrentPlayerHuman
    {
        get
        {
            PlayerResource player = GetCurrentPlayerResource();
            return player != null && player.playerType == PlayerType.Human;
        }
    }

    public bool CanHumanAct
    {
        get { return isHumanTurnWaiting && !isWaveRunning && IsCurrentPlayerHuman; }
    }

    /// <summary>
    /// Updates turn indicator so the display or cached state matches current gameplay data.
    /// </summary>
    private void UpdateTurnIndicator(int playerId)
    {
        if (CurrentTurnIndicatorManager.Instance != null)
        {
            CurrentTurnIndicatorManager.Instance.UpdateCurrentTurnIndicator(playerId);
        }
        
    }

    /// <summary>
    /// Subscribes AI prototype turn manager to the events it needs while enabled.
    /// </summary>
    private void OnEnable()
    {
        ActiveInstance = this;
        Debug.Log("Using AIPrototypeTurnManager as turn source.");
    }

    /// <summary>
    /// Unsubscribes AI prototype turn manager from events so disabled objects stop receiving callbacks.
    /// </summary>
    private void OnDisable()
    {
        if (ActiveInstance == this)
        {
            ActiveInstance = null;
        }
    }

    /// <summary>
    /// Finds and stores AI prototype turn manager references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        FindMissingReferences();
        DisableLegacyPhaseManager();
        ConnectWaveManager();
    }

    /// <summary>
    /// Sets up AI prototype turn manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        LoadPlayerTypesFromPrefs();
        RebindPlayerStatusPanels();
        BuildActivePlayerList();

        if (cardDrawManager != null)
        {
            cardDrawManager.DealInitialHands();
        }

        StartRoundFromFirstActivePlayer();
    }

    // Rebinds every AI prototype player panel to the real PlayerResource/Castle
    // pair after PlayerPrefs setup has loaded names and player types.
    /// <summary>
    /// Handles rebind player status panels for this gameplay system.
    /// </summary>
    private void RebindPlayerStatusPanels()
    {
        if (playerManager == null || playerManager.players == null)
        {
            return;
        }

        PlayerStatusPanelUI[] statusPanels = FindObjectsOfType<PlayerStatusPanelUI>(true);

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            CastleBase castle = GetCastleForPlayer(player);
            PlayerStatusPanelUI resolvedPanel = player.statusPanel;

            if (resolvedPanel == null && castle != null && castle.statusPanel != null)
            {
                resolvedPanel = castle.statusPanel;
            }

            if (resolvedPanel == null)
            {
                foreach (PlayerStatusPanelUI candidate in statusPanels)
                {
                    if (candidate == null)
                    {
                        continue;
                    }

                    if (candidate.linkedResource == player)
                    {
                        resolvedPanel = candidate;
                        break;
                    }

                    if (castle != null && candidate.linkedCastle == castle)
                    {
                        resolvedPanel = candidate;
                        break;
                    }
                }
            }

            if (castle != null)
            {
                castle.ownerPlayerId = player.playerId;
                castle.ownerResource = player;
                castle.SyncPlayerNameFromOwner();
            }

            if (resolvedPanel != null)
            {
                resolvedPanel.Bind(player, castle);
            }
            else
            {
                player.statusPanel = null;
            }

            player.RefreshUI();
        }

        playerManager.RefreshAllPlayerStatusPanels();
        playerManager.RefreshCurrentPlayerUI();
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

    // Called by the End Turn button during a human player's turn.
    /// <summary>
    /// Responds to on end turn button clicked and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnEndTurnButtonClicked()
    {
        EndHumanTurn();
    }

    // Lets external systems remove a player slot mid-match without rewriting the prototype turn loop.
    /// <summary>
    /// Responds to player exit and updates the affected gameplay or UI systems.
    /// </summary>
    public void HandlePlayerExit(int playerId)
    {
        Debug.Log("AIPrototypeTurnManager handling player exit for player " + playerId + ".");

        bool exitingCurrentPlayer = CurrentPlayerId == playerId;
        BuildActivePlayerList();

        if (activePlayers.Count == 0)
        {
            Debug.Log("AIPrototypeTurnManager has no active players after exit cleanup.");
            isHumanTurnWaiting = false;
            SetHumanControlsEnabled(false);
            return;
        }

        if (!exitingCurrentPlayer)
        {
            if (currentActivePlayerIndex >= activePlayers.Count)
            {
                currentActivePlayerIndex = Mathf.Clamp(currentActivePlayerIndex, 0, activePlayers.Count - 1);
            }

            return;
        }

        currentActivePlayerIndex = Mathf.Clamp(currentActivePlayerIndex, 0, activePlayers.Count - 1);
        StopAllCoroutines();
        isHumanTurnWaiting = false;
        StartCurrentPlayerTurn();
    }

    /// <summary>
    /// Handles are all players inactive or eliminated for this gameplay system.
    /// </summary>
    public bool AreAllPlayersInactiveOrEliminated()
    {
        BuildActivePlayerList();
        return activePlayers.Count == 0;
    }

    // Scene-safe end-turn method for UI buttons in GameScene_AIPrototype.
    /// <summary>
    /// Handles end human turn for this gameplay system.
    /// </summary>
    public void EndHumanTurn()
    {
        EndCurrentTurn();
    }

    /// <summary>
    /// Handles end current turn for this gameplay system.
    /// </summary>
    public void EndCurrentTurn()
    {
        if (!isHumanTurnWaiting || isWaveRunning)
        {
            return;
        }

        isHumanTurnWaiting = false;
        StartCoroutine(AdvanceAfterShortDelay());
    }

    // Called by WaveManager when all enemies are gone.
    /// <summary>
    /// Responds to on wave finished and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnWaveFinished()
    {
        isWaveRunning = false;
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
        StartRoundFromFirstActivePlayer();
    }

    // Ends the AI prototype match after the final configured wave.
    /// <summary>
    /// Handles end game for this gameplay system.
    /// </summary>
    private void EndGame()
    {
        List<PlayerResultEntry> results = GameResultBuilder.BuildRankedResults(playerManager, true);
        GameResultData.SetResults(results);

        if (string.IsNullOrEmpty(resultSceneName))
        {
            Debug.LogWarning("Result scene name is empty.");
            return;
        }

        SceneManager.LoadScene(resultSceneName);
    }

    // Finds optional scene references so the prototype can work with minimal Inspector wiring.
    /// <summary>
    /// Searches scene objects or cached lists to find missing references.
    /// </summary>
    private void FindMissingReferences()
    {
        if (playerManager == null) playerManager = FindObjectOfType<PlayerManager>();
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (cardDrawManager == null) cardDrawManager = FindObjectOfType<CardDrawManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (legacyGamePhaseManager == null) legacyGamePhaseManager = FindObjectOfType<GamePhaseManager>();
        if (gateOwnershipManager == null) gateOwnershipManager = FindObjectOfType<GateOwnershipManager>();
        if (buildTowerManager == null) buildTowerManager = FindObjectOfType<BuildTowerManager>();
    }

    // Prevents the original GamePhaseManager from also running turns in the AI prototype scene.
    /// <summary>
    /// Handles disable legacy phase manager for this gameplay system.
    /// </summary>
    private void DisableLegacyPhaseManager()
    {
        if (legacyGamePhaseManager != null)
        {
            legacyGamePhaseManager.enabled = false;
        }
    }

    // Lets WaveManager report wave completion back to this prototype manager.
    /// <summary>
    /// Handles connect wave manager for enemy movement, health, waves, or routing.
    /// </summary>
    private void ConnectWaveManager()
    {
        if (waveManager == null)
        {
            return;
        }

        waveManager.aiPrototypeTurnManager = this;
        waveManager.gamePhaseManager = null;
    }

    // Reads PlayerPrefs created by ModeSelectScene and assigns each PlayerResource a prototype slot type.
    /// <summary>
    /// Loads player types from prefs from saved settings, room data, or scene references.
    /// </summary>
    private void LoadPlayerTypesFromPrefs()
    {
        if (playerManager == null || playerManager.players == null)
        {
            Debug.LogWarning("AIPrototypeTurnManager has no PlayerManager.");
            return;
        }

        bool hasHumanPlayer = false;
        PlayerResource playerZero = null;

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            if (player.playerId == 0)
            {
                playerZero = player;
            }

            string nameKey = "PlayerName_" + player.playerId;
            bool hasName = PlayerPrefs.HasKey(nameKey) && !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(nameKey));

            if (!hasName)
            {
                player.playerType = PlayerType.Empty;
                Debug.Log("Skipped empty player " + player.playerId);
                continue;
            }

            player.LoadSetupFromPlayerPrefs();

            if (player.playerType == PlayerType.AI)
            {
                LoadAIDifficultyFromPlayerPrefs(player.playerId);
            }

            if (player.playerType == PlayerType.Human)
            {
                hasHumanPlayer = true;
            }
        }

        // The prototype is human-vs-AI, not an all-AI battle. If room data is wrong, force player 0 to Human.
        if (!hasHumanPlayer && playerZero != null)
        {
            playerZero.playerType = PlayerType.Human;

            if (string.IsNullOrWhiteSpace(playerZero.displayName))
            {
                playerZero.displayName = "Player";
            }

            Debug.LogWarning("No Human player found in AI prototype setup. Forced Player 0 to Human.");
        }
    }

    // Rebuilds the active turn list and skips Empty or eliminated players.
    /// <summary>
    /// Builds active player list from configured scene objects and runtime state.
    /// </summary>
    private void BuildActivePlayerList()
    {
        activePlayers.Clear();

        if (playerManager == null || playerManager.players == null)
        {
            return;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player != null && player.playerType != PlayerType.Empty && !player.isEliminated)
            {
                activePlayers.Add(player);
            }
        }
    }

    // Starts a round from the first active Human or AI player.
    /// <summary>
    /// Starts round from first active player and enables its related gameplay flow.
    /// </summary>
    private void StartRoundFromFirstActivePlayer()
    {
        BuildActivePlayerList();
        negativeCardTargetCountsThisRound.Clear();

        if (activePlayers.Count == 0)
        {
            ShowToast("No active players.");
            SetHumanControlsEnabled(false);
            return;
        }

        currentActivePlayerIndex = 0;
        StartCurrentPlayerTurn();
    }

    // Starts a human turn or an automated AI turn based on PlayerType.
    /// <summary>
    /// Starts current player turn and enables its related gameplay flow.
    /// </summary>
    private void StartCurrentPlayerTurn()
    {
        if (activePlayers.Count == 0)
        {
            return;
        }

        PlayerResource player = activePlayers[currentActivePlayerIndex];

        if (playerManager != null)
        {
            playerManager.SetCurrentPlayer(player.playerId);
        }

        if (turnManager != null)
        {
            // TurnManager is synchronized for legacy checks, but it does not drive the AI prototype loop.
            turnManager.currentPlayerId = player.playerId;
            turnManager.StartTurn(player.HasPendingDisrupt());
        }

        if (player.HasPendingDisrupt())
        {
            player.ConsumeDisruptForThisTurn();
            ShowToast(player.GetDisplayName() + " is disrupted this turn.");
        }

        UpdateTurnIndicator(player.playerId);

        if (player.playerType == PlayerType.AI)
        {
            Debug.Log("AI turn started: Player " + player.playerId);
            SetHumanControlsEnabled(false);
            if (cardDrawManager != null) cardDrawManager.RenderCurrentPlayerHand();
            StartCoroutine(RunAITurn(player));
            return;
        }

        Debug.Log("Human turn started: Player " + player.playerId);
        SetHumanControlsEnabled(true);
        isHumanTurnWaiting = true;
        if (cardDrawManager != null) cardDrawManager.RenderCurrentPlayerHand();
    }

    // Runs the full AI turn sequence. Every path ends by advancing the turn.
    /// <summary>
    /// Handles run AI turn for this gameplay system.
    /// </summary>
    private IEnumerator RunAITurn(PlayerResource aiPlayer)
    {
        isHumanTurnWaiting = false;
        AIDifficulty difficulty = GetDifficulty(aiPlayer.playerId);

        ShowToast(aiPlayer.GetDisplayName() + " is thinking...");
        /// <summary>
        /// Handles wait for seconds for AI prototype turn manager.
        /// </summary>
        yield return new WaitForSeconds(aiThinkDelay);

        TryAIDraw(aiPlayer);
        RefreshAIResourceDisplays(aiPlayer);
        /// <summary>
        /// Handles wait for seconds for AI prototype turn manager.
        /// </summary>
        yield return new WaitForSeconds(aiActionMessageDelay);

        bool usedCard = TryAIUseCard(aiPlayer, difficulty);

        if (usedCard)
        {
            /// <summary>
            /// Handles wait for seconds for AI prototype turn manager.
            /// </summary>
            yield return new WaitForSeconds(aiActionMessageDelay);
        }

        bool upgradedTower = TryAIUpgradeOneTower(aiPlayer, difficulty);

        if (upgradedTower)
        {
            /// <summary>
            /// Handles wait for seconds for AI prototype turn manager.
            /// </summary>
            yield return new WaitForSeconds(aiActionMessageDelay);
        }

        bool builtTower = !upgradedTower || difficulty != AIDifficulty.Easy
            ? TryAIBuildOneTower(aiPlayer, difficulty)
            : false;

        if (builtTower)
        {
            /// <summary>
            /// Handles wait for seconds for AI prototype turn manager.
            /// </summary>
            yield return new WaitForSeconds(aiActionMessageDelay);
        }
        else if (TryAIBuyOneLand(aiPlayer, difficulty))
        {
            /// <summary>
            /// Handles wait for seconds for AI prototype turn manager.
            /// </summary>
            yield return new WaitForSeconds(aiActionMessageDelay);
            // Tower construction has higher priority than land banking, so try to build immediately after buying.
            if (TryAIBuildOneTower(aiPlayer, difficulty))
            {
                /// <summary>
                /// Handles wait for seconds for AI prototype turn manager.
                /// </summary>
                yield return new WaitForSeconds(aiActionMessageDelay);
            }
        }

        if (!usedCard && TryAIDiscardWeakCard(aiPlayer, difficulty))
        {
            /// <summary>
            /// Handles wait for seconds for AI prototype turn manager.
            /// </summary>
            yield return new WaitForSeconds(aiActionMessageDelay);
        }

        /// <summary>
        /// Handles wait for seconds for AI prototype turn manager.
        /// </summary>
        yield return new WaitForSeconds(aiTurnEndDelay);
        Debug.Log("AI turn ended: Player " + aiPlayer.playerId);
        AdvanceToNextTurnOrWave();
    }

    // Returns the active PlayerResource using the prototype source of truth.
    /// <summary>
    /// Returns current player resource from the current scene or gameplay state.
    /// </summary>
    private PlayerResource GetCurrentPlayerResource()
    {
        if (playerManager == null)
        {
            return null;
        }

        return playerManager.GetCurrentPlayerResource();
    }

    // Uses the normal draw limit when possible, so AI follows the same turn rules.
    /// <summary>
    /// Attempts to AI draw and returns false if rules, resources, or references block it.
    /// </summary>
    private void TryAIDraw(PlayerResource aiPlayer)
    {
        if (cardDrawManager == null)
        {
            Debug.LogWarning("AI cannot draw because CardDrawManager is missing.");
            return;
        }

        cardDrawManager.TryDrawForPlayer(aiPlayer.playerId, true);
    }

    // Chooses one card from hand based on difficulty and simple board conditions.
    /// <summary>
    /// Attempts to AI use card and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIUseCard(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        if (turnManager == null || turnManager.IsCardActionsBlockedThisTurn())
        {
            return false;
        }

        PlayerHand hand = aiPlayer.GetPlayerHand();

        if (hand == null || hand.GetCardCount() <= 0)
        {
            return false;
        }

        List<GameObject> cards = new List<GameObject>(hand.GetCards());
        cards.Sort((a, b) => GetCardPriority(b, aiPlayer, difficulty).CompareTo(GetCardPriority(a, aiPlayer, difficulty)));

        int bestPriority = cards.Count > 0 && cards[0] != null
            ? GetCardPriority(cards[0], aiPlayer, difficulty)
            : 0;

        if (bestPriority < GetMinimumCardPriorityToUse(difficulty))
        {
            return false;
        }

        if (bestPriority < 90 && Random.value > GetCardUseChance(difficulty))
        {
            return false;
        }

        foreach (GameObject cardPrefab in cards)
        {
            if (cardPrefab == null)
            {
                continue;
            }

            if (TryResolveAICard(aiPlayer, cardPrefab, difficulty))
            {
                hand.RemoveCard(cardPrefab);
                aiPlayer.SyncCardCountFromHand();
                RefreshAIResourceDisplays(aiPlayer);
                return true;
            }
        }

        return false;
    }

    // Discards clearly weak cards only when the hand is full and the AI did not find a useful play.
    /// <summary>
    /// Attempts to AI discard weak card and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIDiscardWeakCard(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        if (turnManager == null || turnManager.IsCardActionsBlockedThisTurn())
        {
            return false;
        }

        PlayerHand hand = aiPlayer.GetPlayerHand();

        if (hand == null || hand.GetCardCount() < hand.maxHandSize)
        {
            return false;
        }

        GameObject weakestCard = null;
        int weakestPriority = int.MaxValue;

        foreach (GameObject cardPrefab in hand.GetCards())
        {
            if (cardPrefab == null)
            {
                continue;
            }

            int priority = GetCardPriority(cardPrefab, aiPlayer, difficulty);

            if (priority < weakestPriority)
            {
                weakestPriority = priority;
                weakestCard = cardPrefab;
            }
        }

        if (weakestCard == null || weakestPriority > GetMaximumDiscardPriority(difficulty))
        {
            return false;
        }

        if (!turnManager.TryConsumeDiscard())
        {
            return false;
        }

        hand.RemoveCard(weakestCard);
        aiPlayer.SyncCardCountFromHand();
        RefreshAIResourceDisplays(aiPlayer);
        ShowToast(aiPlayer.GetDisplayName() + " discarded a weak card.");
        return true;
    }

    // Returns a rough priority so strategic cards are considered before weak or unusable cards.
    /// <summary>
    /// Returns card priority used by card handling or target selection.
    /// </summary>
    private int GetCardPriority(GameObject cardPrefab, PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        string cardName = NormalizeCardName(cardPrefab.name);

        if (cardName.Contains("powerboost")) return HasOwnedTower(aiPlayer.playerId) ? 90 : 5;
        if (cardName.Contains("takeover")) return HasAffordableTakeOverTarget(aiPlayer) ? 85 : 5;
        if (cardName.Contains("lockgate")) return HasValidAIGateTarget(aiPlayer, GateActionType.LockGate) ? 82 : 5;
        if (cardName.Contains("shocktrap")) return shockTrapPrefab != null ? 75 : 5;
        if (cardName.Contains("disrupt")) return FindBalancedDisruptTarget(aiPlayer) != null ? 70 : 5;
        if (cardName.Contains("opengate")) return HasValidAIGateTarget(aiPlayer, GateActionType.OpenGate) ? 68 : 5;
        if (cardName.Contains("stealcard")) return FindPlayerWithMostCards(aiPlayer) != null ? 60 : 5;
        if (cardName.Contains("freezeclaim")) return FindBestFreezeTarget(aiPlayer, difficulty) != null ? 55 : 5;
        if (cardName.Contains("tradehands")) return FindTradeHandsTarget(aiPlayer) != null ? 45 : 5;

        return 1;
    }

    // Applies supported AI card effects directly, without using visual targeting UI.
    /// <summary>
    /// Attempts to resolve AI card and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryResolveAICard(PlayerResource aiPlayer, GameObject cardPrefab, AIDifficulty difficulty)
    {
        if (turnManager == null || !turnManager.CanPlayCard())
        {
            return false;
        }

        string cardName = NormalizeCardName(cardPrefab.name);
        bool success = false;

        if (cardName.Contains("powerboost")) success = TryAIPowerBoost(aiPlayer, difficulty);
        else if (cardName.Contains("freezeclaim")) success = TryAIFreezeClaim(aiPlayer, difficulty);
        else if (cardName.Contains("takeover")) success = TryAITakeOver(aiPlayer, difficulty);
        else if (cardName.Contains("shocktrap")) success = TryAIShockTrap(aiPlayer, difficulty);
        else if (cardName.Contains("lockgate")) success = TryAIGateCard(aiPlayer, difficulty, GateActionType.LockGate);
        else if (cardName.Contains("opengate")) success = TryAIGateCard(aiPlayer, difficulty, GateActionType.OpenGate);
        else if (cardName.Contains("disrupt")) success = TryAIDisrupt(aiPlayer);
        else if (cardName.Contains("stealcard")) success = TryAIStealCard(aiPlayer);
        else if (cardName.Contains("tradehands")) success = TryAITradeHands(aiPlayer);

        if (!success)
        {
            return false;
        }

        bool gateActionCard = cardName.Contains("lockgate") || cardName.Contains("opengate");
        /// <summary>
        /// Handles consume AI card after resolution for AI prototype turn manager.
        /// </summary>
        return ConsumeAICardAfterResolution(aiPlayer, cardPrefab, !gateActionCard);
    }

    // Power Boost targets the best owned tower and boosts it for the next wave.
    /// <summary>
    /// Attempts to AI power boost and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIPowerBoost(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        TowerTargetingManager towerTargetingManager = FindObjectOfType<TowerTargetingManager>();

        if (towerTargetingManager == null)
        {
            return false;
        }

        CannonTower bestTower = null;
        float bestScore = float.MinValue;

        foreach (CannonTower tower in FindObjectsOfType<CannonTower>())
        {
            if (tower == null ||
                tower.ownerPlayerId != aiPlayer.playerId ||
                tower.boostActive ||
                tower.boostPendingForNextWave ||
                tower.IsDisabledByFreeze())
            {
                continue;
            }

            float score = ScoreWorldPosition(tower.transform.position, difficulty);
            if (score > bestScore)
            {
                bestScore = score;
                bestTower = tower;
            }
        }

        if (bestTower == null)
        {
            return false;
        }

        if (!towerTargetingManager.ResolvePowerBoost(aiPlayer.playerId, bestTower))
        {
            return false;
        }

        ShowAICardAnnouncement(aiPlayer, "Power Boost", "their tower");
        return true;
    }

    // Freeze Claim blocks a valuable claimable tile until the AI's next turn.
    /// <summary>
    /// Attempts to AI freeze claim and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIFreezeClaim(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        TileTargetingManager tileTargetingManager = FindObjectOfType<TileTargetingManager>();

        if (tileTargetingManager == null)
        {
            return false;
        }

        TowerBuildArea target = FindBestFreezeTarget(aiPlayer, difficulty);

        if (target == null)
        {
            return false;
        }

        if (!tileTargetingManager.ResolveFreezeClaim(aiPlayer.playerId, target))
        {
            return false;
        }

        RegisterLandNegativeTarget(aiPlayer, target);
        ShowAICardAnnouncement(aiPlayer, "Freeze Claim", "a land tile");
        return true;
    }

    // Take Over buys an opponent land at the card discount and removes its tower.
    /// <summary>
    /// Attempts to AI take over and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAITakeOver(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        TileTargetingManager tileTargetingManager = FindObjectOfType<TileTargetingManager>();

        if (tileTargetingManager == null)
        {
            return false;
        }

        TowerBuildArea target = FindBestTakeOverTarget(aiPlayer, difficulty);

        if (target == null)
        {
            return false;
        }

        if (!tileTargetingManager.ResolveTakeOver(aiPlayer.playerId, target))
        {
            return false;
        }

        RegisterLandNegativeTarget(aiPlayer, target);
        ShowAICardAnnouncement(aiPlayer, "Take Over", "an opponent's land");
        return true;
    }

    // Shock Trap places a trap on a strong route node so it can trigger in a future wave.
    /// <summary>
    /// Attempts to AI shock trap and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIShockTrap(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        ShockTrapTargetingManager shockTrapTargetingManager = FindObjectOfType<ShockTrapTargetingManager>();

        if (shockTrapTargetingManager == null)
        {
            return false;
        }

        PathNode node = FindBestTrapNode(difficulty);

        if (node == null)
        {
            return false;
        }

        if (!shockTrapTargetingManager.ResolveShockTrapPlacement(aiPlayer.playerId, node))
        {
            return false;
        }

        ShowAICardAnnouncement(aiPlayer, "Shock Trap", "the path");
        return true;
    }

    // Disrupt targets a valid opponent while avoiding repeated focus on the same player.
    /// <summary>
    /// Attempts to AI disrupt and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIDisrupt(PlayerResource aiPlayer)
    {
        PlayerTargetingManager playerTargetingManager = FindObjectOfType<PlayerTargetingManager>();

        if (playerTargetingManager == null)
        {
            return false;
        }

        PlayerResource target = FindBalancedDisruptTarget(aiPlayer);

        if (target == null || target.HasPendingDisrupt())
        {
            return false;
        }

        if (!playerTargetingManager.ResolveDisrupt(aiPlayer.playerId, target.playerId))
        {
            return false;
        }

        RegisterNegativeCardTarget(target);
        ShowAICardAnnouncement(aiPlayer, "Disrupt", target.GetDisplayName());
        return true;
    }

    // Steal Card takes one random card from the opponent with the largest hand.
    /// <summary>
    /// Attempts to AI steal card and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIStealCard(PlayerResource aiPlayer)
    {
        PlayerTargetingManager playerTargetingManager = FindObjectOfType<PlayerTargetingManager>();

        if (playerTargetingManager == null)
        {
            return false;
        }

        PlayerResource target = FindPlayerWithMostCards(aiPlayer);
        PlayerHand aiHand = aiPlayer.GetPlayerHand();

        if (target == null || aiHand == null || !aiHand.CanAddCard())
        {
            return false;
        }

        if (!playerTargetingManager.ResolveStealCard(aiPlayer.playerId, target.playerId))
        {
            return false;
        }

        RegisterNegativeCardTarget(target);
        ShowAICardAnnouncement(aiPlayer, "Steal Card", target.GetDisplayName());
        return true;
    }

    // Trade Hands swaps with a target only when the AI has fewer cards.
    /// <summary>
    /// Attempts to AI trade hands and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAITradeHands(PlayerResource aiPlayer)
    {
        PlayerTargetingManager playerTargetingManager = FindObjectOfType<PlayerTargetingManager>();

        if (playerTargetingManager == null)
        {
            return false;
        }

        PlayerResource target = FindTradeHandsTarget(aiPlayer);

        if (target == null)
        {
            return false;
        }

        if (!playerTargetingManager.ResolveTradeHands(aiPlayer.playerId, target.playerId))
        {
            return false;
        }

        RegisterNegativeCardTarget(target);
        ShowAICardAnnouncement(aiPlayer, "Trade Hands", target.GetDisplayName());
        return true;
    }

    // Gate changes are card-only. AI uses this only after resolving an actual Open Gate or Lock Gate card.
    /// <summary>
    /// Attempts to AI gate card and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIGateCard(PlayerResource aiPlayer, AIDifficulty difficulty, GateActionType actionType)
    {
        List<GateFrameAnimation> validGates = GetValidAIGateTargets(aiPlayer, actionType);

        if (validGates.Count == 0)
        {
            return false;
        }

        GateFrameAnimation selectedGate = difficulty == AIDifficulty.Easy
            ? validGates[Random.Range(0, validGates.Count)]
            : GetBestGateByPosition(validGates, difficulty);

        GateTargetingManager gateTargetingManager = GateTargetingManager.Instance != null
            ? GateTargetingManager.Instance
            : FindObjectOfType<GateTargetingManager>();

        if (gateTargetingManager == null)
        {
            return false;
        }

        bool changed = actionType == GateActionType.OpenGate
            ? gateTargetingManager.TryOpenGateForPlayer(aiPlayer.playerId, selectedGate, true, false)
            : gateTargetingManager.TryLockGateForPlayer(aiPlayer.playerId, selectedGate, true, false);

        if (!changed)
        {
            return false;
        }

        string cardName = actionType == GateActionType.LockGate ? "Lock Gate" : "Open Gate";
        ShowAICardAnnouncement(aiPlayer, cardName, "a gate");
        return true;
    }

    /// <summary>
    /// Checks whether valid AI gate target is present before the code depends on it.
    /// </summary>
    private bool HasValidAIGateTarget(PlayerResource aiPlayer, GateActionType actionType)
    {
        return GetValidAIGateTargets(aiPlayer, actionType).Count > 0;
    }

    /// <summary>
    /// Returns valid AI gate targets used by card handling or target selection.
    /// </summary>
    private List<GateFrameAnimation> GetValidAIGateTargets(PlayerResource aiPlayer, GateActionType actionType)
    {
        GateTargetingManager gateTargetingManager = GateTargetingManager.Instance != null
            ? GateTargetingManager.Instance
            : FindObjectOfType<GateTargetingManager>();

        if (aiPlayer == null || gateTargetingManager == null)
        {
            return new List<GateFrameAnimation>();
        }

        // Reuse the human targeting rules: public gates, owned linked land, lock path-safety, and gate state.
        return gateTargetingManager.GetValidGatesForPlayer(actionType, aiPlayer.playerId);
    }

    // Keeps all visible resource panels correct after AI-only hand changes.
    /// <summary>
    /// Refreshes AI resource displays from the latest gameplay data.
    /// </summary>
    private void RefreshAIResourceDisplays(params PlayerResource[] changedPlayers)
    {
        if (changedPlayers != null)
        {
            foreach (PlayerResource player in changedPlayers)
            {
                if (player != null)
                {
                    player.RefreshUI();
                }
            }
        }

        if (playerManager != null)
        {
            playerManager.RefreshAllPlayerStatusPanels();
        }

        foreach (PlayerStatusPanelUI panel in FindObjectsOfType<PlayerStatusPanelUI>())
        {
            if (panel != null)
            {
                panel.Refresh();
            }
        }

        // In AI Prototype mode this redraws the local human hand only, not AI hands.
        if (cardDrawManager != null)
        {
            cardDrawManager.RenderCurrentPlayerHand();
        }
    }

    // AI buys one affordable unowned claimable land, scored by difficulty.
    // It reserves tower gold whenever possible because building towers is higher priority than buying land.
    /// <summary>
    /// Attempts to AI buy one land and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIBuyOneLand(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        if (buildTowerManager == null)
        {
            return false;
        }

        List<TowerBuildArea> candidates = new List<TowerBuildArea>();
        int minimumTowerCost = GetMinimumAffordableTowerCost(aiPlayer.playerId);

        foreach (TowerBuildArea area in FindObjectsOfType<TowerBuildArea>())
        {
            if (area == null)
            {
                continue;
            }

            bool canStillBuildAfterBuying = aiPlayer.money - area.landPurchaseCost >= minimumTowerCost;

            if (area.IsClaimable() &&
                area.IsUnowned() &&
                !area.IsFrozen() &&
                aiPlayer.CanAfford(area.landPurchaseCost) &&
                (canStillBuildAfterBuying || !HasAnyBuildableTowerArea(aiPlayer.playerId)))
            {
                candidates.Add(area);
            }
        }

        TowerBuildArea bestArea = ChooseBuildArea(candidates, difficulty, aiPlayer.money, true);

        if (bestArea == null || !buildTowerManager.TryBuyLandForPlayer(bestArea, aiPlayer.playerId))
        {
            return false;
        }
        ShowToast(aiPlayer.GetDisplayName() + " bought land.");
        return true;
    }

    // AI upgrades a strong owned tower before buying more land. Higher difficulty upgrades more deliberately.
    /// <summary>
    /// Attempts to AI upgrade one tower and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIUpgradeOneTower(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        if (buildTowerManager == null)
        {
            return false;
        }

        List<TowerBuildArea> candidates = new List<TowerBuildArea>();

        foreach (TowerBuildArea area in FindObjectsOfType<TowerBuildArea>())
        {
            TowerStats stats = GetTowerStatsFromArea(area);

            if (area == null ||
                stats == null ||
                area.towerOwnerPlayerId != aiPlayer.playerId ||
                !stats.CanUpgrade() ||
                area.IsFrozen())
            {
                continue;
            }

            if (aiPlayer.CanAfford(stats.GetUpgradeCost()))
            {
                candidates.Add(area);
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        if (difficulty == AIDifficulty.Easy && Random.value > 0.25f)
        {
            return false;
        }

        if (difficulty == AIDifficulty.Medium && Random.value > 0.65f)
        {
            return false;
        }

        TowerBuildArea bestArea = ChooseTowerUpgradeArea(candidates, difficulty);

        if (bestArea == null)
        {
            return false;
        }

        bool upgraded = buildTowerManager.TryUpgradeTowerForAI(bestArea, aiPlayer.playerId);

        if (upgraded)
        {
            ShowToast(aiPlayer.GetDisplayName() + " upgraded a tower.");
        }

        return upgraded;
    }

    // AI builds one tower on an owned active empty tile using the prefab for its playerId.
    /// <summary>
    /// Attempts to AI build one tower and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIBuildOneTower(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        if (buildTowerManager != null && GetAffordableTowerTypes(aiPlayer.playerId, aiPlayer.money).Count == 0)
        {
            return false;
        }

        if (buildTowerManager == null && !aiPlayer.CanAfford(cannonTowerCost))
        {
            return false;
        }

        List<TowerBuildArea> candidates = new List<TowerBuildArea>();

        foreach (TowerBuildArea area in FindObjectsOfType<TowerBuildArea>())
        {
            if (area != null &&
                !area.isOccupied &&
                !area.IsFrozen() &&
                area.CanBuildTower(aiPlayer.playerId))
            {
                candidates.Add(area);
            }
        }

        TowerBuildArea bestArea = ChooseBuildArea(candidates, difficulty, aiPlayer.money, false);

        if (bestArea == null)
        {
            return false;
        }

        if (buildTowerManager != null)
        {
            TowerType selectedType = ChooseTowerTypeForArea(bestArea, aiPlayer, difficulty);
            bool built = buildTowerManager.TryBuildTowerForAI(bestArea, selectedType, aiPlayer.playerId);

            if (built)
            {
                ShowToast(aiPlayer.GetDisplayName() + " built a " + selectedType + " tower.");
            }

            return built;
        }

        /// <summary>
        /// Attempts to AI build one tower legacy and reports whether it succeeded.
        /// </summary>
        return TryAIBuildOneTowerLegacy(aiPlayer, bestArea);
    }

    /// <summary>
    /// Handles choose tower type for area for land ownership, tower actions, or build UI.
    /// </summary>
    private TowerType ChooseTowerTypeForArea(TowerBuildArea area, PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        List<TowerType> affordableTypes = GetAffordableTowerTypes(aiPlayer.playerId, aiPlayer.money);

        if (affordableTypes.Count == 0)
        {
            return TowerType.Cannon;
        }

        if (difficulty == AIDifficulty.Easy)
        {
            /// <summary>
            /// Handles choose easy tower type for AI prototype turn manager.
            /// </summary>
            return ChooseEasyTowerType(affordableTypes);
        }

        TowerType bestType = affordableTypes[0];
        float bestScore = float.MinValue;

        foreach (TowerType towerType in affordableTypes)
        {
            float score = ScoreTowerTypeForArea(towerType, area, difficulty, aiPlayer.playerId);

            if (score > bestScore)
            {
                bestScore = score;
                bestType = towerType;
            }
        }

        return bestType;
    }

    /// <summary>
    /// Handles choose easy tower type for land ownership, tower actions, or build UI.
    /// </summary>
    private TowerType ChooseEasyTowerType(List<TowerType> affordableTypes)
    {
        if (affordableTypes == null || affordableTypes.Count == 0)
        {
            return TowerType.Cannon;
        }

        // Easy AI is intentionally simple but no longer builds only Cannon.
        TowerType[] preferenceBag =
        {
            TowerType.Cannon,
            TowerType.Cannon,
            TowerType.Archer,
            TowerType.Archer,
            TowerType.Frost,
            TowerType.Shock,
            TowerType.Sniper
        };

        for (int attempt = 0; attempt < preferenceBag.Length; attempt++)
        {
            TowerType candidate = preferenceBag[Random.Range(0, preferenceBag.Length)];

            if (affordableTypes.Contains(candidate))
            {
                return candidate;
            }
        }

        return affordableTypes[Random.Range(0, affordableTypes.Count)];
    }

    /// <summary>
    /// Returns affordable tower types used by land, tower, cost, or build decisions.
    /// </summary>
    private List<TowerType> GetAffordableTowerTypes(int playerId, int gold)
    {
        List<TowerType> affordableTypes = new List<TowerType>();

        if (buildTowerManager == null)
        {
            if (gold >= cannonTowerCost)
            {
                affordableTypes.Add(TowerType.Cannon);
            }

            return affordableTypes;
        }

        TowerType[] allTypes =
        {
            TowerType.Cannon,
            TowerType.Archer,
            TowerType.Frost,
            TowerType.Shock,
            TowerType.Sniper
        };

        foreach (TowerType towerType in allTypes)
        {
            if (gold >= buildTowerManager.GetTowerCost(towerType, playerId))
            {
                affordableTypes.Add(towerType);
            }
        }

        return affordableTypes;
    }

    /// <summary>
    /// Returns minimum affordable tower cost used by land, tower, cost, or build decisions.
    /// </summary>
    private int GetMinimumAffordableTowerCost(int playerId)
    {
        if (buildTowerManager == null)
        {
            return cannonTowerCost;
        }

        int minCost = int.MaxValue;
        TowerType[] allTypes =
        {
            TowerType.Cannon,
            TowerType.Archer,
            TowerType.Frost,
            TowerType.Shock,
            TowerType.Sniper
        };

        foreach (TowerType towerType in allTypes)
        {
            minCost = Mathf.Min(minCost, buildTowerManager.GetTowerCost(towerType, playerId));
        }

        return minCost == int.MaxValue ? cannonTowerCost : minCost;
    }

    /// <summary>
    /// Handles score tower type for area for land ownership, tower actions, or build UI.
    /// </summary>
    private float ScoreTowerTypeForArea(TowerType towerType, TowerBuildArea area, AIDifficulty difficulty, int playerId)
    {
        float routeDistance = DistanceToNearestRouteNode(area != null ? area.transform.position : Vector3.zero);
        int routeDensity = CountRouteNodesNear(area != null ? area.transform.position : Vector3.zero, 4f);
        float score = 10f;

        if (towerType == TowerType.Cannon)
        {
            score += routeDensity * 5f;
            score += Mathf.Max(0f, 16f - routeDistance);
        }
        else if (towerType == TowerType.Archer)
        {
            score += 18f;
            score += Mathf.Max(0f, 10f - routeDistance);
        }
        else if (towerType == TowerType.Frost)
        {
            score += routeDensity * 3f;
            score += Mathf.Max(0f, 18f - routeDistance);
        }
        else if (towerType == TowerType.Shock)
        {
            score += routeDensity * 7f;
            score += difficulty == AIDifficulty.Hard ? 12f : 0f;
        }
        else if (towerType == TowerType.Sniper)
        {
            score += CountCastlesNear(area != null ? area.transform.position : Vector3.zero, 8f) * 4f;
            score += difficulty == AIDifficulty.Hard ? 10f : 0f;
        }

        if (difficulty == AIDifficulty.Medium && (towerType == TowerType.Shock || towerType == TowerType.Sniper))
        {
            score -= 12f;
        }

        score -= CountOwnedTowersOfType(towerType, playerId) * 2f;
        return score;
    }

    /// <summary>
    /// Handles count owned towers of type for land ownership, tower actions, or build UI.
    /// </summary>
    private int CountOwnedTowersOfType(TowerType towerType, int playerId)
    {
        int count = 0;

        foreach (TowerBuildArea area in FindObjectsOfType<TowerBuildArea>())
        {
            TowerStats stats = GetTowerStatsFromArea(area);

            if (area != null && stats != null && area.towerOwnerPlayerId == playerId && stats.towerType == towerType)
            {
                count += 1;
            }
        }

        return count;
    }

    /// <summary>
    /// Handles choose tower upgrade area for land ownership, tower actions, or build UI.
    /// </summary>
    private TowerBuildArea ChooseTowerUpgradeArea(List<TowerBuildArea> candidates, AIDifficulty difficulty)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        if (difficulty == AIDifficulty.Easy)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        TowerBuildArea bestArea = null;
        float bestScore = float.MinValue;

        foreach (TowerBuildArea area in candidates)
        {
            float score = ScoreTowerForUpgrade(area, difficulty);

            if (score > bestScore)
            {
                bestScore = score;
                bestArea = area;
            }
        }

        return bestArea;
    }

    /// <summary>
    /// Handles score tower for upgrade for land ownership, tower actions, or build UI.
    /// </summary>
    private float ScoreTowerForUpgrade(TowerBuildArea area, AIDifficulty difficulty)
    {
        TowerStats stats = GetTowerStatsFromArea(area);

        if (area == null || stats == null)
        {
            return float.MinValue;
        }

        float score = ScoreBuildArea(area, difficulty);
        score += stats.damage * 4f;
        score += stats.range * 2f;
        score += stats.level * 6f;

        if (stats.towerType == TowerType.Cannon || stats.towerType == TowerType.Shock)
        {
            score += CountRouteNodesNear(area.transform.position, 4f) * 4f;
        }

        if (stats.towerType == TowerType.Frost)
        {
            score += Mathf.Max(0f, 18f - DistanceToNearestRouteNode(area.transform.position));
        }

        if (difficulty == AIDifficulty.Hard)
        {
            score += CountRouteNodesNear(area.transform.position, 5f) * 3f;
        }

        return score;
    }

    /// <summary>
    /// Returns tower stats from area used by land, tower, cost, or build decisions.
    /// </summary>
    private TowerStats GetTowerStatsFromArea(TowerBuildArea area)
    {
        if (area == null || area.currentTower == null)
        {
            return null;
        }

        TowerStats stats = area.currentTower.GetComponent<TowerStats>();
        return stats != null ? stats : area.currentTower.GetComponentInChildren<TowerStats>();
    }

    /// <summary>
    /// Attempts to AI build one tower legacy and returns false if rules, resources, or references block it.
    /// </summary>
    private bool TryAIBuildOneTowerLegacy(PlayerResource aiPlayer, TowerBuildArea bestArea)
    {
        GameObject towerPrefab = GetTowerPrefabForPlayerId(aiPlayer.playerId);

        if (towerPrefab == null || !aiPlayer.SpendMoney(cannonTowerCost))
        {
            return false;
        }

        Transform spawnPoint = bestArea.towerSpawnPoint != null ? bestArea.towerSpawnPoint : bestArea.transform;
        GameObject towerObject = towersParent != null
            ? Instantiate(towerPrefab, spawnPoint.position, Quaternion.identity, towersParent)
            : Instantiate(towerPrefab, spawnPoint.position, Quaternion.identity);

        CannonTower tower = towerObject.GetComponent<CannonTower>();

        if (tower == null)
        {
            tower = towerObject.GetComponentInChildren<CannonTower>();
        }

        if (tower != null)
        {
            tower.ownerPlayerId = aiPlayer.playerId;
            tower.ownerResource = aiPlayer;
            tower.ApplyOwnerVisual(playerManager, aiPlayer.playerId, false);
        }

        bestArea.SetTower(towerObject);
        ShowToast(aiPlayer.GetDisplayName() + " built a tower.");
        return true;
    }

    /// <summary>
    /// Consumes AI card after resolution and records that the player has used that action.
    /// </summary>
    private bool ConsumeAICardAfterResolution(PlayerResource aiPlayer, GameObject cardPrefab, bool consumePlayAction)
    {
        if (cardDrawManager == null || aiPlayer == null || cardPrefab == null)
        {
            return false;
        }

        bool consumed = cardDrawManager.TryConsumeDirectPlayedCard(aiPlayer.playerId, cardPrefab, consumePlayAction);

        if (consumed)
        {
            RefreshAIResourceDisplays(aiPlayer);
        }

        return consumed;
    }

    // Checks whether the AI has an existing valid place to build before spending gold on more land.
    /// <summary>
    /// Checks whether any buildable tower area is present before the code depends on it.
    /// </summary>
    private bool HasAnyBuildableTowerArea(int playerId)
    {
        foreach (TowerBuildArea area in FindObjectsOfType<TowerBuildArea>())
        {
            if (area != null &&
                !area.isOccupied &&
                !area.IsFrozen() &&
                area.CanBuildTower(playerId))
            {
                return true;
            }
        }

        return false;
    }

    // Chooses a land/tower area with random, medium, or stronger heuristic behavior.
    /// <summary>
    /// Handles choose build area for land ownership, tower actions, or build UI.
    /// </summary>
    private TowerBuildArea ChooseBuildArea(List<TowerBuildArea> candidates, AIDifficulty difficulty, int aiGold, bool buyingLand)
    {
        /// <summary>
        /// Handles choose build area for AI prototype turn manager.
        /// </summary>
        return ChooseBuildArea(candidates, difficulty, aiGold, buyingLand, null);
    }

    // Chooses a land/tower area and optionally reduces score for repeatedly targeting the same opponent's land.
    private TowerBuildArea ChooseBuildArea(
        List<TowerBuildArea> candidates,
        AIDifficulty difficulty,
        int aiGold,
        bool buyingLand,
        PlayerResource actingPlayer)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        if (difficulty == AIDifficulty.Easy)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        TowerBuildArea bestArea = null;
        float bestScore = float.MinValue;

        foreach (TowerBuildArea area in candidates)
        {
            float score = ScoreBuildArea(area, difficulty);

            if (buyingLand && aiGold <= area.landPurchaseCost + GetMinimumAffordableTowerCost(actingPlayer != null ? actingPlayer.playerId : CurrentPlayerId))
            {
                score -= area.landPurchaseCost * 0.5f;
            }

            if (actingPlayer != null &&
                area.isOwned &&
                area.ownerPlayerId != actingPlayer.playerId)
            {
                score -= GetNegativeCardTargetCount(area.ownerPlayerId) * 40f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestArea = area;
            }
        }

        return bestArea;
    }

    // Scores a build area using route proximity, linked gates, center control, and occupancy potential.
    /// <summary>
    /// Handles score build area for land ownership, tower actions, or build UI.
    /// </summary>
    private float ScoreBuildArea(TowerBuildArea area, AIDifficulty difficulty)
    {
        if (area == null)
        {
            return float.MinValue;
        }

        float score = 0f;
        score += area.linkedGates != null ? area.linkedGates.Count * 12f : 0f;
        score += Mathf.Max(0f, 30f - DistanceToNearestRouteNode(area.transform.position));
        score += Mathf.Max(0f, 8f - Vector2.Distance(Vector2.zero, area.transform.position));

        if (area.towerSpawnPoint != null)
        {
            score += 5f;
        }

        if (difficulty == AIDifficulty.Hard)
        {
            score += CountRouteNodesNear(area.transform.position, 4f) * 3f;
            score += CountCastlesNear(area.transform.position, 6f) * 2f;
        }

        return score;
    }

    // Scores any map position with route and center heuristics.
    /// <summary>
    /// Handles score world position for this gameplay system.
    /// </summary>
    private float ScoreWorldPosition(Vector3 position, AIDifficulty difficulty)
    {
        float score = Mathf.Max(0f, 30f - DistanceToNearestRouteNode(position));
        score += Mathf.Max(0f, 8f - Vector2.Distance(Vector2.zero, position));

        if (difficulty == AIDifficulty.Hard)
        {
            score += CountRouteNodesNear(position, 4f) * 3f;
        }

        return score;
    }

    // Finds a strong freeze target: opponent-owned or high-value unowned claimable land.
    /// <summary>
    /// Searches scene objects or cached lists to find best freeze target.
    /// </summary>
    private TowerBuildArea FindBestFreezeTarget(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        List<TowerBuildArea> candidates = new List<TowerBuildArea>();

        foreach (TowerBuildArea area in FindObjectsOfType<TowerBuildArea>())
        {
            if (area != null && area.CanBeFrozen())
            {
                candidates.Add(area);
            }
        }

        /// <summary>
        /// Handles choose build area for AI prototype turn manager.
        /// </summary>
        return ChooseBuildArea(candidates, difficulty, aiPlayer.money, false, aiPlayer);
    }

    // Finds an affordable opponent tile worth taking over.
    /// <summary>
    /// Searches scene objects or cached lists to find best take over target.
    /// </summary>
    private TowerBuildArea FindBestTakeOverTarget(PlayerResource aiPlayer, AIDifficulty difficulty)
    {
        List<TowerBuildArea> candidates = new List<TowerBuildArea>();

        foreach (TowerBuildArea area in FindObjectsOfType<TowerBuildArea>())
        {
            if (area != null &&
                area.CanBeTakenOverBy(aiPlayer.playerId) &&
                aiPlayer.CanAfford(area.GetTakeOverCardCost()))
            {
                candidates.Add(area);
            }
        }

        /// <summary>
        /// Handles choose build area for AI prototype turn manager.
        /// </summary>
        return ChooseBuildArea(candidates, difficulty, aiPlayer.money, false, aiPlayer);
    }

    // Finds the best route node for a shock trap.
    /// <summary>
    /// Searches scene objects or cached lists to find best trap node.
    /// </summary>
    private PathNode FindBestTrapNode(AIDifficulty difficulty)
    {
        PathNode[] nodes = routeNodesParent != null
            ? routeNodesParent.GetComponentsInChildren<PathNode>(true)
            : FindObjectsOfType<PathNode>();

        PathNode bestNode = null;
        float bestScore = float.MinValue;

        foreach (PathNode node in nodes)
        {
            if (node == null || node.GetComponent<CastleEndNode>() != null || node.GetComponent<EnemySpawner>() != null)
            {
                continue;
            }

            float score = ScoreWorldPosition(node.transform.position, difficulty);
            score += node.edges != null ? node.edges.Count * 2f : 0f;

            if (score > bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        return bestNode;
    }

    // Finds a disrupt target by score while spreading harmful card pressure across players.
    /// <summary>
    /// Searches scene objects or cached lists to find balanced disrupt target.
    /// </summary>
    private PlayerResource FindBalancedDisruptTarget(PlayerResource aiPlayer)
    {
        return FindBalancedOpponentTarget(
            aiPlayer,
            player => !player.HasPendingDisrupt(),
            player => player.score
        );
    }

    // Finds the opponent with the highest score.
    /// <summary>
    /// Returns leading opponent from the current scene or gameplay state.
    /// </summary>
    private PlayerResource GetLeadingOpponent(PlayerResource aiPlayer)
    {
        /// <summary>
        /// Handles find balanced opponent target for AI prototype turn manager.
        /// </summary>
        return FindBalancedOpponentTarget(aiPlayer, player => true, player => player.score);
    }

    // Finds an opponent using score as the tie-breaker while avoiding dogpiling one target.
    private PlayerResource FindBalancedOpponentTarget(
        PlayerResource aiPlayer,
        System.Func<PlayerResource, bool> isValid,
        System.Func<PlayerResource, int> scoreTarget)
    {
        PlayerResource bestTarget = null;
        int bestPressure = int.MaxValue;
        int bestScore = int.MinValue;

        foreach (PlayerResource player in activePlayers)
        {
            if (player == null || player == aiPlayer || player.isEliminated)
            {
                continue;
            }

            if (isValid != null && !isValid(player))
            {
                continue;
            }

            int pressure = GetNegativeCardTargetCount(player.playerId);
            int score = scoreTarget != null ? scoreTarget(player) : 0;

            if (pressure < bestPressure || pressure == bestPressure && score > bestScore)
            {
                bestPressure = pressure;
                bestScore = score;
                bestTarget = player;
            }
        }

        return bestTarget;
    }

    // Finds the opponent with the most cards for Steal Card.
    /// <summary>
    /// Searches scene objects or cached lists to find player with most cards.
    /// </summary>
    private PlayerResource FindPlayerWithMostCards(PlayerResource aiPlayer)
    {
        return FindBalancedOpponentTarget(
            aiPlayer,
            player => player.GetHandCardCount() > 0,
            player => player.GetHandCardCount()
        );
    }

    // Finds a trade target only if the AI receives a larger hand.
    /// <summary>
    /// Searches scene objects or cached lists to find trade hands target.
    /// </summary>
    private PlayerResource FindTradeHandsTarget(PlayerResource aiPlayer)
    {
        int aiCards = aiPlayer.GetHandCardCount();
        PlayerResource bestTarget = null;
        int bestGain = 0;
        int bestPressure = int.MaxValue;

        foreach (PlayerResource player in activePlayers)
        {
            if (player == null || player == aiPlayer || player.isEliminated)
            {
                continue;
            }

            int gain = player.GetHandCardCount() - aiCards;
            int pressure = GetNegativeCardTargetCount(player.playerId);

            if (gain > 0 && (pressure < bestPressure || pressure == bestPressure && gain > bestGain))
            {
                bestPressure = pressure;
                bestGain = gain;
                bestTarget = player;
            }
        }

        return bestTarget;
    }

    // Checks whether AI has at least one tower.
    /// <summary>
    /// Checks whether owned tower is present before the code depends on it.
    /// </summary>
    private bool HasOwnedTower(int playerId)
    {
        foreach (CannonTower tower in FindObjectsOfType<CannonTower>())
        {
            if (tower != null && tower.ownerPlayerId == playerId)
            {
                return true;
            }
        }

        return false;
    }

    // Checks whether Take Over has at least one affordable target.
    /// <summary>
    /// Checks whether affordable take over target is present before the code depends on it.
    /// </summary>
    private bool HasAffordableTakeOverTarget(PlayerResource aiPlayer)
    {
        return FindBestTakeOverTarget(aiPlayer, GetDifficulty(aiPlayer.playerId)) != null;
    }

    // Selects a controllable gate by position score.
    /// <summary>
    /// Returns best gate by position from the current scene or gameplay state.
    /// </summary>
    private GateFrameAnimation GetBestGateByPosition(List<GateFrameAnimation> gates, AIDifficulty difficulty)
    {
        GateFrameAnimation bestGate = gates[0];
        float bestScore = float.MinValue;

        foreach (GateFrameAnimation gate in gates)
        {
            float score = ScoreWorldPosition(gate.transform.position, difficulty);

            if (score > bestScore)
            {
                bestScore = score;
                bestGate = gate;
            }
        }

        return bestGate;
    }

    // Uses GateOwnershipManager when available, otherwise allows only unlinked fallback behavior.
    /// <summary>
    /// Checks whether AI control gate is allowed before enabling that action.
    /// </summary>
    private bool CanAIControlGate(int playerId, GateFrameAnimation gate)
    {
        if (gate == null)
        {
            return false;
        }

        if (gateOwnershipManager == null)
        {
            return true;
        }

        return gateOwnershipManager.CanPlayerControlGate(playerId, gate.gameObject);
    }

    // Distance to closest route node. Lower distance means stronger path control.
    /// <summary>
    /// Handles distance to nearest route node for this gameplay system.
    /// </summary>
    private float DistanceToNearestRouteNode(Vector3 position)
    {
        PathNode[] nodes = FindObjectsOfType<PathNode>();
        float bestDistance = 999f;

        foreach (PathNode node in nodes)
        {
            if (node == null || node.GetComponent<CastleEndNode>() != null)
            {
                continue;
            }

            bestDistance = Mathf.Min(bestDistance, Vector2.Distance(position, node.transform.position));
        }

        return bestDistance;
    }

    // Counts route nodes near a position as a rough high-traffic estimate.
    /// <summary>
    /// Handles count route nodes near for this gameplay system.
    /// </summary>
    private int CountRouteNodesNear(Vector3 position, float radius)
    {
        int count = 0;

        foreach (PathNode node in FindObjectsOfType<PathNode>())
        {
            if (node != null && node.GetComponent<CastleEndNode>() == null &&
                Vector2.Distance(position, node.transform.position) <= radius)
            {
                count += 1;
            }
        }

        return count;
    }

    // Counts castles near a position so Hard AI can value defensive/offensive areas.
    /// <summary>
    /// Handles count castles near for this gameplay system.
    /// </summary>
    private int CountCastlesNear(Vector3 position, float radius)
    {
        int count = 0;

        foreach (CastleBase castle in FindObjectsOfType<CastleBase>())
        {
            if (castle != null && Vector2.Distance(position, castle.transform.position) <= radius)
            {
                count += 1;
            }
        }

        return count;
    }

    // Tracks harmful card pressure so AI players do not all focus the same target in one round.
    /// <summary>
    /// Registers negative card target so later callbacks, lookups, or sync messages can use it.
    /// </summary>
    private void RegisterNegativeCardTarget(PlayerResource target)
    {
        if (target == null)
        {
            return;
        }

        int currentCount = GetNegativeCardTargetCount(target.playerId);
        negativeCardTargetCountsThisRound[target.playerId] = currentCount + 1;
        lastNegativeCardTargetRound[target.playerId] = currentRound;
    }

    // Tracks owner-directed pressure for harmful land cards when the land has an opponent owner.
    /// <summary>
    /// Registers land negative target so later callbacks, lookups, or sync messages can use it.
    /// </summary>
    private void RegisterLandNegativeTarget(PlayerResource aiPlayer, TowerBuildArea area)
    {
        if (aiPlayer == null || area == null || !area.isOwned || area.ownerPlayerId == aiPlayer.playerId)
        {
            return;
        }

        PlayerResource owner = playerManager != null ? playerManager.GetPlayerResource(area.ownerPlayerId) : null;
        RegisterNegativeCardTarget(owner);
    }

    // Returns how many harmful cards have targeted a player this round.
    /// <summary>
    /// Returns negative card target count used by card handling or target selection.
    /// </summary>
    private int GetNegativeCardTargetCount(int playerId)
    {
        int count = negativeCardTargetCountsThisRound.TryGetValue(playerId, out int currentRoundCount) ? currentRoundCount : 0;

        if (lastNegativeCardTargetRound.TryGetValue(playerId, out int lastRound) && lastRound == currentRound - 1)
        {
            count += 1;
        }

        return count;
    }

    // Publicly announces AI card use so human players can understand what happened.
    /// <summary>
    /// Shows AI card announcement with the correct current context.
    /// </summary>
    private void ShowAICardAnnouncement(PlayerResource actor, string cardName, string targetDescription)
    {
        string actorName = actor != null ? actor.GetDisplayName() : "AI";
        string message = actorName + " used " + cardName;

        if (!string.IsNullOrWhiteSpace(targetDescription))
        {
            message += " on " + targetDescription;
        }

        message += ".";
        ShowToast(message);
    }

    // Reads configured difficulty by playerId with safe fallback.
    /// <summary>
    /// Returns difficulty from the current scene or gameplay state.
    /// </summary>
    private AIDifficulty GetDifficulty(int playerId)
    {
        if (aiDifficultiesByPlayerId == null || playerId < 0 || playerId >= aiDifficultiesByPlayerId.Length)
        {
            return AIDifficulty.Easy;
        }

        return aiDifficultiesByPlayerId[playerId];
    }

    // Loads one AI difficulty from the lobby PlayerPrefs and stores it in the per-player difficulty array.
    /// <summary>
    /// Loads AI difficulty from player prefs from saved settings, room data, or scene references.
    /// </summary>
    private void LoadAIDifficultyFromPlayerPrefs(int playerId)
    {
        if (aiDifficultiesByPlayerId == null || playerId < 0 || playerId >= aiDifficultiesByPlayerId.Length)
        {
            return;
        }

        string key = "PlayerAIDifficulty_" + playerId;

        if (!PlayerPrefs.HasKey(key))
        {
            return;
        }

        string savedDifficulty = PlayerPrefs.GetString(key);

        if (System.Enum.TryParse(savedDifficulty, out AIDifficulty parsedDifficulty))
        {
            aiDifficultiesByPlayerId[playerId] = parsedDifficulty;
        }
    }

    // Minimum useful-card score by difficulty. Easy only uses obvious cards; Hard uses tactical cards aggressively.
    /// <summary>
    /// Returns minimum card priority to use used by card handling or target selection.
    /// </summary>
    private int GetMinimumCardPriorityToUse(AIDifficulty difficulty)
    {
        if (difficulty == AIDifficulty.Hard) return 25;
        if (difficulty == AIDifficulty.Medium) return 45;
        return 60;
    }

    /// <summary>
    /// Returns card use chance used by card handling or target selection.
    /// </summary>
    private float GetCardUseChance(AIDifficulty difficulty)
    {
        if (difficulty == AIDifficulty.Hard) return hardCardUseChance;
        if (difficulty == AIDifficulty.Medium) return mediumCardUseChance;
        return easyCardUseChance;
    }

    // Maximum card score the AI is willing to discard when its hand is full.
    /// <summary>
    /// Returns maximum discard priority from the current scene or gameplay state.
    /// </summary>
    private int GetMaximumDiscardPriority(AIDifficulty difficulty)
    {
        if (difficulty == AIDifficulty.Hard) return 20;
        if (difficulty == AIDifficulty.Medium) return 12;
        return 5;
    }

    // Selects tower prefab strictly by playerId so each slot keeps its own color/style.
    /// <summary>
    /// Returns tower prefab for player ID used by land, tower, cost, or build decisions.
    /// </summary>
    private GameObject GetTowerPrefabForPlayerId(int playerId)
    {
        if (towerPrefabsByPlayerId == null ||
            playerId < 0 ||
            playerId >= towerPrefabsByPlayerId.Length)
        {
            Debug.LogWarning("No tower prefab slot exists for playerId " + playerId + ".");
            return null;
        }

        GameObject prefab = towerPrefabsByPlayerId[playerId];

        if (prefab == null)
        {
            Debug.LogWarning("Tower prefab for playerId " + playerId + " is missing.");
        }

        return prefab;
    }

    // Removes spaces, underscores, hyphens, and clone suffixes from card prefab names.
    /// <summary>
    /// Handles normalize card name for card state, hand state, or targeting.
    /// </summary>
    private string NormalizeCardName(string cardName)
    {
        if (string.IsNullOrEmpty(cardName))
        {
            return "";
        }

        return cardName
            .Replace("(Clone)", "")
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }

    // Moves to the next active player, or starts the wave after every active player has acted.
    /// <summary>
    /// Advances to next turn or wave to the next turn, wave, player, or tutorial step.
    /// </summary>
    private void AdvanceToNextTurnOrWave()
    {
        BuildActivePlayerList();

        if (activePlayers.Count == 0)
        {
            return;
        }

        currentActivePlayerIndex += 1;

        if (currentActivePlayerIndex >= activePlayers.Count)
        {
            StartWavePhase();
            return;
        }

        StartCurrentPlayerTurn();
    }

    // Allows a human end-turn click to finish after the current UI frame.
    /// <summary>
    /// Advances after short delay to the next turn, wave, player, or tutorial step.
    /// </summary>
    private IEnumerator AdvanceAfterShortDelay()
    {
        yield return null;
        AdvanceToNextTurnOrWave();
    }

    // Starts the enemy wave and notifies wave-based tower/trap effects.
    /// <summary>
    /// Starts wave phase and enables its related gameplay flow.
    /// </summary>
    private void StartWavePhase()
    {
        isWaveRunning = true;
        SetHumanControlsEnabled(false);
        buildTowerManager?.HideBuildInteractionUI();
        cardDrawManager?.CancelPendingCard();

        if (waveManager != null)
        {
            waveManager.ShowWaveIncoming(currentWaveIndex);
        }

        NotifyTowersWaveStarted();
        NotifyShockTrapsWaveStarted();

        if (waveManager != null)
        {
            waveManager.currentRound = currentWaveIndex;
            waveManager.StartWave();
        }
    }

    // Enables human UI controls during human turns and blocks them during AI/wave automation.
    /// <summary>
    /// Sets human controls enabled and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetHumanControlsEnabled(bool enabled)
    {
        if (normalGameplayUI == null)
        {
            return;
        }

        // Keep the overall UI raycastable so Exit remains clickable even when
        // it is not the human player's turn. Gameplay actions are still blocked
        // by the shared turn checks in their own entry points.
        normalGameplayUI.interactable = true;
        normalGameplayUI.blocksRaycasts = true;
    }

    // Notifies all towers that a wave started so temporary effects can activate.
    /// <summary>
    /// Notifies connected systems that towers wave started occurred.
    /// </summary>
    private void NotifyTowersWaveStarted()
    {
        foreach (CannonTower tower in FindObjectsOfType<CannonTower>())
        {
            if (tower != null)
            {
                tower.OnWaveStarted();
            }
        }
    }

    // Notifies all towers that a wave ended so temporary effects can expire.
    /// <summary>
    /// Notifies connected systems that towers wave ended occurred.
    /// </summary>
    private void NotifyTowersWaveEnded()
    {
        foreach (CannonTower tower in FindObjectsOfType<CannonTower>())
        {
            if (tower != null)
            {
                tower.OnWaveEnded();
            }
        }
    }

    // Notifies all shock traps that enemy waves are active.
    /// <summary>
    /// Notifies connected systems that shock traps wave started occurred.
    /// </summary>
    private void NotifyShockTrapsWaveStarted()
    {
        foreach (ShockTrap trap in FindObjectsOfType<ShockTrap>())
        {
            if (trap != null)
            {
                trap.OnWaveStarted();
            }
        }
    }

    // Notifies all shock traps that the wave is no longer active.
    /// <summary>
    /// Notifies connected systems that shock traps wave ended occurred.
    /// </summary>
    private void NotifyShockTrapsWaveEnded()
    {
        foreach (ShockTrap trap in FindObjectsOfType<ShockTrap>())
        {
            if (trap != null)
            {
                trap.OnWaveEnded();
            }
        }
    }

    // Shows a toast through the assigned toast component, global UI, or CardDrawManager warning UI.
    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
            return;
        }

        if (GlobalUIManager.Instance != null)
        {
            GlobalUIManager.Instance.ShowToast(message);
            return;
        }

        if (cardDrawManager == null)
        {
            cardDrawManager = FindObjectOfType<CardDrawManager>();
        }

        if (cardDrawManager != null)
        {
            cardDrawManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }

    // Calls common toast method names without requiring a specific ToastMessage class.
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
