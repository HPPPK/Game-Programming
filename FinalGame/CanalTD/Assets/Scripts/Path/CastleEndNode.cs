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
            targetCastle.TakeDamage(damagePerEnemy);
        }
        else
        {
            Debug.LogWarning(name + " has no target castle assigned.");
        }

        Destroy(enemy);
        
    }
}