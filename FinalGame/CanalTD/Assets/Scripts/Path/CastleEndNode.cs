/*
 * File: CastleEndNode.cs
 *
 * Purpose:
 * Implements CastleEndNode for the path layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Path nodes, edges, gates, or path-debug objects placed in the gameplay scene.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for CastleEndNode within the path system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Path graph data, wave settings, target selection, or movement parameters.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify CastleEndNode in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;

public class CastleEndNode : PathNode
{
    [Header("Linked Castle")]
    public CastleBase targetCastle;

    [Header("Damage")]
    public int damagePerEnemy = 1;

    /// <summary>
    /// Responds to on enemy arrive and updates the affected gameplay or UI systems.
    /// </summary>
    public void OnEnemyArrive(GameObject enemy)
    {
        if (PhotonOnlineWaveCombatSyncManager.ShouldBlockLocalCastleDamage())
        {
            return;
        }

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

        PhotonOnlineWaveCombatSyncManager.NotifyEnemyResolvedAtCastleByMaster(enemy, targetCastle);
        Destroy(enemy);
        
    }
}
