/*
 * File: EnemyHealthBar.cs
 *
 * Purpose:
 * Implements EnemyHealthBar for the ui layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Canvas objects, scene UI roots, status panels, buttons, or cursor feedback objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemyHealthBar within the ui system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify EnemyHealthBar in the scene or prefab where it is used and confirm the main happy path still works.
 * - Confirm the related UI remains readable in both the normal scene flow and edge/error states.
 */
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI")]
    public Image fillImage;

    [Header("Damage Flash")]
    public Color normalColor = Color.green;
    public Color damageColor = Color.red;
    public float flashTime = 0.12f;

    private Coroutine flashCoroutine;

    public void SetHealth(int currentHP, int maxHP)
    {
        if (fillImage == null)
        {
            return;
        }

        float ratio = 0f;

        if (maxHP > 0)
        {
            ratio = Mathf.Clamp01((float)currentHP / maxHP);
        }

        fillImage.fillAmount = ratio;
    }

    public void PlayDamageFlash()
    {
        if (fillImage == null)
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
        fillImage.color = damageColor;
        yield return new WaitForSeconds(flashTime);
        fillImage.color = normalColor;
    }
}
