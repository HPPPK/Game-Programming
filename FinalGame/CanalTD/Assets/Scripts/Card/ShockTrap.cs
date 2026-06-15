/*
 * File: ShockTrap.cs
 *
 * Purpose:
 * Implements ShockTrap for the card layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Card UI objects, targeting overlays, gate helpers, or card-related gameplay managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for ShockTrap within the card system.
 * - Update the owning object state and react to gameplay events during play.
 * - Handle card usage, targeting, hand state, or card-driven map interactions.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Card selections, targeting choices, turn permissions, and player hand data.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Applies card outcomes, targeting results, hand count changes, or card-related restrictions.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify ShockTrap in the scene or prefab where it is used and confirm the main happy path still works.
 * - Check local mode, AI mode, and online mode if the script participates in shared card flow.
 */
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class ShockTrap : MonoBehaviour
{
    [Header("Owner")]
    public int ownerPlayerId = 0;
    public PlayerResource ownerResource;

    [Header("Trap")]
    public float damage = 2f;
    [FormerlySerializedAs("radius")]
    public float triggerRadius = 0.75f;
    public float explosionRadius = 0.75f;
    public float damageDelayAfterTrigger = 1f;
    public bool triggered = false;
    public LayerMask enemyLayer;
    public Transform routeNodeTransform;
    public bool countsAsActiveTrap = false;

    [Header("Explosion Animation")]
    public Animator animator;
    public string explodeTriggerName = "Explode";
    public float destroyDelayAfterExplode = 0.35f;

    [Header("Owner Visual")]
    public SpriteRenderer trapRenderer;
    public bool useOwnerSprite = true;

    private bool activeDuringWave = false;
    private PlayerManager cachedPlayerManager;
    private Sprite ownerIdleSprite;

    /// <summary>
    /// Finds and stores shock trap references before scene gameplay begins.
    /// </summary>
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (trapRenderer == null)
        {
            trapRenderer = FindTrapRenderer();
        }
    }

    /// <summary>
    /// Initializes initialize and prepares the references needed before use.
    /// </summary>
    public void Initialize(int playerId, PlayerResource resource)
    {
        Initialize(playerId, resource, null);
    }

    /// <summary>
    /// Initializes initialize and prepares the references needed before use.
    /// </summary>
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

    /// <summary>
    /// Applies owner visual to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyOwnerVisual(PlayerManager playerManager)
    {
        cachedPlayerManager = playerManager;

        if (trapRenderer == null)
        {
            trapRenderer = FindTrapRenderer();
        }

        if (trapRenderer == null)
        {
            return;
        }

        if (useOwnerSprite && playerManager != null)
        {
            Sprite ownerSprite = playerManager.GetShockTrapSpriteForPlayer(ownerPlayerId);

            if (ownerSprite != null)
            {
                ownerIdleSprite = ownerSprite;
                ApplyOwnerIdleSprite();
            }
        }

        EnsureVisibleAlpha();
    }

    /// <summary>
    /// Keeps shock trap visuals aligned after normal frame updates finish.
    /// </summary>
    private void LateUpdate()
    {
        if (!triggered)
        {
            ApplyOwnerIdleSprite();
        }
    }

    /// <summary>
    /// Applies owner idle sprite to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyOwnerIdleSprite()
    {
        if (!useOwnerSprite)
        {
            return;
        }

        if (trapRenderer == null)
        {
            trapRenderer = FindTrapRenderer();
        }

        if (ownerIdleSprite == null && cachedPlayerManager != null)
        {
            ownerIdleSprite = cachedPlayerManager.GetShockTrapSpriteForPlayer(ownerPlayerId);
        }

        if (trapRenderer != null && ownerIdleSprite != null)
        {
            trapRenderer.sprite = ownerIdleSprite;
        }
    }

    /// <summary>
    /// Searches scene objects or cached lists to find trap renderer.
    /// </summary>
    private SpriteRenderer FindTrapRenderer()
    {
        Transform visual = transform.Find("Visual");

        if (visual != null)
        {
            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();

            if (visualRenderer != null)
            {
                return visualRenderer;
            }

            visualRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);

            if (visualRenderer != null)
            {
                return visualRenderer;
            }
        }

        return GetComponentInChildren<SpriteRenderer>(true);
    }

    /// <summary>
    /// Ensures visible alpha exists or is initialized before the flow continues.
    /// </summary>
    private void EnsureVisibleAlpha()
    {
        if (trapRenderer == null)
        {
            trapRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (trapRenderer == null)
        {
            return;
        }

        Color color = trapRenderer.color;
        color.a = 1f;
        trapRenderer.color = color;
    }

    /// <summary>
    /// Checks shock trap input, timing, animation, or UI state once per frame.
    /// </summary>
    private void Update()
    {
        if (!activeDuringWave || triggered)
        {
            return;
        }

        EnemyHealth[] enemies = GetEnemiesInRadius(triggerRadius);

        if (enemies.Length <= 0)
        {
            return;
        }

        TriggerTrap();
    }

    /// <summary>
    /// Responds to on wave started and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnWaveStarted()
    {
        activeDuringWave = true;
    }

    /// <summary>
    /// Responds to on wave ended and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnWaveEnded()
    {
        activeDuringWave = false;
    }

    // Shared radius query used separately for trigger detection and explosion damage.
    /// <summary>
    /// Returns enemies in radius used by card handling or target selection.
    /// </summary>
    private EnemyHealth[] GetEnemiesInRadius(float searchRadius)
    {
        if (enemyLayer.value != 0)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRadius, enemyLayer);
            /// <summary>
            /// Handles collect enemies from hits for shock trap.
            /// </summary>
            return CollectEnemiesFromHits(hits);
        }

        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        System.Collections.Generic.List<EnemyHealth> enemies = new System.Collections.Generic.List<EnemyHealth>();

        foreach (EnemyHealth enemy in allEnemies)
        {
            if (enemy != null && Vector2.Distance(transform.position, enemy.transform.position) <= searchRadius)
            {
                enemies.Add(enemy);
            }
        }

        return enemies.ToArray();
    }

    /// <summary>
    /// Handles collect enemies from hits for card state, hand state, or targeting.
    /// </summary>
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

    /// <summary>
    /// Handles trigger trap for card state, hand state, or targeting.
    /// </summary>
    private void TriggerTrap()
    {
        triggered = true;
        DisableColliders();
        Debug.Log("Shock Trap triggered");

        StartCoroutine(DamageAfterDelay());
    }

    /// <summary>
    /// Handles damage after delay for card state, hand state, or targeting.
    /// </summary>
    private IEnumerator DamageAfterDelay()
    {
        /// <summary>
        /// Handles wait for seconds for shock trap.
        /// </summary>
        yield return new WaitForSeconds(damageDelayAfterTrigger);

        EnemyHealth[] enemies = GetEnemiesInRadius(explosionRadius);
        int finalDamage = Mathf.RoundToInt(damage);

        if (!PhotonOnlineWaveCombatSyncManager.ShouldBlockLocalEnemyDamage())
        {
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.TakeDamage(finalDamage, ownerResource);
                }
            }
        }
        else
        {
            Debug.Log("Shock Trap visual trigger only on non-master online client.");
        }

        UnregisterTrap();
        PlayExplosionAndDestroy();
    }

    /// <summary>
    /// Handles disable colliders for card state, hand state, or targeting.
    /// </summary>
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

    /// <summary>
    /// Plays explosion and destroy in the current scene context.
    /// </summary>
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

    /// <summary>
    /// Removes shock trap listeners and temporary references before destruction.
    /// </summary>
    private void OnDestroy()
    {
        UnregisterTrap();
    }

    /// <summary>
    /// Unregisters trap so old callbacks or duplicate listeners cannot fire.
    /// </summary>
    private void UnregisterTrap()
    {
        if (routeNodeTransform != null)
        {
            ShockTrapTargetingManager.UnregisterTrap(routeNodeTransform, this);
            routeNodeTransform = null;
        }
    }

    /// <summary>
    /// Handles blocks placement for card state, hand state, or targeting.
    /// </summary>
    public bool BlocksPlacement()
    {
        return countsAsActiveTrap && !triggered;
    }

    /// <summary>
    /// Draws editor-only gizmos when this object is selected in the Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
