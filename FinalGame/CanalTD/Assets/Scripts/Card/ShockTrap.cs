/*
 * File: ShockTrap.cs
 *
 * Purpose:
 * Runtime trap object placed by the Shock Trap card. It stays on the route until
 * an enemy enters its radius, then damages nearby enemies once, unregisters, and
 * destroys itself after the explosion animation.
 *
 * Notes:
 * The owner resource is passed into EnemyHealth.TakeDamage so kills and assists
 * still count for the player who placed the trap.
 */
using UnityEngine;
using System.Collections;

public class ShockTrap : MonoBehaviour
{
    [Header("Owner")]
    public int ownerPlayerId = 0;
    public PlayerResource ownerResource;

    [Header("Trap")]
    public float damage = 2f;
    public float radius = 0.75f;
    public float damageDelayAfterTrigger = 1f;
    public bool triggered = false;
    public LayerMask enemyLayer;
    public Transform routeNodeTransform;
    public bool countsAsActiveTrap = false;

    [Header("Explosion Animation")]
    public Animator animator;
    public string explodeTriggerName = "Explode";
    public float destroyDelayAfterExplode = 0.35f;

    private bool activeDuringWave = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

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

        TriggerTrap();
    }

    public void OnWaveStarted()
    {
        activeDuringWave = true;
    }

    public void OnWaveEnded()
    {
        activeDuringWave = false;
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

    private void TriggerTrap()
    {
        triggered = true;
        DisableColliders();
        Debug.Log("Shock Trap triggered");

        StartCoroutine(DamageAfterDelay());
    }

    private IEnumerator DamageAfterDelay()
    {
        yield return new WaitForSeconds(damageDelayAfterTrigger);

        EnemyHealth[] enemies = GetEnemiesInRadius();
        int finalDamage = Mathf.RoundToInt(damage);

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(finalDamage, ownerResource);
            }
        }

        UnregisterTrap();
        PlayExplosionAndDestroy();
    }

    private void DisableColliders()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D trapCollider in colliders)
        {
            if (trapCollider != null)
            {
                trapCollider.enabled = false;
            }
        }
    }

    private void PlayExplosionAndDestroy()
    {
        if (animator != null && !string.IsNullOrEmpty(explodeTriggerName))
        {
            animator.SetTrigger(explodeTriggerName);
            Destroy(gameObject, destroyDelayAfterExplode);
            return;
        }

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
