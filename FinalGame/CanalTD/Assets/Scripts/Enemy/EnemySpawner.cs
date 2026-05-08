/*
 * File: EnemySpawner.cs
 *
 * Purpose:
 * This script creates enemy instances at a specific starting PathNode. It is a
 * simple bridge between WaveManager and EnemyMover: WaveManager decides when to
 * spawn, while EnemySpawner creates the object and initializes its movement.
 *
 * Runtime behavior:
 * - SpawnOne() validates that an enemy prefab and start node are assigned.
 * - It instantiates the prefab at the start node's world position.
 * - It looks for EnemyMover on the spawned prefab.
 * - It calls EnemyMover.Init(startNode) so the enemy can begin moving.
 *
 * Inspector setup:
 * - enemyPrefab should be a prefab with an EnemyMover component.
 * - startNode should point to the PathNode where enemies enter the map.
 *
 * Dependency notes:
 * - WaveManager calls SpawnOne() repeatedly during a wave.
 * - EnemyMover handles path selection and movement after spawning.
 */
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Start Node")]
    public PathNode startNode;

    public void SpawnOne()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(name + " spawn failed: enemyPrefab is missing.");
            return;
        }

        if (startNode == null)
        {
            Debug.LogError(name + " spawn failed: startNode is missing.");
            return;
        }

        GameObject enemy = Instantiate(
            enemyPrefab,
            startNode.transform.position,
            Quaternion.identity
        );

        EnemyMover mover = enemy.GetComponent<EnemyMover>();

        if (mover == null)
        {
            Debug.LogError("Spawned enemy has no EnemyMover component.");
            Destroy(enemy);
            return;
        }

        mover.Init(startNode);
    }
}
