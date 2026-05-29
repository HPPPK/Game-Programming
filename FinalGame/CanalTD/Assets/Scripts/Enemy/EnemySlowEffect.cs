/*
 * File: EnemySlowEffect.cs
 *
 * Purpose:
 * Applies temporary slow effects to enemies. Repeated slows refresh duration and
 * use the same original speed so slow effects do not multiply infinitely.
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
