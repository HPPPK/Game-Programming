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
 * - enemyPrefab is the fallback prefab used when WaveManager does not provide
 *   a weighted enemy entry prefab.
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

    public GameObject SpawnOne()
    {
        return SpawnOne(null, 1, 0f, 0f);
    }

    public GameObject SpawnOne(GameObject waveEnemyPrefab, int round, float hpScalePerRound, float speedScalePerRound)
    {
        GameObject prefabToSpawn = waveEnemyPrefab != null ? waveEnemyPrefab : enemyPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError(name + " spawn failed: enemyPrefab is missing.");
            return null;
        }

        if (startNode == null)
        {
            Debug.LogError(name + " spawn failed: startNode is missing.");
            return null;
        }

        GameObject enemy = Instantiate(
            prefabToSpawn,
            startNode.transform.position,
            Quaternion.identity
        );

        EnemyMover mover = enemy.GetComponent<EnemyMover>();

        if (mover == null)
        {
            Debug.LogError("Spawned enemy has no EnemyMover component.");
            Destroy(enemy);
            return null;
        }

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.InitializeFromStats(round, hpScalePerRound, speedScalePerRound);
        }

        mover.ApplyStatsSpeed(round, speedScalePerRound);
        mover.Init(startNode);
        return enemy;
    }
}
