/*
 * File: TurnMarkerAnimator.cs
 *
 * Purpose:
 * Handles the floating animation for castle turn markers.
 */
using UnityEngine;

public class TurnMarkerAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float floatHeight = 0.15f;
    public float floatSpeed = 2f;
    public float scaleAmount = 0.15f;
    public float scaleSpeed = 2f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private bool isAnimating = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        Debug.Log($"originalPosition: {originalPosition}");
        Debug.Log($"originalScale: {originalScale}");
    }

    private void Update()
    {
        if (!isAnimating) return;

        // Float animation
        float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.localPosition = originalPosition + new Vector3(0, floatOffset, 0);

        // Scale animation
        float scaleOffset = 1 + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
        transform.localScale = originalScale * scaleOffset;
    }

    public void Show()
    {
        isAnimating = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        isAnimating = false;
        gameObject.SetActive(false);
    }

    public void SetMarkerColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public void ResetMarkerColor()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
