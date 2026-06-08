/*
 * File: EnemySlowEffect.cs
 *
 * Purpose:
 * Implements EnemySlowEffect for the enemy layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Enemy prefabs, wave helpers, or enemy-related scene managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemySlowEffect within the enemy system.
 * - Update the owning object state and react to gameplay events during play.
 * - Support enemy spawning, pathing, targeting, combat, or wave pressure behaviour.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Path graph data, wave settings, target selection, or movement parameters.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Moves enemies, adjusts combat results, or influences castle pressure and wave outcomes.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify EnemySlowEffect in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections;
using UnityEngine;

public class EnemySlowEffect : MonoBehaviour
{
    private EnemyMover mover;
    private Coroutine slowCoroutine;
    private float originalSpeed;
    private bool hasOriginalSpeed = false;

    public void ApplySlow(float percent, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        if (mover == null)
        {
            mover = GetComponent<EnemyMover>();
        }

        if (mover == null)
        {
            return;
        }

        if (!hasOriginalSpeed)
        {
            originalSpeed = mover.moveSpeed;
            hasOriginalSpeed = true;
        }

        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        float safePercent = Mathf.Clamp01(percent);
        mover.moveSpeed = Mathf.Max(0.01f, originalSpeed * (1f - safePercent));
        slowCoroutine = StartCoroutine(RestoreAfterDelay(duration));
    }

    private IEnumerator RestoreAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (mover != null && hasOriginalSpeed)
        {
            mover.moveSpeed = originalSpeed;
        }

        slowCoroutine = null;
    }
}
