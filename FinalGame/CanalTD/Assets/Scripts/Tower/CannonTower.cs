/*
 * File: CannonTower.cs
 *
 * Purpose:
 * Implements CannonTower for the tower layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Tower prefabs, build spots, projectiles, or tower-related scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CannonTower within the tower system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify CannonTower in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;

public class CannonTower : MonoBehaviour
{
    [Header("Owner")]
    public int ownerPlayerId = 0;
    public PlayerResource ownerResource;
    public SpriteRenderer towerSpriteRenderer;

    [Header("Attack")]
    public float attackRange = 3f;
    public float attackInterval = 0.6f;
    public int damage = 1;
    public float baseDamage = 1f;
    public float boostedDamage = 2f;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Power Boost")]
    public bool boostPendingForNextWave = false;
    public bool boostActive = false;
    public Color boostActiveTint = new Color(1f, 0.8f, 0.25f, 1f);
    public GameObject boostIcon;

    private float attackTimer = 0f;
    private bool disabledByFreeze = false;
    private Color normalTowerColor = Color.white;
    private TowerStats towerStats;

    /// <summary>
    /// Finds and stores cannon tower references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        towerStats = GetComponent<TowerStats>();

        if (towerStats == null)
        {
            towerStats = GetComponentInChildren<TowerStats>();
        }

        if (towerSpriteRenderer == null)
        {
            towerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (towerSpriteRenderer != null)
        {
            normalTowerColor = towerSpriteRenderer.color;
        }

        SetBoostVisual(false);
    }

    /// <summary>
    /// Checks cannon tower input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        if (disabledByFreeze)
        {
            return;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            SyncAttackFieldsFromStats();
            EnemyHealth target = FindTargetEnemy();

            if (target != null)
            {
                AttackTarget(target);
                attackTimer = attackInterval;
            }
        }
    }

    /// <summary>
    /// Searches scene objects or cached lists to find target enemy.
    /// </summary>
    private EnemyHealth FindTargetEnemy()
    {
        SyncAttackFieldsFromStats();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        EnemyHealth bestEnemy = null;
        float bestScore = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            if (enemy == null)
            {
                enemy = hit.GetComponentInParent<EnemyHealth>();
            }

            if (enemy == null)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            float score = GetTargetScore(enemy, distance);

            if (score < bestScore)
            {
                bestScore = score;
                bestEnemy = enemy;
            }
        }

        return bestEnemy;
    }

    /// <summary>
    /// Returns target score used by card handling or target selection.
    /// </summary>
    private float GetTargetScore(EnemyHealth enemy, float distance)
    {
        TargetPriority priority = towerStats != null ? towerStats.targetPriority : TargetPriority.Closest;

        if (priority == TargetPriority.Strongest)
        {
            return -enemy.CurrentHP;
        }

        if (priority == TargetPriority.Weakest)
        {
            return enemy.CurrentHP;
        }

        if (priority == TargetPriority.First)
        {
            // V1 approximation: enemies farther from this tower are treated as farther along the route.
            return -distance;
        }

        return distance;
    }

    /// <summary>
    /// Handles attack target for card state, hand state, or targeting.
    /// </summary>
    private void AttackTarget(EnemyHealth target)
    {
        if (target == null)
        {
            return;
        }

        TowerType towerType = towerStats != null ? towerStats.towerType : TowerType.Cannon;

        if (towerType == TowerType.Cannon)
        {
            ApplyCannonDamage(target);
            return;
        }

        if (towerType == TowerType.Frost)
        {
            ApplySingleTargetDamage(target);
            ApplySlow(target);
            return;
        }

        if (towerType == TowerType.Shock)
        {
            ApplyShockDamage(target);
            return;
        }

        ApplySingleTargetDamage(target);
    }

    /// <summary>
    /// Applies single target damage to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplySingleTargetDamage(EnemyHealth target)
    {
        ShootProjectileOrDamage(target);
    }

    /// <summary>
    /// Applies cannon damage to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyCannonDamage(EnemyHealth target)
    {
        float radius = towerStats != null ? towerStats.splashRadius : 0f;

        if (radius <= 0f)
        {
            ShootProjectileOrDamage(target);
            return;
        }

        AudioManager.Instance?.PlayTowerShoot();

        foreach (EnemyHealth enemy in GetEnemiesNear(target.transform.position, radius))
        {
            enemy.TakeDamage(Mathf.RoundToInt(GetCurrentDamage()), ownerResource);
        }
    }

    /// <summary>
    /// Applies shock damage to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyShockDamage(EnemyHealth target)
    {
        int maxTargets = towerStats != null ? Mathf.Max(1, towerStats.chainCount + 1) : 1;
        float radius = towerStats != null ? towerStats.chainRange : attackRange;
        int appliedCount = 0;

        AudioManager.Instance?.PlayTowerShoot();

        foreach (EnemyHealth enemy in GetEnemiesNear(target.transform.position, radius))
        {
            enemy.TakeDamage(Mathf.RoundToInt(GetCurrentDamage()), ownerResource);
            appliedCount++;

            if (appliedCount >= maxTargets)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Applies slow to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplySlow(EnemyHealth target)
    {
        EnemySlowEffect slowEffect = target.GetComponent<EnemySlowEffect>();

        if (slowEffect == null)
        {
            slowEffect = target.gameObject.AddComponent<EnemySlowEffect>();
        }

        float slowPercent = towerStats != null ? towerStats.slowPercent : 0.35f;
        float slowDuration = towerStats != null ? towerStats.slowDuration : 1.5f;
        slowEffect.ApplySlow(slowPercent, slowDuration);
    }

    /// <summary>
    /// Returns enemies near used by land, tower, cost, or build decisions.
    /// </summary>
    private System.Collections.Generic.List<EnemyHealth> GetEnemiesNear(Vector3 center, float radius)
    {
        System.Collections.Generic.List<EnemyHealth> enemies = new System.Collections.Generic.List<EnemyHealth>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

            if (enemy == null)
            {
                enemy = hit.GetComponentInParent<EnemyHealth>();
            }

            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        return enemies;
    }

    /// <summary>
    /// Handles shoot projectile or damage for land ownership, tower actions, or build UI.
    /// </summary>
    private void ShootProjectileOrDamage(EnemyHealth target)
    {
        if (projectilePrefab == null || target == null)
        {
            if (target != null)
            {
                target.TakeDamage(Mathf.RoundToInt(GetCurrentDamage()), ownerResource);
                AudioManager.Instance?.PlayTowerShoot();
            }

            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        AudioManager.Instance?.PlayTowerShoot();

        TowerProjectile projectile = projectileObject.GetComponent<TowerProjectile>();

        if (projectile != null)
        {
            float currentDamage = GetCurrentDamage();
            Debug.Log("Current tower damage = " + currentDamage);
            projectile.Initialize(target, Mathf.RoundToInt(currentDamage), ownerResource);
        }
    }

    /// <summary>
    /// Returns current damage used by land, tower, cost, or build decisions.
    /// </summary>
    public float GetCurrentDamage()
    {
        SyncAttackFieldsFromStats();
        return boostActive ? boostedDamage : baseDamage;
    }

    /// <summary>
    /// Handles sync attack fields from stats for land ownership, tower actions, or build UI.
    /// </summary>
    public void SyncAttackFieldsFromStats()
    {
        if (towerStats == null)
        {
            towerStats = GetComponent<TowerStats>();
        }

        if (towerStats == null)
        {
            return;
        }

        attackRange = towerStats.range;
        attackInterval = towerStats.attackInterval;
        baseDamage = towerStats.damage;
        damage = Mathf.RoundToInt(towerStats.damage);
    }

    /// <summary>
    /// Applies owner visual to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyOwnerVisual(PlayerManager playerManager, int playerId)
    {
        ApplyOwnerVisual(playerManager, playerId, false);
    }

    /// <summary>
    /// Applies owner visual to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyOwnerVisual(PlayerManager playerManager, int playerId, bool applyFallbackVisual)
    {
        if (towerSpriteRenderer == null)
        {
            towerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (towerSpriteRenderer == null)
        {
            return;
        }

        if (applyFallbackVisual && playerManager != null)
        {
            Sprite towerSprite = playerManager.GetTowerSpriteForPlayer(playerId);

            if (towerSprite != null)
            {
                towerSpriteRenderer.sprite = towerSprite;
            }
        }

        Color color = towerSpriteRenderer.color;
        color.a = 1f;
        towerSpriteRenderer.color = color;
        normalTowerColor = color;

        if (boostActive)
        {
            SetBoostVisual(true);
        }
    }

    /// <summary>
    /// Sets disabled by freeze and immediately updates the related state, UI, or visuals.
    /// </summary>
    public void SetDisabledByFreeze(bool disabled)
    {
        disabledByFreeze = disabled;
    }

    /// <summary>
    /// Checks the current state to decide whether disabled by freeze is true.
    /// </summary>
    public bool IsDisabledByFreeze()
    {
        return disabledByFreeze;
    }

    /// <summary>
    /// Applies power boost for next wave to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyPowerBoostForNextWave()
    {
        if (boostPendingForNextWave || boostActive)
        {
            return;
        }

        boostPendingForNextWave = true;
    }

    /// <summary>
    /// Responds to on wave started and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnWaveStarted()
    {
        if (boostPendingForNextWave)
        {
            boostPendingForNextWave = false;
            boostActive = true;
            SetBoostVisual(true);
            Debug.Log("Power Boost activated on tower");
        }
    }

    /// <summary>
    /// Responds to on wave ended and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnWaveEnded()
    {
        if (boostActive || boostPendingForNextWave)
        {
            Debug.Log("Power Boost ended");
        }

        boostPendingForNextWave = false;
        boostActive = false;
        SetBoostVisual(false);
    }

    /// <summary>
    /// Sets boost visual and immediately updates the related state, UI, or visuals.
    /// </summary>
    private void SetBoostVisual(bool active)
    {
        // 只控制 boost icon 的显示/隐藏，不再改变塔的颜色
        if (boostIcon != null)
        {
            boostIcon.SetActive(active);
        }
    }

    /// <summary>
    /// Draws editor-only gizmos when this object is selected in the Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
