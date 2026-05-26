/*
 * File: GateTargetingManager.cs
 *
 * Purpose:
 * This manager controls the temporary mode where the player chooses a gate after
 * playing a gate-related card. It dims the map, highlights valid gates, tracks
 * the selected gate, and runs the final Open Gate or Lock Gate action when the
 * player confirms the choice.
 *
 * Main gameplay flow:
 * 1. CardDrawManager calls EnterGateTargetMode(actionType) after a gate card is played.
 * 2. This manager finds every GateFrameAnimation in the scene.
 * 3. It filters gates into validTargets based on the requested GateActionType.
 * 4. Valid gates are highlighted and the targeting UI/dark overlay is shown.
 * 5. GateFrameAnimation calls SelectGate(this) when the player clicks a valid gate.
 * 6. ConfirmSelection() executes OpenGate() or LockGate() on the selected gate.
 * 7. The pending card is confirmed through CardDrawManager only after the gate
 *    action succeeds.
 *
 * Inspector setup:
 * - tilemapsToDim and spritesToDim define which map visuals get darkened.
 * - targetingUI should contain the confirm/cancel controls for targeting mode.
 * - darkOverlay is the screen overlay shown while picking a gate.
 * - hammerButton is shown only while the player is choosing a gate target. If
 *   it is not assigned manually, this script tries to find a child named
 *   "HammerButton" under normalGameplayUI.
 * - normalGameplayUI is disabled during targeting so normal UI does not receive clicks.
 *
 * Dependency notes:
 * - GateFrameAnimation owns each individual gate's animation and blocking state.
 * - CardDrawManager owns the pending card that should be consumed or returned.
 * - CursorToolManager exits hammer mode when targeting ends.
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
        RefreshGates();
        ForceExitVisualState();
    }

    public void RefreshGates()
    {
        gates = FindObjectsOfType<GateFrameAnimation>();
    }

    public bool EnterGateTargetMode(GateActionType actionType)
    {
        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast("You cannot control gates during enemy wave.");
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
        List<GateFrameAnimation> results = new List<GateFrameAnimation>();

        if (gates == null)
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

            if (valid && CanCurrentPlayerControlGate(gate))
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

        if (gamePhaseManager != null && !gamePhaseManager.IsPlayerPhase())
        {
            ShowToast("You cannot control gates during enemy wave.");
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

        if (manager != null && !manager.CanPlayCard())
        {
            manager.TryConsumePlayCard();
            return;
        }

        if (manager != null && !manager.CanChangeGate())
        {
            manager.TryConsumeGateChange();
            return;
        }

        bool actionSucceeded = false;

        if (currentActionType == GateActionType.OpenGate)
        {
            actionSucceeded = selectedGate.OpenGate();
        }
        else if (currentActionType == GateActionType.LockGate)
        {
            actionSucceeded = selectedGate.LockGate();
        }

        if (!actionSucceeded)
        {
            Debug.LogWarning("Gate action failed: " + currentActionType);
            return;
        }

        if (manager != null && !manager.TryConsumeGateChange())
        {
            return;
        }

        CardDrawManager cardManager = FindObjectOfType<CardDrawManager>();
        if (cardManager != null)
        {
            cardManager.ConfirmPendingCard();
        }

        string actorName = playerManager != null ? playerManager.GetPlayerDisplayName(currentPlayerId) : "Current player";
        string cardName = currentActionType == GateActionType.LockGate ? "Lock Gate" : "Open Gate";
        ShowToast(actorName + " used " + cardName + ".");
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
        CastleEndNode[] ends = GetCastleEndsForPathCheck();

        if (spawners.Length == 0 || ends.Length == 0)
        {
            Debug.LogWarning("Gate path safety check skipped because spawners or castle ends are missing.");
            return true;
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

    public void ShowToast(string message)
    {
        CardDrawManager cardManager = FindObjectOfType<CardDrawManager>();

        if (cardManager != null)
        {
            cardManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }
}
