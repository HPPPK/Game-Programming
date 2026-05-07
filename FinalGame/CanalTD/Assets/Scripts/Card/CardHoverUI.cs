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