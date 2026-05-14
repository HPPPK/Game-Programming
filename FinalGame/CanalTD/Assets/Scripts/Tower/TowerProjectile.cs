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
