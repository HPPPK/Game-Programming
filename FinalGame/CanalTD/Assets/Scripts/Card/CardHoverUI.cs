/*
 * File: CardHoverUI.cs
 *
 * Purpose:
 * This is a small UI helper for card tooltips. It listens for pointer enter
 * and pointer exit events on a card UI object, then shows or hides the assigned
 * tooltip GameObject.
 *
 * Runtime behavior:
 * - OnPointerEnter() turns the tooltip on.
 * - OnPointerExit() turns the tooltip off.
 *
 * Inspector setup:
 * - tooltip should point to the UI panel/text object that explains the card.
 * - The same GameObject must be under a Canvas and must receive UI raycasts.
 *
 * Dependency notes:
 * - Requires Unity's EventSystem to be present in the scene.
 * - Does not control card selection or card play logic; that is handled by
 *   CardInstanceSelectable and CardDrawManager.
 */
using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip != null)
            tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip != null)
            tooltip.SetActive(false);
    }
}
