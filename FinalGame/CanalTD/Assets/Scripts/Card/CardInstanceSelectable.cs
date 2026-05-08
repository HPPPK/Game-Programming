/*
 * File: CardInstanceSelectable.cs
 *
 * Purpose:
 * This script lives on a card instance after it is drawn into the player's hand.
 * It remembers which original prefab created the card, reports click selection
 * back to CardDrawManager, and moves the UI card upward/downward to show whether
 * it is currently selected.
 *
 * Runtime behavior:
 * - Init() is called by CardDrawManager right after a card prefab is instantiated.
 * - OnPointerClick() sends this card instance back to CardDrawManager.SelectCard().
 * - SetSelected(true) moves the card up by moveUp pixels.
 * - SetSelected(false) restores the card to its original anchored position.
 *
 * Inspector/setup notes:
 * - Usually this script is placed on card prefabs.
 * - If the prefab does not have it, CardDrawManager adds it at runtime.
 * - The object must have a RectTransform because this script moves UI elements.
 *
 * Dependency notes:
 * - This class does not decide whether a card effect succeeds.
 * - CardDrawManager owns deck state, selected-card state, and card play effects.
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

    public void Init(CardDrawManager m, GameObject prefab, float moveDistance)
    {
        manager = m;
        sourcePrefab = prefab;
        moveUp = moveDistance;

        rect = GetComponent<RectTransform>();
        originalPos = rect.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
        {
            manager.SelectCard(this);
        }
    }

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

    public bool CanSelect()
    {
        return manager != null && manager.CanSelectCards();
    }
}
