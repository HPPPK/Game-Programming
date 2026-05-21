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
