/*
 * File: EnemyHealth.cs
 *
 * Purpose:
 * Stores enemy HP, applies wave stats, receives tower/trap damage, updates the
 * health bar, and awards gold/score when the enemy dies.
 *
 * Notes:
 * Damage participants are tracked so the last hitter receives kill score and
 * other unique contributors receive assist score.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 3;
    public int currentHP;

    [Header("Reward")]
    public int goldReward = 1;
    [FormerlySerializedAs("scoreReward")]
    public int killerScoreReward = 2;
    public int assistScoreReward = 1;

    [Header("Death Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite deathSprite;
    public Animator animator;
    public float destroyDelay = 0.5f;

    [Header("Health Bar")]
    public EnemyHealthBarSprite healthBar;
    public bool autoPositionHealthBar = true;
    public float healthBarVerticalPadding = 0.12f;

    private bool isDead = false;
    private PlayerResource lastDamageOwner;
    private HashSet<PlayerResource> damageParticipants = new HashSet<PlayerResource>();

    private void Awake()
    {
        currentHP = maxHP;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = FindEnemySpriteRenderer();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null && spriteRenderer != null)
        {
            animator = spriteRenderer.GetComponent<Animator>();
        }
    }

    private void Start()
    {
        if (currentHP <= 0)
        {
            currentHP = maxHP;
        }

        if (healthBar != null)
        {
            PositionHealthBarAboveSprite();
            healthBar.SetHealth(currentHP, maxHP);
        }
    }

    private void LateUpdate()
    {
        PositionHealthBarAboveSprite();
    }

    public void ApplyWaveStats(WaveConfig config)
    {
        if (config == null)
        {
            return;
        }

        maxHP = Mathf.Max(1, config.enemyMaxHP);
        currentHP = maxHP;
        goldReward = config.goldReward;
        killerScoreReward = config.killerScoreReward;
        assistScoreReward = config.assistScoreReward;

        isDead = false;
        lastDamageOwner = null;
        damageParticipants.Clear();

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            PositionHealthBarAboveSprite();
            healthBar.SetHealth(currentHP, maxHP);
        }
    }

    private SpriteRenderer FindEnemySpriteRenderer()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            if (healthBar != null && renderer.transform.IsChildOf(healthBar.transform))
            {
                continue;
            }

            return renderer;
        }

        return null;
    }

    private void PositionHealthBarAboveSprite()
    {
        if (!autoPositionHealthBar || healthBar == null || spriteRenderer == null)
        {
            return;
        }

        Bounds bounds = spriteRenderer.bounds;
        Vector3 worldPosition = new Vector3(
            bounds.center.x,
            bounds.max.y + healthBarVerticalPadding,
            healthBar.transform.position.z
        );

        healthBar.transform.localPosition = transform.InverseTransformPoint(worldPosition);
    }

    public void TakeDamage(int damage, PlayerResource damageOwner)
    {
        if (isDead)
        {
            return;
        }

        currentHP -= damage;

        if (damageOwner != null)
        {
            damageParticipants.Add(damageOwner);
            lastDamageOwner = damageOwner;
        }

        if (currentHP < 0)
        {
            currentHP = 0;
        }

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHP, maxHP);
            healthBar.PlayDamageFlash();
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (lastDamageOwner != null)
        {
            lastDamageOwner.AddMoney(goldReward);
            lastDamageOwner.AddScore(killerScoreReward);
        }

        foreach (PlayerResource participant in damageParticipants)
        {
            if (participant != null && participant != lastDamageOwner)
            {
                participant.AddScore(assistScoreReward);
            }
        }

        Collider2D collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            collider2D.enabled = false;
        }

        EnemyMover mover = GetComponent<EnemyMover>();
        if (mover != null)
        {
            mover.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (spriteRenderer != null && deathSprite != null)
        {
            spriteRenderer.sprite = deathSprite;
        }

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
