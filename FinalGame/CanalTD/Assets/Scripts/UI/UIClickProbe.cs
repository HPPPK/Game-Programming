/*
 * File: UIClickProbe.cs
 *
 * Purpose:
 * Small debug helper for checking whether a UI object receives EventSystem
 * pointer events during Play mode.
 */
using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickProbe : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("UI Pointer Down: " + gameObject.name);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("UI Click: " + gameObject.name);
    }
}
