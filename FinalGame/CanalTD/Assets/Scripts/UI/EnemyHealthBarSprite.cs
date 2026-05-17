using System.Collections;
using UnityEngine;

public class EnemyHealthBarSprite : MonoBehaviour
{
    [Header("Bar Parts")]
    public Transform fillTransform;
    public SpriteRenderer fillRenderer;

    [Header("Color")]
    public Color normalColor = Color.green;
    public Color damageColor = Color.red;
    public float flashTime = 0.12f;

    [Header("Debug")]
    public float fallbackFullWidth = 0.6f;

    private float fullWidth;
    private float fullHeight;
    private float fullDepth;
    private Vector3 originalLocalPosition;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (fillTransform != null)
        {
            fullWidth = fillTransform.localScale.x;
            fullHeight = fillTransform.localScale.y;
            fullDepth = fillTransform.localScale.z;
            originalLocalPosition = fillTransform.localPosition;

            if (Mathf.Approximately(fullWidth, 0f))
            {
                fullWidth = fallbackFullWidth;
            }

            if (Mathf.Approximately(fullHeight, 0f))
            {
                fullHeight = 0.06f;
            }

            if (Mathf.Approximately(fullDepth, 0f))
            {
                fullDepth = 1f;
            }
        }

        if (fillRenderer != null)
        {
            fillRenderer.color = normalColor;
        }
    }

    public void SetHealth(int currentHP, int maxHP)
    {
        if (fillTransform == null)
        {
            return;
        }


        float ratio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;

        fillTransform.localScale = new Vector3(
            fullWidth * ratio,
            fullHeight,
            fullDepth
        );

        float lostWidth = fullWidth * (1f - ratio);
        fillTransform.localPosition = new Vector3(
            originalLocalPosition.x - lostWidth * 0.5f,
            originalLocalPosition.y,
            originalLocalPosition.z
        );

        if (fillRenderer != null)
        {
            fillRenderer.color = normalColor;
        }
    }

    public void PlayDamageFlash()
    {
        if (fillRenderer == null)
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        fillRenderer.color = damageColor;
        yield return new WaitForSeconds(flashTime);
        fillRenderer.color = normalColor;
    }
}
