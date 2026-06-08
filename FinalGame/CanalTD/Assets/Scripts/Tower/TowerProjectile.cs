/*
 * File: TowerProjectile.cs
 *
 * Purpose:
 * Implements TowerProjectile for the tower layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Tower prefabs, build spots, projectiles, or tower-related scene controllers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for TowerProjectile within the tower system.
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
 * - Verify TowerProjectile in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;

public class TowerProjectile : MonoBehaviour
{
    [Header("Projectile")]
    public float speed = 8f;
    public float hitDistance = 0.12f;

    private EnemyHealth target;
    private int damage;
    private PlayerResource damageOwner;

    public void Initialize(EnemyHealth targetEnemy, int projectileDamage, PlayerResource owner)
    {
        target = targetEnemy;
        damage = projectileDamage;
        damageOwner = owner;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.transform.position;
        Vector3 direction = targetPosition - transform.position;

        float moveDistance = speed * Time.deltaTime;

        if (direction.magnitude <= hitDistance || direction.magnitude <= moveDistance)
        {
            HitTarget();
            return;
        }

        transform.position += direction.normalized * moveDistance;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void HitTarget()
    {
        if (target != null)
        {
            target.TakeDamage(damage, damageOwner);
        }

        Destroy(gameObject);
    }
}
