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

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float attackTimer = 0f;

    private void Awake()
    {
        if (towerSpriteRenderer == null)
        {
            towerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
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
            projectile.Initialize(target, damage, ownerResource);
        }
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
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
