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
