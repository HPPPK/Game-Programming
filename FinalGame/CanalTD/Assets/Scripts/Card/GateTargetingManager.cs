/*
 * File: GateTargetingManager.cs
 *
 * Purpose:
 * Implements GateTargetingManager for the card layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for GateTargetingManager within the card system.
 * - Coordinate related objects, state changes, and cross-system communication.
 * - Handle card usage, targeting, hand state, or card-driven map interactions.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Card selections, targeting choices, turn permissions, and player hand data.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Applies card outcomes, targeting results, hand count changes, or card-related restrictions.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify GateTargetingManager in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class GateTargetingManager : MonoBehaviour
{
    public static GateTargetingManager Instance;

    [Header("Map Layers To Dim")]
    public Tilemap[] tilemapsToDim;

    [Header("Sprites To Dim")]
    public SpriteRenderer[] spritesToDim;

    [Header("UI")]
    public GameObject targetingUI;
    public GameObject darkOverlay;
    public GameObject hammerButton;
    public CanvasGroup normalGameplayUI;
    public Button confirmButton;

    [Header("Gate Ownership")]
    public int currentPlayerId = 0;
    public GateOwnershipManager gateOwnershipManager;
    public PlayerManager playerManager;

    [Header("Turn")]
    public TurnManager turnManager;

    [Header("Phase Manager")]
    public GamePhaseManager gamePhaseManager;

    [Header("Path Safety")]
    public bool preventLocksThatBlockAllPaths = true;
    public EnemySpawner[] enemySpawners;
    public CastleEndNode[] castleEnds;

    [Header("Colors")]
    public Color normalMapColor = Color.white;
    public Color dimMapColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    public Color normalGateColor = Color.white;
    public Color highlightGateColor = Color.yellow;
    public Color selectedGateColor = Color.green;
    public Color lockPreviewColor = new Color(0.65f, 0.85f, 1f, 0.45f);
    public Color lockSelectedColor = new Color(0.25f, 1f, 0.45f, 1f);

    private GateFrameAnimation[] gates;
    private List<GateFrameAnimation> validTargets = new List<GateFrameAnimation>();
    private GateFrameAnimation selectedGate;
    private bool isTargetingGate = false;
    private GateActionType currentActionType = GateActionType.None;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ResolveHammerButton();
        ResolveConfirmButton();
        RefreshGates();
        ForceExitVisualState();
    }

    public void RefreshGates()
    {
        gates = FindObjectsOfType<GateFrameAnimation>();
    }

    public bool EnterGateTargetMode(GateActionType actionType)
    {
        if (TutorialActionGate.BlockIfNotAllowed(GetTutorialActionType(actionType), null))
        {
            return false;
        }

        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot control gates during enemy wave.")
                : "You cannot control gates during enemy wave.");
            return false;
        }

        if (actionType == GateActionType.None)
        {
            Debug.LogWarning("Cannot enter gate targeting mode without a gate action.");
            return false;
        }

        if (gateOwnershipManager == null)
        {
            ShowToast("Gate ownership manager is missing.");
            return false;
        }

        RefreshGates();
        validTargets = GetValidGates(actionType);

        if (validTargets.Count == 0)
        {
            Debug.Log("No valid gates for action " + actionType);
            ShowToast("No valid gates.");
            return false;
        }

        isTargetingGate = true;
        currentActionType = actionType;
        selectedGate = null;

        EnterTargetingVisualState();
        UpdateConfirmButtonState();

        foreach (GateFrameAnimation gate in validTargets)
        {
            Debug.Log("Valid gate target: " + gate.name);
            gate.SetHighlight(true, GetValidTargetColor());
        }

        Debug.Log("Gate targeting mode ON. Action = " + currentActionType + ". Valid targets = " + validTargets.Count);
        return true;
    }

    public List<GateFrameAnimation> GetValidGates(GateActionType actionType)
    {
        return GetValidGatesForPlayer(actionType, GetCurrentPlayerId());
    }

    // Shared by human targeting and AI card logic so both follow the same gate ownership and path-safety rules.
    public List<GateFrameAnimation> GetValidGatesForPlayer(GateActionType actionType, int playerId)
    {
        List<GateFrameAnimation> results = new List<GateFrameAnimation>();

        if (gates == null)
        {
            RefreshGates();
        }

        if (gateOwnershipManager == null)
        {
            return results;
        }

        foreach (GateFrameAnimation gate in gates)
        {
            if (gate == null) continue;

            bool valid = false;

            if (actionType == GateActionType.LockGate)
            {
                valid = !gate.IsBlocking() &&
                    !gate.IsLocked() &&
                    CanLockGateWithoutRemovingAllEnemyPaths(gate);
            }
            else if (actionType == GateActionType.OpenGate)
            {
                valid = gate.CanOpen();
            }

            if (valid && gateOwnershipManager.CanPlayerControlGate(playerId, gate.gameObject))
            {
                results.Add(gate);
            }
        }

        return results;
    }

    public bool HasValidGateTargets(GateActionType actionType)
    {
        if (actionType == GateActionType.None)
        {
            return false;
        }

        RefreshGates();
        return GetValidGates(actionType).Count > 0;
    }

    public void SelectGate(GateFrameAnimation gate)
    {
        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (!isTargetingGate) return;

        if (!IsValidTarget(gate))
        {
            ShowGateBlockToast(GetGateBlockReason(gate));
            return;
        }

        if (selectedGate != null)
        {
            selectedGate.SetHighlight(true, GetValidTargetColor());
        }

        selectedGate = gate;
        selectedGate.SetHighlight(true, GetSelectedTargetColor());
        UpdateConfirmButtonState();
        TutorialManager.Instance?.NotifyTutorialAction(TutorialActionType.SelectTarget, gate != null ? gate.gameObject : null);

        Debug.Log("Gate selected: " + gate.name);
    }

    public bool IsValidTarget(GateFrameAnimation gate)
    {
        return gate != null && validTargets != null && validTargets.Contains(gate);
    }

    private bool CanCurrentPlayerControlGate(GateFrameAnimation gate)
    {
        if (gateOwnershipManager == null)
        {
            return false;
        }

        return gate != null &&
            gateOwnershipManager.CanPlayerControlGate(GetCurrentPlayerId(), gate.gameObject);
    }

    private string GetGateBlockReason(GateFrameAnimation gate)
    {
        if (gateOwnershipManager == null)
        {
            return "Gate ownership manager is missing.";
        }

        string reason = gateOwnershipManager.GetGateBlockReason(
            GetCurrentPlayerId(),
            gate != null ? gate.gameObject : null
        );

        return string.IsNullOrEmpty(reason) ? "Invalid target" : reason;
    }

    private void ShowGateBlockToast(string message)
    {
        if (gateOwnershipManager != null)
        {
            gateOwnershipManager.ShowToast(message);
            return;
        }

        ShowToast(message);
    }

    public void PreviewGate(GateFrameAnimation gate, bool hovering)
    {
        if (!isTargetingGate) return;
        if (!IsValidTarget(gate)) return;
        if (gate == selectedGate) return;

        gate.SetHighlight(true, hovering ? GetHoverTargetColor() : GetValidTargetColor());
    }

    private Color GetValidTargetColor()
    {
        return currentActionType == GateActionType.LockGate ? lockPreviewColor : highlightGateColor;
    }

    private Color GetHoverTargetColor()
    {
        return currentActionType == GateActionType.LockGate ? lockSelectedColor : selectedGateColor;
    }

    private Color GetSelectedTargetColor()
    {
        return currentActionType == GateActionType.LockGate ? lockSelectedColor : selectedGateColor;
    }

    public void ConfirmSelection()
    {
        if (!isTargetingGate) return;

        if (ShouldBlockOnlineAction())
        {
            return;
        }

        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot control gates during enemy wave.")
                : "You cannot control gates during enemy wave.");
            return;
        }

        if (selectedGate == null)
        {
            Debug.Log("No gate selected.");
            return;
        }

        if (!CanSelectedGateStillChange())
        {
            ShowToast("Invalid target");
            return;
        }

        TurnManager manager = GetTurnManager();
        ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource(manager);

        if (TurnSourceResolver.IsAIPrototypeActive() && (turnSource == null || !turnSource.CanHumanAct))
        {
            ShowToast("Wait for your turn.");
            return;
        }

        if (PhotonOnlineGameSceneManager.IsLiveOnlineGameSceneContext() &&
            PhotonOnlineCardSyncManager.Instance != null)
        {
            CardDrawManager cardManager = GetCardDrawManager();
            GameObject pendingCardPrefab = cardManager != null ? cardManager.GetPendingCardPrefabForTargeting() : null;

            if (pendingCardPrefab == null)
            {
                ShowToast("Could not play this card.");
                return;
            }

            bool requested = PhotonOnlineCardSyncManager.Instance.RequestPlayCard(
                CardDrawManager.NormalizeCardId(pendingCardPrefab.name),
                pendingCardPrefab.name,
                selectedGate.gameObject.name,
                -1
            );

            if (requested)
            {
                ExitGateTargetMode();
            }

            return;
        }

        bool actionSucceeded = currentActionType == GateActionType.OpenGate
            ? TryOpenGateForPlayer(GetCurrentPlayerId(), selectedGate, true, true)
            : TryLockGateForPlayer(GetCurrentPlayerId(), selectedGate, true, true);

        if (!actionSucceeded)
        {
            Debug.LogWarning("Gate action failed: " + currentActionType);
            return;
        }

        string actorName = playerManager != null ? playerManager.GetPlayerDisplayName(GetCurrentPlayerId()) : "Current player";
        string cardName = currentActionType == GateActionType.LockGate ? "Lock Gate" : "Open Gate";
        ShowToast(actorName + " used " + cardName + ".");
    }

    public bool TryOpenGateForPlayer(int playerId, GateFrameAnimation gate)
    {
        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        return TryOpenGateForPlayer(playerId, gate, true, false);
    }

    public bool TryLockGateForPlayer(int playerId, GateFrameAnimation gate)
    {
        if (ShouldBlockOnlineAction())
        {
            return false;
        }

        return TryLockGateForPlayer(playerId, gate, true, false);
    }

    public bool TryOpenGateForPlayer(int playerId, GateFrameAnimation gate, bool consumeTurnResources, bool consumePendingCard)
    {
        return TryExecuteGateActionForPlayer(playerId, gate, GateActionType.OpenGate, consumeTurnResources, consumePendingCard);
    }

    public bool TryLockGateForPlayer(int playerId, GateFrameAnimation gate, bool consumeTurnResources, bool consumePendingCard)
    {
        return TryExecuteGateActionForPlayer(playerId, gate, GateActionType.LockGate, consumeTurnResources, consumePendingCard);
    }

    public void CancelSelection()
    {
        CardDrawManager cardManager = FindObjectOfType<CardDrawManager>();
        if (cardManager != null)
        {
            cardManager.CancelPendingCard();
        }

        ExitGateTargetMode();
    }

    public void ToggleHammerTool()
    {
        if (!isTargetingGate)
        {
            ShowToast("Play a gate card first.");
            return;
        }

        ToggleHammerToolState();
    }

    public void ExitGateTargetMode()
    {
        isTargetingGate = false;
        currentActionType = GateActionType.None;

        ExitTargetingVisualState();

        if (gates != null)
        {
            foreach (GateFrameAnimation gate in gates)
            {
                if (gate != null)
                {
                    gate.SetHighlight(false, normalGateColor);
                }
            }
        }

        selectedGate = null;
        validTargets.Clear();
        UpdateConfirmButtonState();

        Debug.Log("Gate targeting mode OFF.");
    }

    void ForceExitVisualState()
    {
        ResolveHammerButton();

        if (targetingUI != null)
        {
            targetingUI.SetActive(false);
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(false);
            SetOverlayRaycastBlocking(false);
        }

        if (hammerButton != null)
        {
            hammerButton.SetActive(false);
        }

        if (normalGameplayUI != null)
        {
            normalGameplayUI.interactable = true;
            normalGameplayUI.blocksRaycasts = true;
        }
    }

    void EnterTargetingVisualState()
    {
        EnterHammerTool();
        ResolveHammerButton();

        if (BuildTowerManager.Instance != null)
        {
            BuildTowerManager.Instance.HideBuildInteractionUI();
        }

        if (normalGameplayUI != null)
        {
            normalGameplayUI.interactable = false;
            normalGameplayUI.blocksRaycasts = false;
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(true);
            SetOverlayRaycastBlocking(true);
            darkOverlay.transform.SetAsLastSibling();
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(true);
            targetingUI.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning("TargetingUI is not assigned.");
        }

        if (hammerButton != null)
        {
            hammerButton.SetActive(true);
        }

        foreach (Tilemap tilemap in tilemapsToDim)
        {
            if (tilemap != null)
            {
                tilemap.color = dimMapColor;
            }
        }

        foreach (SpriteRenderer sr in spritesToDim)
        {
            if (sr != null)
            {
                sr.color = dimMapColor;
            }
        }
    }

    void ExitTargetingVisualState()
    {
        if (normalGameplayUI != null)
        {
            normalGameplayUI.interactable = true;
            normalGameplayUI.blocksRaycasts = true;
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(false);
            SetOverlayRaycastBlocking(false);
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(false);
        }

        if (hammerButton != null)
        {
            hammerButton.SetActive(false);
        }

        foreach (Tilemap tilemap in tilemapsToDim)
        {
            if (tilemap != null)
            {
                tilemap.color = normalMapColor;
            }
        }

        foreach (SpriteRenderer sr in spritesToDim)
        {
            if (sr != null)
            {
                sr.color = normalMapColor;
            }
        }

        ExitHammerTool();
    }

    void SetOverlayRaycastBlocking(bool blocking)
    {
        if (darkOverlay == null)
        {
            return;
        }

        Image overlayImage = darkOverlay.GetComponent<Image>();

        if (overlayImage != null)
        {
            overlayImage.raycastTarget = blocking;
        }
    }

    void ResolveHammerButton()
    {
        if (hammerButton != null)
        {
            return;
        }

        ResolveNormalGameplayUI();

        if (normalGameplayUI == null)
        {
            return;
        }

        Transform[] children = normalGameplayUI.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child != null && child.name == "HammerButton")
            {
                hammerButton = child.gameObject;
                return;
            }
        }
    }

    void ResolveConfirmButton()
    {
        if (confirmButton != null || targetingUI == null)
        {
            return;
        }

        Transform[] children = targetingUI.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null || child.name != "ConfirmButton")
            {
                continue;
            }

            confirmButton = child.GetComponent<Button>();

            if (confirmButton != null)
            {
                return;
            }
        }
    }

    void ResolveNormalGameplayUI()
    {
        if (normalGameplayUI != null)
        {
            return;
        }

        CanvasGroup[] canvasGroups = FindObjectsOfType<CanvasGroup>(true);

        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            if (canvasGroup != null && canvasGroup.name == "NormalGameplayUI")
            {
                normalGameplayUI = canvasGroup;
                return;
            }
        }
    }

    public bool IsTargetingGate()
    {
        return isTargetingGate;
    }

    private void UpdateConfirmButtonState()
    {
        ResolveConfirmButton();

        if (confirmButton == null)
        {
            return;
        }

        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialGameplayActive)
        {
            confirmButton.interactable = true;
            return;
        }

        confirmButton.interactable = isTargetingGate && selectedGate != null;
    }

    public bool IsAnyTargetingActive()
    {
        return isTargetingGate;
    }

    public bool IsHammerToolActive()
    {
        if (VisualCursorFollower.Instance != null)
        {
            return VisualCursorFollower.Instance.IsHammerMode();
        }

        return CursorToolManager.Instance != null && CursorToolManager.Instance.IsHammerMode;
    }

    void EnterHammerTool()
    {
        if (VisualCursorFollower.Instance != null)
        {
            VisualCursorFollower.Instance.SetHammerCursor();
            return;
        }

        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.EnterHammerMode();
        }
    }

    void ExitHammerTool()
    {
        if (VisualCursorFollower.Instance != null)
        {
            VisualCursorFollower.Instance.SetNormalCursor();
            return;
        }

        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.ExitToolMode();
        }
    }

    void ToggleHammerToolState()
    {
        if (VisualCursorFollower.Instance != null)
        {
            VisualCursorFollower.Instance.ToggleHammerCursor();
            return;
        }

        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.ToggleHammerMode();
            return;
        }

        Debug.LogWarning("No cursor controller found.");
    }

    private bool CanSelectedGateStillChange()
    {
        if (selectedGate == null)
        {
            return false;
        }

        if (currentActionType == GateActionType.OpenGate)
        {
            return selectedGate.CanOpen();
        }

        if (currentActionType == GateActionType.LockGate)
        {
            if (!CanLockGateWithoutRemovingAllEnemyPaths(selectedGate))
            {
                ShowToast("This gate would block all enemy paths.");
                return false;
            }

            return !selectedGate.IsLocked() &&
                !selectedGate.IsPlaying() &&
                !selectedGate.IsBlocking();
        }

        return false;
    }

    private bool TryExecuteGateActionForPlayer(
        int playerId,
        GateFrameAnimation gate,
        GateActionType actionType,
        bool consumeTurnResources,
        bool consumePendingCard)
    {
        if (TutorialActionGate.BlockIfNotAllowed(
                GetTutorialActionType(actionType),
                gate != null ? gate.gameObject : null))
        {
            return false;
        }

        if (actionType == GateActionType.None || gate == null)
        {
            return false;
        }

        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast(TutorialManager.Instance != null
                ? TutorialManager.Instance.GetWaveInteractionBlockedMessageOrDefault("You cannot control gates during enemy wave.")
                : "You cannot control gates during enemy wave.");
            return false;
        }

        if (gateOwnershipManager == null)
        {
            ShowToast("Gate ownership manager is missing.");
            return false;
        }

        if (!gateOwnershipManager.CanPlayerControlGate(playerId, gate.gameObject))
        {
            ShowGateBlockToast(gateOwnershipManager.GetGateBlockReason(playerId, gate.gameObject));
            return false;
        }

        if (actionType == GateActionType.OpenGate)
        {
            if (!gate.CanOpen())
            {
                ShowToast("Invalid target");
                return false;
            }
        }
        else
        {
            if (!CanLockGateWithoutRemovingAllEnemyPaths(gate))
            {
                ShowToast("This gate would block all enemy paths.");
                return false;
            }

            if (gate.IsLocked() || gate.IsPlaying() || gate.IsBlocking())
            {
                ShowToast("Invalid target");
                return false;
            }
        }

        TurnManager manager = GetTurnManager();

        if (consumeTurnResources && manager != null)
        {
            if (!manager.CanPlayCard())
            {
                manager.TryConsumePlayCard();
                return false;
            }

            if (!manager.CanChangeGate())
            {
                manager.TryConsumeGateChange();
                return false;
            }
        }

        bool actionSucceeded = actionType == GateActionType.OpenGate
            ? gate.OpenGate()
            : gate.LockGate();

        if (!actionSucceeded)
        {
            return false;
        }

        if (consumeTurnResources && manager != null && !manager.TryConsumePlayCard())
        {
            return false;
        }

        if (consumeTurnResources && manager != null && !manager.TryConsumeGateChange())
        {
            return false;
        }

        if (consumePendingCard)
        {
            CardDrawManager cardManager = GetCardDrawManager();

            if (cardManager != null && !cardManager.ConfirmCardConsumeAfterSuccessfulResolution(false))
            {
                return false;
            }
        }

        TutorialManager.Instance?.NotifyCardPlayed(GetTutorialActionType(actionType), gate != null ? gate.gameObject : null);

        return true;
    }

    private TutorialActionType GetTutorialActionType(GateActionType actionType)
    {
        if (actionType == GateActionType.OpenGate)
        {
            return TutorialActionType.OpenGate;
        }

        if (actionType == GateActionType.LockGate)
        {
            return TutorialActionType.LockGate;
        }

        return TutorialActionType.PlayCard;
    }

    private TurnManager GetTurnManager()
    {
        return turnManager != null ? turnManager : TurnManager.Instance;
    }

    private int GetCurrentPlayerId()
    {
        if (playerManager != null)
        {
            return playerManager.GetCurrentPlayerId();
        }

        TurnManager manager = GetTurnManager();
        ITurnSource turnSource = TurnSourceResolver.GetActiveTurnSource(manager);

        if (turnSource != null)
        {
            return turnSource.CurrentPlayerId;
        }

        return currentPlayerId;
    }

    private bool CanLockGateWithoutRemovingAllEnemyPaths(GateFrameAnimation gate)
    {
        if (!preventLocksThatBlockAllPaths)
        {
            return true;
        }

        if (gate == null)
        {
            return false;
        }

        if (gate.IsBlocking())
        {
            return true;
        }

        EnemySpawner[] spawners = GetEnemySpawnersForPathCheck();
        CastleEndNode[] ends = GetActiveCastleEndsForPathCheck();

        if (spawners.Length == 0 || ends.Length == 0)
        {
            Debug.LogWarning("Lock Gate rejected because spawners or active castle ends are missing.");
            return false;
        }

        bool originalBlocking = gate.isBlocking;
        gate.isBlocking = true;
        PathGraphState.MarkDirty();

        bool everySpawnerStillHasPath = EverySpawnerCanReachAnyCastle(spawners, ends);

        gate.isBlocking = originalBlocking;
        PathGraphState.MarkDirty();

        return everySpawnerStillHasPath;
    }

    private bool EverySpawnerCanReachAnyCastle(EnemySpawner[] spawners, CastleEndNode[] ends)
    {
        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner == null || spawner.startNode == null)
            {
                continue;
            }

            bool hasPath = false;

            foreach (CastleEndNode castleEnd in ends)
            {
                if (castleEnd == null)
                {
                    continue;
                }

                List<PathNode> path = Pathfinder.FindPath(spawner.startNode, castleEnd);

                if (path != null && path.Count > 0)
                {
                    hasPath = true;
                    break;
                }
            }

            if (!hasPath)
            {
                Debug.Log("Lock Gate rejected because " + spawner.name + " would have no reachable castle.");
                return false;
            }
        }

        return true;
    }

    private EnemySpawner[] GetEnemySpawnersForPathCheck()
    {
        if (enemySpawners != null && enemySpawners.Length > 0)
        {
            return enemySpawners;
        }

        return FindObjectsOfType<EnemySpawner>();
    }

    private CastleEndNode[] GetCastleEndsForPathCheck()
    {
        if (castleEnds != null && castleEnds.Length > 0)
        {
            return castleEnds;
        }

        if (EnemyPathAssignmentManager.Instance != null &&
            EnemyPathAssignmentManager.Instance.castleEnds != null &&
            EnemyPathAssignmentManager.Instance.castleEnds.Length > 0)
        {
            return EnemyPathAssignmentManager.Instance.castleEnds;
        }

        return FindObjectsOfType<CastleEndNode>();
    }

    // Lock Gate safety only counts castles that still belong to non-eliminated players.
    private CastleEndNode[] GetActiveCastleEndsForPathCheck()
    {
        List<CastleEndNode> activeEnds = new List<CastleEndNode>();

        foreach (CastleEndNode castleEnd in GetCastleEndsForPathCheck())
        {
            if (IsActiveCastleEnd(castleEnd))
            {
                activeEnds.Add(castleEnd);
            }
        }

        return activeEnds.ToArray();
    }

    private bool IsActiveCastleEnd(CastleEndNode castleEnd)
    {
        if (castleEnd == null || castleEnd.targetCastle == null)
        {
            return false;
        }

        CastleBase castle = castleEnd.targetCastle;

        if (castle.currentHP <= 0)
        {
            return false;
        }

        if (castle.ownerResource != null)
        {
            return !castle.ownerResource.isEliminated;
        }

        if (playerManager != null)
        {
            return !playerManager.IsPlayerEliminated(castle.ownerPlayerId);
        }

        return true;
    }

    public void ShowToast(string message)
    {
        CardDrawManager cardManager = GetCardDrawManager();

        if (cardManager != null)
        {
            cardManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }

    private CardDrawManager GetCardDrawManager()
    {
        return FindObjectOfType<CardDrawManager>();
    }

    private bool ShouldBlockOnlineAction()
    {
        return PhotonOnlineGameSceneManager.ShouldBlockLocalGameplayAction(true);
    }
}
