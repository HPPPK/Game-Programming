using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 3;
    public int currentHP;

    [Header("Reward")]
    public int goldReward = 1;
    public int scoreReward = 1;

    [Header("Death Visual")]
    public SpriteRenderer spriteRenderer;
    public Sprite deathSprite;
    public Animator animator;
    public float destroyDelay = 0.5f;

    private bool isDead = false;
    private PlayerResource lastDamageOwner;

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

    public void TakeDamage(int damage, PlayerResource damageOwner)
    {
        if (isDead)
        {
            return;
        }

        currentHP -= damage;
        lastDamageOwner = damageOwner;

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
            lastDamageOwner.AddKillReward(goldReward, scoreReward);
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

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}