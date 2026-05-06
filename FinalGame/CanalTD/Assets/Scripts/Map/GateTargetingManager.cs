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
    private readonly List<GateFrameAnimation> validTargets = new List<GateFrameAnimation>();
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
        validTargets.Clear();
        validTargets.AddRange(GetValidGates(actionType));

        if (validTargets.Count == 0)
        {
            Debug.Log("No valid gates for action " + actionType);
            ShowToast("No valid gates.");
            return false;
        }

        isTargetingGate = true;
        currentActionType = actionType;
        selectedGate = null;

        SetTargetingVisuals(true);

        foreach (GateFrameAnimation gate in validTargets)
        {
            gate.SetHighlight(true, GetValidTargetColor());
        }

        Debug.Log("Gate targeting mode ON. Action = " + currentActionType);
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
        return gate != null && validTargets.Contains(gate);
    }

    public void PreviewGate(GateFrameAnimation gate, bool hovering)
    {
        if (!isTargetingGate) return;
        if (!IsValidTarget(gate)) return;
        if (gate == selectedGate) return;

        gate.SetHighlight(true, hovering ? GetHoverTargetColor() : GetValidTargetColor());
    }

    public void ConfirmSelection()
    {
        if (!isTargetingGate) return;

        if (selectedGate == null)
        {
            ShowToast("Select a gate first.");
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
        }
    }

    public void CancelSelection()
    {
        ExitGateTargetMode();
    }

    public void ExitGateTargetMode()
    {
        isTargetingGate = false;
        currentActionType = GateActionType.None;
        selectedGate = null;
        validTargets.Clear();

        SetTargetingVisuals(false);

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

        if (CursorToolManager.Instance != null)
        {
            CursorToolManager.Instance.ExitToolMode();
        }

        Debug.Log("Gate targeting mode OFF.");
    }

    public bool IsTargetingGate()
    {
        return isTargetingGate;
    }

    public void ShowToast(string message)
    {
        Debug.Log(message);
    }

    private void ForceExitVisualState()
    {
        SetTargetingVisuals(false);
    }

    private void SetTargetingVisuals(bool active)
    {
        if (normalGameplayUI != null)
        {
            normalGameplayUI.interactable = !active;
            normalGameplayUI.blocksRaycasts = !active;
        }

        if (darkOverlay != null)
        {
            darkOverlay.SetActive(active);
        }

        if (targetingUI != null)
        {
            targetingUI.SetActive(active);
        }

        foreach (Tilemap tilemap in tilemapsToDim)
        {
            if (tilemap != null)
            {
                tilemap.color = active ? dimMapColor : normalMapColor;
            }
        }

        foreach (SpriteRenderer sr in spritesToDim)
        {
            if (sr != null)
            {
                sr.color = active ? dimMapColor : normalMapColor;
            }
        }
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
}
