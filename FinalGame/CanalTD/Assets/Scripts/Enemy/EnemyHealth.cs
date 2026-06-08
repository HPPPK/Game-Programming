/*
 * File: EnemyHealth.cs
 *
 * Purpose:
 * Implements EnemyHealth for the enemy layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Enemy prefabs, wave helpers, or enemy-related scene managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemyHealth within the enemy system.
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
 * - Verify EnemyHealth in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyHealth : MonoBehaviour
{
    [Header("Fallback Health")]
    [Tooltip("Used only when this enemy prefab has no EnemyStats component.")]
    public int fallbackMaxHP = 3;

    [Header("Fallback Reward")]
    [Tooltip("Used only when this enemy prefab has no EnemyStats component.")]
    public int fallbackGoldReward = 1;
    [FormerlySerializedAs("scoreReward")]
    [Tooltip("Used only when this enemy prefab has no EnemyStats component.")]
    public int fallbackKillerScoreReward = 2;
    public int assistScoreReward = 1;

    [Header("Optional Death Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite deathSprite;
    [Tooltip("Optional. Only assign this if a legacy enemy prefab still uses Animator-based death animation.")]
    public Animator animator;
    public float destroyDelay = 0.5f;

    [Header("Health Bar")]
    public EnemyHealthBarSprite healthBar;
    public bool autoPositionHealthBar = true;
    public float healthBarVerticalPadding = 0.12f;

    private bool isDead = false;
    private int maxHP = 3;
    private int currentHP = 3;
    private int goldReward = 1;
    private int killerScoreReward = 2;
    private PlayerResource lastDamageOwner;
    private HashSet<PlayerResource> damageParticipants = new HashSet<PlayerResource>();
    private EnemyStats enemyStats;
    private int appliedRound = 1;
    private float appliedHpScalePerRound = 0f;
    private float appliedSpeedScalePerRound = 0f;

    public int CurrentHP
    {
        get { return currentHP; }
    }

    public int MaxHP
    {
        get { return maxHP; }
    }

    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        maxHP = Mathf.Max(1, fallbackMaxHP);
        currentHP = maxHP;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = FindEnemySpriteRenderer();
        }

        // Animator is optional. Most enemy prefabs can use SimpleFrameAnimator on
        // the Visual object and leave this field empty.
        ResolveOptionalAnimator();
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

    public void InitializeFromStats(int round)
    {
        InitializeFromStats(round, appliedHpScalePerRound, appliedSpeedScalePerRound);
    }

    public void InitializeFromStats(int round, float hpScalePerRound, float speedScalePerRound)
    {
        enemyStats = GetComponent<EnemyStats>();
        appliedRound = Mathf.Max(1, round);
        appliedHpScalePerRound = hpScalePerRound;
        appliedSpeedScalePerRound = speedScalePerRound;

        if (enemyStats != null)
        {
            maxHP = enemyStats.GetFinalHP(appliedRound, appliedHpScalePerRound);
            currentHP = maxHP;
            goldReward = enemyStats.GetFinalReward(appliedRound);
            killerScoreReward = enemyStats.GetFinalScoreReward(appliedRound);
        }
        else
        {
            maxHP = Mathf.Max(1, fallbackMaxHP);
            currentHP = maxHP;
            goldReward = fallbackGoldReward;
            killerScoreReward = fallbackKillerScoreReward;
        }

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

        int finalDamage = enemyStats != null ? enemyStats.GetFinalDamage(damage) : Mathf.Max(1, damage);
        currentHP -= finalDamage;

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
        AudioManager.Instance?.PlayEnemyDeath();

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

        SpawnSplitEnemies();
        StartCoroutine(DestroyAfterDelay());
    }

    private void ResolveOptionalAnimator()
    {
        if (animator != null)
        {
            return;
        }

        animator = GetComponent<Animator>();

        if (animator == null && spriteRenderer != null)
        {
            animator = spriteRenderer.GetComponent<Animator>();
        }
    }

    private void SpawnSplitEnemies()
    {
        if (enemyStats == null ||
            !enemyStats.canSplit ||
            enemyStats.splitEnemyPrefab == null ||
            enemyStats.splitCount <= 0)
        {
            return;
        }

        for (int i = 0; i < enemyStats.splitCount; i++)
        {
            Vector3 offset = Random.insideUnitCircle * 0.2f;
            Vector3 spawnPosition = transform.position + offset;
            GameObject splitEnemy = Instantiate(
                enemyStats.splitEnemyPrefab,
                spawnPosition,
                Quaternion.identity
            );

            EnemyMover parentMover = GetComponent<EnemyMover>();
            EnemyMover splitMover = splitEnemy.GetComponent<EnemyMover>();
            EnemyHealth splitHealth = splitEnemy.GetComponent<EnemyHealth>();

            if (splitHealth != null)
            {
                splitHealth.InitializeFromStats(
                    appliedRound,
                    appliedHpScalePerRound,
                    appliedSpeedScalePerRound
                );
            }

            if (splitMover != null)
            {
                splitMover.ApplyStatsSpeed(appliedRound, appliedSpeedScalePerRound);
            }

            if (parentMover != null && splitMover != null)
            {
                splitMover.CopyPathProgressFrom(parentMover, spawnPosition);
            }
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}