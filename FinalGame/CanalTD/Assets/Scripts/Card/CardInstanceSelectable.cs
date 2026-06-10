/*
 * File: CardInstanceSelectable.cs
 *
 * Purpose:
 * Implements CardInstanceSelectable for the card layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CardInstanceSelectable within the card system.
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
 * - Verify CardInstanceSelectable in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
 */
using UnityEngine;
using UnityEngine.EventSystems;

public class CardInstanceSelectable : MonoBehaviour, IPointerClickHandler
{
    public GameObject sourcePrefab;

    private CardDrawManager manager;
    private RectTransform rect;
    private Vector2 originalPos;
    private float moveUp;

    /// <summary>
    /// Handles init for card state, hand state, or targeting.
    /// </summary>
    public void Init(CardDrawManager m, GameObject prefab, float moveDistance)
    {
        manager = m;
        sourcePrefab = prefab;
        moveUp = moveDistance;

        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
    }

    /// <summary>
    /// Responds to a UI click and forwards it to the related menu or gameplay action.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.SelectCard(this);
        }
    }

    /// <summary>
    /// Sets selected and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (rect == null)
        {
            rect = GetComponent<RectTransform>();
        }

        if (selected)
        {
            rect.anchoredPosition = originalPos + new Vector2(0, moveUp);
        }
        else
        {
            rect.anchoredPosition = originalPos;
        }
    }

    /// <summary>
    /// Checks whether select is allowed before enabling that action.
    /// </summary>
    public bool CanSelect()
    {
        return manager != null && manager.CanSelectCards();
    }
}
