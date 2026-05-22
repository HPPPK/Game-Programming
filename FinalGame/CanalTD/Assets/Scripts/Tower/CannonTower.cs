/*
 * File: CannonTower.cs
 *
 * Purpose:
 * Finds enemies in range, fires projectiles, stores owner player data, and
 * supports temporary card effects such as Freeze Claim and Power Boost.
 *
 * Notes:
 * Power Boost changes outgoing damage only during the next wave. Freeze Claim
 * temporarily disables attacking without destroying the tower.
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

    private void Awake()
    {
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

    private void Update()
    {
        if (disabledByFreeze)
        {
            return;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            EnemyHealth target = FindNearestEnemy();

            if (target != null)
            {
                Shoot(target);
                attackTimer = attackInterval;
            }
        }
    }

    private EnemyHealth FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);

        EnemyHealth nearestEnemy = null;
        float nearestDistance = float.MaxValue;

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

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    private void Shoot(EnemyHealth target)
    {
        if (projectilePrefab == null || target == null)
        {
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        TowerProjectile projectile = projectileObject.GetComponent<TowerProjectile>();

        if (projectile != null)
        {
            float currentDamage = GetCurrentDamage();
            Debug.Log("Current tower damage = " + currentDamage);
            projectile.Initialize(target, Mathf.RoundToInt(currentDamage), ownerResource);
        }
    }

    public float GetCurrentDamage()
    {
        return boostActive ? boostedDamage : baseDamage;
    }

    public void ApplyOwnerVisual(PlayerManager playerManager, int playerId)
    {
        ApplyOwnerVisual(playerManager, playerId, false);
    }

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

    public void SetDisabledByFreeze(bool disabled)
    {
        disabledByFreeze = disabled;
    }

    public bool IsDisabledByFreeze()
    {
        return disabledByFreeze;
    }

    public void ApplyPowerBoostForNextWave()
    {
        if (boostPendingForNextWave || boostActive)
        {
            return;
        }

        boostPendingForNextWave = true;
    }

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

    private void SetBoostVisual(bool active)
    {
        if (towerSpriteRenderer != null)
        {
            Color color = active ? boostActiveTint : normalTowerColor;
            color.a = 1f;
            towerSpriteRenderer.color = color;
        }

        if (boostIcon != null)
        {
            boostIcon.SetActive(active);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
