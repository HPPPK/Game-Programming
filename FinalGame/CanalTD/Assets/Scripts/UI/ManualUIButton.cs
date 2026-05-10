/*
 * File: ManualUIButton.cs
 *
 * Purpose:
 * This script provides manual UI click detection for buttons whose visual
 * RectTransform and Unity Button hitbox do not line up reliably. It checks the
 * RectTransform directly with RectTransformUtility and invokes a UnityEvent when
 * the mouse click is inside the visual rectangle.
 *
 * Usage:
 * - Attach this to a UI button object such as PlayCardButton, DrawCardButton,
 *   DiscardButton, HammerButton, ConfirmButton, or CancelButton.
 * - targetRect can be left empty if this script is on the same object as the
 *   RectTransform.
 * - uiCamera should be null for Screen Space Overlay canvases.
 * - uiCamera should be the canvas event camera for World Space canvases.
 * - Assign gameplay methods to onClick in the Inspector.
 *
 * Important:
 * - This does not replace visual Image/Button components.
 * - Remove old Unity Button OnClick events or leave the Button component only
 *   for visuals, otherwise the same action may trigger twice.
 */
using UnityEngine;
using UnityEngine.Events;

public class ManualUIButton : MonoBehaviour
{
    public RectTransform targetRect;
    public Camera uiCamera;
    public UnityEvent onClick;
    public bool debugLog = true;

    void Awake()
    {
        ResolveTargetRect();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        ResolveTargetRect();

        if (targetRect == null)
        {
            if (debugLog)
            {
                Debug.LogWarning(name + " ManualUIButton has no target RectTransform.");
            }

            return;
        }

        Vector2 mousePosition = Input.mousePosition;
        bool hit = RectTransformUtility.RectangleContainsScreenPoint(
            targetRect,
            mousePosition,
            uiCamera
        );

        if (debugLog)
        {
            Debug.Log(
                "ManualUIButton " + name +
                " mouse = " + mousePosition +
                " hit = " + hit
            );
        }

        if (hit)
        {
            onClick?.Invoke();
        }
    }

    private void ResolveTargetRect()
    {
        if (targetRect != null)
        {
            return;
        }

        targetRect = GetComponent<RectTransform>();
    }
}
