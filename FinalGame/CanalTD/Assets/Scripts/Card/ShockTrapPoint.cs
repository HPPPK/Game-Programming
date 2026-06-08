/*
 * File: ShockTrapPoint.cs
 *
 * Purpose:
 * Implements ShockTrapPoint for the card layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for ShockTrapPoint within the card system.
 * - Update the owning object state and react to gameplay events during play.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify ShockTrapPoint in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
 */
using UnityEngine;

public class ShockTrapPoint : MonoBehaviour
{
    public SpriteRenderer highlightRenderer;
    public Color normalColor = Color.white;
    public Color validHighlightColor = new Color(0.4f, 1f, 1f, 1f);
    public Color selectedColor = new Color(1f, 0.9f, 0.1f, 1f);
    public bool hasTrap = false;

    private Color originalColor = Color.white;
    private int originalSortingOrder = 0;

    private void Awake()
    {
        if (highlightRenderer == null)
        {
            highlightRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (highlightRenderer != null)
        {
            originalColor = highlightRenderer.color;
            originalSortingOrder = highlightRenderer.sortingOrder;
        }
    }

    public void ShowValid(int sortingOrder)
    {
        ApplyVisual(validHighlightColor, sortingOrder);
    }

    public void ShowSelected(int sortingOrder)
    {
        ApplyVisual(selectedColor, sortingOrder);
    }

    public void RestoreVisual()
    {
        if (highlightRenderer == null)
        {
            return;
        }

        highlightRenderer.color = originalColor;
        highlightRenderer.sortingOrder = originalSortingOrder;
    }

    private void ApplyVisual(Color color, int sortingOrder)
    {
        if (highlightRenderer == null)
        {
            return;
        }

        Color visibleColor = color;
        visibleColor.a = 1f;
        highlightRenderer.color = visibleColor;
        highlightRenderer.sortingOrder = sortingOrder;
    }
}
