/*
 * File: UIRaycastCleaner.cs
 *
 * Purpose:
 * This utility disables raycastTarget on decorative UI Graphics so they do not
 * intercept pointer/raycast checks. It is useful when large images or text
 * RectTransforms overlap real controls and make UI click detection feel offset.
 *
 * Usage:
 * - Attach this script to the Canvas or any manager object.
 * - Assign targetCanvas, or leave it empty to use the Canvas on this object or
 *   the first Canvas found in the scene.
 * - If runOnStart is true, cleanup runs automatically at Start().
 * - Use PrintActiveRaycastTargets() from the component context menu to list
 *   remaining active raycast targets.
 *
 * Safety:
 * - TextMeshProUGUI raycastTarget is always disabled.
 * - Images stay raycastable only if they are clearly clickable.
 * - Known controls, Unity Button objects, ManualUIButton objects, and selectable
 *   card instances are preserved.
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

    void Start()
    {
        if (runOnStart)
        {
            CleanRaycastTargets();
        }
    }

    [ContextMenu("Clean Raycast Targets")]
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
