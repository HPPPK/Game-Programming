/*
 * File: CastleHealthBar.cs
 *
 * Purpose:
 * Implements CastleHealthBar for the ui layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CastleHealthBar within the ui system.
 * - Update the owning object state and react to gameplay events during play.
 * - Present readable feedback so players can understand turns, actions, and results.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Player input, button clicks, pointer events, or scene transition requests.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Updates visible UI, indicators, prompts, and player-facing status messages.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify CastleHealthBar in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
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
