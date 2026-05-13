/*
 * File: ManualUIButtonDebug.cs
 *
 * Purpose:
 * Optional helper for checking whether the current mouse position is inside a
 * UI RectTransform. This does not invoke gameplay actions. It is only for
 * diagnosing UI click offset problems.
 */
using UnityEngine;

public class ManualUIButtonDebug : MonoBehaviour
{
    public RectTransform targetRect;
    public Camera uiCamera;
    public bool logEveryFrame = false;
    public bool logOnClick = true;

    void Awake()
    {
        ResolveTargetRect();
    }

    void Update()
    {
        if (!logEveryFrame && !(logOnClick && Input.GetMouseButtonDown(0)))
        {
            return;
        }

        ResolveTargetRect();

        if (targetRect == null)
        {
            Debug.LogWarning(name + " ManualUIButtonDebug has no target RectTransform.");
            return;
        }

        Vector2 mousePosition = Input.mousePosition;
        bool hit = RectTransformUtility.RectangleContainsScreenPoint(
            targetRect,
            mousePosition,
            uiCamera
        );

        Debug.Log(
            "ManualUIButtonDebug " + name +
            " mouse = " + mousePosition +
            " hit = " + hit
        );
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
