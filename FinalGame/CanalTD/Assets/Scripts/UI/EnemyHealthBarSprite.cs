/*
 * File: EnemyHealthBarSprite.cs
 *
 * Purpose:
 * Implements EnemyHealthBarSprite for the ui layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemyHealthBarSprite within the ui system.
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
 * - Verify EnemyHealthBarSprite in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
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
