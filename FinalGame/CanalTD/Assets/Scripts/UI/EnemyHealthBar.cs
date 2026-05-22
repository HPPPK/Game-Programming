/*
 * File: EnemyHealthBar.cs
 *
 * Purpose:
 * UI Image-based enemy health bar. It updates fill amount and can briefly flash
 * when the enemy takes damage.
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
