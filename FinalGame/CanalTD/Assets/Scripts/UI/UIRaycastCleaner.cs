/*
 * File: UIRaycastCleaner.cs
 *
 * Purpose:
 * Implements UIRaycastCleaner for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for UIRaycastCleaner within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify UIRaycastCleaner in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UIRaycastCleaner : MonoBehaviour
{
    public Canvas targetCanvas;
    public bool runOnStart = true;

    private static readonly string[] ClickableObjectNames =
    {
        "PlayCardButton",
        "DrawCardButton",
        "DiscardButton",
        "ConfirmButton",
        "CancelButton",
        "HammerButton"
    };

    /// <summary>
    /// Sets up UI raycast cleaner when this scene object starts running.
    /// </summary>
    void Start()
    {
        if (runOnStart)
        {
            CleanRaycastTargets();
        }
    }

    [ContextMenu("Clean Raycast Targets")]
    /// <summary>
    /// Cleans raycast targets from scene state, UI state, or saved runtime data.
    /// </summary>
    public void CleanRaycastTargets()
    {
        Canvas canvas = ResolveCanvas();

        if (canvas == null)
        {
            Debug.LogWarning("UIRaycastCleaner could not find a Canvas.");
            return;
        }

        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
            {
                continue;
            }

            if (!graphic.raycastTarget)
            {
                continue;
            }

            if (ShouldDisableRaycastTarget(graphic))
            {
                graphic.raycastTarget = false;
                Debug.Log("Disabled Raycast Target: " + GetObjectPath(graphic.transform));
            }
        }
    }

    [ContextMenu("Print Active Raycast Targets")]
    /// <summary>
    /// Handles print active raycast targets for card state, hand state, or targeting.
    /// </summary>
    public void PrintActiveRaycastTargets()
    {
        Canvas canvas = ResolveCanvas();

        if (canvas == null)
        {
            Debug.LogWarning("UIRaycastCleaner could not find a Canvas.");
            return;
        }

        Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || !graphic.raycastTarget)
            {
                continue;
            }

            RectTransform rectTransform = graphic.GetComponent<RectTransform>();
            Vector2 size = rectTransform != null ? rectTransform.rect.size : Vector2.zero;

            Debug.Log(
                "Active Raycast Target: " +
                GetObjectPath(graphic.transform) +
                " | size = " + size
            );
        }
    }

    /// <summary>
    /// Decides whether should disable raycast target should happen in the current mode and turn state.
    /// </summary>
    private bool ShouldDisableRaycastTarget(Graphic graphic)
    {
        if (IsClickableGraphic(graphic))
        {
            return false;
        }

        if (graphic is TextMeshProUGUI)
        {
            return true;
        }

        if (graphic is Image)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks the current state to decide whether clickable graphic is true.
    /// </summary>
    private bool IsClickableGraphic(Graphic graphic)
    {
        GameObject targetObject = graphic.gameObject;

        if (IsKnownClickableName(targetObject.name))
        {
            return true;
        }

        if (targetObject.GetComponent<Button>() != null)
        {
            return true;
        }

        if (targetObject.GetComponent<CardInstanceSelectable>() != null)
        {
            return true;
        }

        if (targetObject.GetComponent<ManualUIButton>() != null)
        {
            return true;
        }

        if (targetObject.GetComponent<Toggle>() != null)
        {
            return true;
        }

        if (targetObject.GetComponent<Slider>() != null)
        {
            return true;
        }

        if (targetObject.GetComponent<Scrollbar>() != null)
        {
            return true;
        }

        if (targetObject.GetComponent<Dropdown>() != null)
        {
            return true;
        }

        if (targetObject.GetComponent<TMP_Dropdown>() != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks the current state to decide whether known clickable name is true.
    /// </summary>
    private bool IsKnownClickableName(string objectName)
    {
        foreach (string clickableName in ClickableObjectNames)
        {
            if (objectName == clickableName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Looks up the target for canvas and applies the resolved gameplay result.
    /// </summary>
    private Canvas ResolveCanvas()
    {
        if (targetCanvas != null)
        {
            return targetCanvas;
        }

        targetCanvas = GetComponent<Canvas>();

        if (targetCanvas != null)
        {
            return targetCanvas;
        }

        targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas != null)
        {
            return targetCanvas;
        }

        targetCanvas = FindObjectOfType<Canvas>();
        return targetCanvas;
    }

    /// <summary>
    /// Returns object path used to update UI text, layout, or feedback.
    /// </summary>
    private string GetObjectPath(Transform target)
    {
        if (target == null)
        {
            return "";
        }

        string path = target.name;
        Transform current = target.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
