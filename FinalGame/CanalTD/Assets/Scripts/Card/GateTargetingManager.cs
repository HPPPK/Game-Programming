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
    public CanvasGroup normalGameplayUI;

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
        RefreshGates();
        ForceExitVisualState();
    }

    public void RefreshGates()
    {
        gates = FindObjectsOfType<GateFrameAnimation>();
    }

    public bool EnterGateTargetMode(GateActionType actionType)
    {
        if (actionType == GateActionType.None)
        {
            Debug.LogWarning("Cannot enter gate targeting mode without a gate action.");
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

        if (normalGameplayUI != null)
        {
            normalGameplayUI.interactable = false;
            normalGameplayUI.blocksRaycasts = false;
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(true);
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("TargetingUI is not assigned.");
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
                valid = !gate.IsBlocking() && !gate.IsLocked();
            }
            else if (actionType == GateActionType.OpenGate)
            {
                valid = gate.IsBlocking() && !gate.IsLocked();
            }

            if (valid)
            {
                results.Add(gate);
            }
        }

        return results;
    }

    public void SelectGate(GateFrameAnimation gate)
    {
        if (!isTargetingGate) return;

        if (!IsValidTarget(gate))
        {
            ShowToast("Invalid target");
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

        if (selectedGate == null)
        {
            Debug.Log("No gate selected.");
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

        CardDrawManager cardManager = FindObjectOfType<CardDrawManager>();
        if (cardManager != null)
        {
            cardManager.ConfirmPendingCard();
        }
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

    public void ExitGateTargetMode()
    {
        isTargetingGate = false;
        currentActionType = GateActionType.None;

        if (normalGameplayUI != null)
        {
            normalGameplayUI.interactable = true;
            normalGameplayUI.blocksRaycasts = true;
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(false);
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(false);
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

        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.ExitToolMode();
        }

        Debug.Log("Gate targeting mode OFF.");
    }

    void ForceExitVisualState()
    {
        if (targetingUI != null)
        {
            targetingUI.SetActive(false);
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(false);
        }

        if (normalGameplayUI != null)
        {
            normalGameplayUI.interactable = true;
            normalGameplayUI.blocksRaycasts = true;
        }
    }

    public bool IsTargetingGate()
    {
        return isTargetingGate;
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
