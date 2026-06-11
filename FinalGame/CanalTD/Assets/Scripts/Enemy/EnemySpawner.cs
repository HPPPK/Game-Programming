/*
 * File: EnemySpawner.cs
 *
 * Purpose:
 * Implements EnemySpawner for the enemy layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Enemy prefabs, wave helpers, or enemy-related scene managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemySpawner within the enemy system.
 * - Update the owning object state and react to gameplay events during play.
 * - Support enemy spawning, pathing, targeting, combat, or wave pressure behaviour.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Path graph data, wave settings, target selection, or movement parameters.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Moves enemies, adjusts combat results, or influences castle pressure and wave outcomes.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify EnemySpawner in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Start Node")]
    public PathNode startNode;

    /// <summary>
    /// Spawns one into the scene and initializes its runtime state.
    /// </summary>
    public GameObject SpawnOne()
    {
        /// <summary>
        /// Handles spawn one for enemy spawner.
        /// </summary>
        return SpawnOne(null, 1, 0f, 0f);
    }

    /// <summary>
    /// Spawns one into the scene and initializes its runtime state.
    /// </summary>
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
