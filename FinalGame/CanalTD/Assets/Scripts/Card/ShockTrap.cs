using UnityEngine;

public class ShockTrap : MonoBehaviour
{
    public int ownerPlayerId = 0;
    public PlayerResource ownerResource;
    public float damage = 2f;
    public float radius = 0.75f;
    public bool triggered = false;
    public LayerMask enemyLayer;
    public Transform routeNodeTransform;
    public bool countsAsActiveTrap = false;

    private bool activeDuringWave = false;

    public void Initialize(int playerId, PlayerResource resource)
    {
        Initialize(playerId, resource, null);
    }

    public void Initialize(int playerId, PlayerResource resource, Transform routeNode)
    {
        ownerPlayerId = playerId;
        ownerResource = resource;
        routeNodeTransform = routeNode;
        countsAsActiveTrap = true;

        if (routeNodeTransform != null)
        {
            ShockTrapTargetingManager.RegisterTrap(routeNodeTransform, this);
        }
    }

    private void Update()
    {
        if (!activeDuringWave || triggered)
        {
            return;
        }

        EnemyHealth[] enemies = GetEnemiesInRadius();

        if (enemies.Length <= 0)
        {
            return;
        }

        TriggerTrap(enemies);
    }

    public void OnWaveStarted()
    {
        activeDuringWave = true;
    }

    public void OnWaveEnded()
    {
        if (!triggered)
        {
            UnregisterTrap();
            Destroy(gameObject);
        }
    }

    private EnemyHealth[] GetEnemiesInRadius()
    {
        if (enemyLayer.value != 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyLayer);
            return CollectEnemiesFromHits(hits);
        }

        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        System.Collections.Generic.List<EnemyHealth> enemies = new System.Collections.Generic.List<EnemyHealth>();

        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy != null && Vector2.Distance(transform.position, enemy.transform.position) <= radius)
            {
                enemies.Add(enemy);
            }
        }

        return enemies.ToArray();
    }

    private EnemyHealth[] CollectEnemiesFromHits(Collider2D[] hits)
    {
        System.Collections.Generic.List<EnemyHealth> enemies = new System.Collections.Generic.List<EnemyHealth>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

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

        return enemies.ToArray();
    }

    private void TriggerTrap(EnemyHealth[] enemies)
    {
        triggered = true;
        Debug.Log("Shock Trap triggered");
        int finalDamage = Mathf.RoundToInt(damage);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage, ownerResource);
            }
        }

        UnregisterTrap();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        UnregisterTrap();
    }

    private void UnregisterTrap()
    {
        if (routeNodeTransform != null)
        {
            ShockTrapTargetingManager.UnregisterTrap(routeNodeTransform, this);
            routeNodeTransform = null;
        }
    }

    public bool BlocksPlacement()
    {
        return countsAsActiveTrap && !triggered;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
