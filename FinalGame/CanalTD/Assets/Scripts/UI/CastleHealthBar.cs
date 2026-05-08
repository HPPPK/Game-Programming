/*
 * File: CastleHealthBar.cs
 *
 * Purpose:
 * This script displays and animates a castle's HP bar. It reads health values
 * from CastleBase, scales the fill object, updates optional text, and plays a
 * short damage reaction when HP decreases.
 *
 * Runtime behavior:
 * - Start() records the original fill/bar transform values.
 * - Update() compares current HP with lastHP to detect damage.
 * - UpdateBarSmooth() smoothly changes the fill scale and position.
 * - UpdateBarInstant() sets the correct visual value immediately at startup.
 * - DamageFlashRoutine() briefly turns the fill red and scales the bar up/down.
 *
 * Inspector setup:
 * - castleBase should point to the CastleBase this UI represents.
 * - fill should be the transform that visually shrinks as HP decreases.
 * - hpTextUI is optional and shows "current/max" HP.
 * - smoothSpeed controls interpolation speed.
 * - punchScale, punchDuration, and recoverDuration control hit feedback.
 *
 * Dependency notes:
 * - CastleBase owns the actual HP numbers.
 * - This script only displays HP; it does not apply damage or decide game over.
 */
using UnityEngine;
using TMPro;
using System.Collections;

public class CastleHealthBar : MonoBehaviour
{
    public CastleBase castleBase;
    public Transform fill;
    public TextMeshProUGUI hpTextUI;

    [Header("Animation")]
    public float smoothSpeed = 8f;
    public float punchScale = 1.25f;
    public float punchDuration = 0.12f;
    public float recoverDuration = 0.18f;

    private Vector3 originalFillScale;
    private Vector3 originalFillPosition;
    private Vector3 originalBarScale;

    private SpriteRenderer fillRenderer;
    private Color originalColor;

    private int lastHP;
    private Coroutine damageRoutine;

    void Start()
    {
        originalFillScale = fill.localScale;
        originalFillPosition = fill.localPosition;
        originalBarScale = transform.localScale;

        fillRenderer = fill.GetComponent<SpriteRenderer>();
        if (fillRenderer != null)
        {
            originalColor = fillRenderer.color;
        }

        if (hpTextUI != null)
        {
            hpTextUI.gameObject.SetActive(true);
        }

        if (castleBase != null)
        {
            lastHP = castleBase.currentHP;
        }

        UpdateBarInstant();
    }

    void Update()
    {
        if (castleBase == null || fill == null) return;

        if (castleBase.currentHP < lastHP)
        {
            if (damageRoutine != null)
            {
                StopCoroutine(damageRoutine);
            }

            damageRoutine = StartCoroutine(DamageFlashRoutine());
        }

        lastHP = castleBase.currentHP;

        UpdateBarSmooth();
    }

    void UpdateBarSmooth()
    {
        float ratio = (float)castleBase.currentHP / castleBase.maxHP;
        ratio = Mathf.Clamp01(ratio);

        Vector3 targetScale = new Vector3(
            originalFillScale.x * ratio,
            originalFillScale.y,
            originalFillScale.z
        );

        fill.localScale = Vector3.Lerp(
            fill.localScale,
            targetScale,
            Time.deltaTime * smoothSpeed
        );

        float offset = (1f - ratio) * originalFillScale.x / 2f;
        Vector3 targetPosition = originalFillPosition + new Vector3(-offset, 0, 0);

        fill.localPosition = Vector3.Lerp(
            fill.localPosition,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );

        if (hpTextUI != null)
        {
            hpTextUI.text = castleBase.currentHP + "/" + castleBase.maxHP;
        }
    }

    void UpdateBarInstant()
    {
        if (castleBase == null || fill == null) return;

        float ratio = (float)castleBase.currentHP / castleBase.maxHP;
        ratio = Mathf.Clamp01(ratio);

        fill.localScale = new Vector3(
            originalFillScale.x * ratio,
            originalFillScale.y,
            originalFillScale.z
        );

        float offset = (1f - ratio) * originalFillScale.x / 2f;
        fill.localPosition = originalFillPosition + new Vector3(-offset, 0, 0);

        if (hpTextUI != null)
        {
            hpTextUI.text = castleBase.currentHP + "/" + castleBase.maxHP;
        }
    }

    IEnumerator DamageFlashRoutine()
    {
        if (fillRenderer != null)
        {
            fillRenderer.color = Color.red;
        }

        transform.localScale = originalBarScale * punchScale;

        yield return new WaitForSeconds(punchDuration);

        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < recoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / recoverDuration;

            transform.localScale = Vector3.Lerp(startScale, originalBarScale, t);

            yield return null;
        }

        transform.localScale = originalBarScale;

        if (fillRenderer != null)
        {
            fillRenderer.color = originalColor;
        }
    }
}
