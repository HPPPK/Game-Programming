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

        if (animator == null)
        {
            animator = GetComponent<Animator>();
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
            healthBar.SetHealth(currentHP, maxHP);
        }
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
            healthBar.SetHealth(currentHP, maxHP);
        }
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
