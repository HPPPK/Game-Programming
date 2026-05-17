/*
 * File: CastleEndNode.cs
 *
 * Purpose:
 * This script marks a PathNode as a castle endpoint. When an enemy reaches this
 * node, the endpoint applies damage to its linked CastleBase and removes the
 * enemy from the scene.
 *
 * Runtime behavior:
 * - EnemyMover detects that its target node is a CastleEndNode.
 * - EnemyMover calls OnEnemyArrive(enemy).
 * - OnEnemyArrive() calls targetCastle.TakeDamage(damagePerEnemy).
 * - The arriving enemy GameObject is destroyed after the damage step.
 *
 * Inspector setup:
 * - targetCastle should point to the castle/base that this endpoint belongs to.
 * - damagePerEnemy controls how much HP one enemy removes.
 *
 * Dependency notes:
 * - Inherits from PathNode, so it can still be used in the graph.
 * - CastleBase owns HP and game-over behavior.
 */
using UnityEngine;

public class CastleEndNode : PathNode
{
    [Header("Linked Castle")]
    public CastleBase targetCastle;

    [Header("Damage")]
    public int damagePerEnemy = 1;

    public void OnEnemyArrive(GameObject enemy)
    {
        if (targetCastle != null)
        {
            int damage = damagePerEnemy;
            EnemyMover enemyMover = enemy != null ? enemy.GetComponent<EnemyMover>() : null;

            if (enemyMover != null)
            {
                damage = enemyMover.castleDamage;
            }

            targetCastle.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning(name + " has no target castle assigned.");
        }

        Destroy(enemy);
        
    }
}
