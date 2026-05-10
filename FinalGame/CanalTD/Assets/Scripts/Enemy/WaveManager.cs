/*
 * File: WaveManager.cs
 *
 * Purpose:
 * This script starts and controls enemy waves. A wave is a timed sequence of
 * enemy spawns distributed across the assigned EnemySpawner objects.
 *
 * Runtime behavior:
 * - Pressing Space calls StartWave() for quick testing.
 * - StartWave() refuses to start a second wave while one is already running.
 * - At the beginning of a wave, EnemyPathAssignmentManager resets its assignment
 *   counts so this wave can rebalance castle targets from a clean state.
 * - SpawnWaveRoutine() spawns enemyCount enemies with spawnInterval seconds
 *   between each spawn.
 * - Spawners are used in round-robin order.
 *
 * Inspector setup:
 * - spawners should include every spawn point that can produce enemies.
 * - enemyCount controls how many enemies this wave creates.
 * - spawnInterval controls the delay between spawns.
 *
 * Dependency notes:
 * - EnemySpawner creates actual enemy GameObjects.
 * - EnemyPathAssignmentManager is reset before each wave so target balancing
 *   starts fresh.
 */
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Spawners")]
    public EnemySpawner[] spawners;

    [Header("Wave Settings")]
    public int enemyCount = 8;
    public float spawnInterval = 0.5f;

    private bool isSpawning = false;

    public bool IsSpawning
    {
        get { return isSpawning; }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartWave();
        }
    }

    public void StartWave()
    {
        if (isSpawning) return;
        

        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("No spawners assigned.");
            return;
        }

        if (EnemyPathAssignmentManager.Instance != null)
        {
            EnemyPathAssignmentManager.Instance.ResetAssignmentsForWave(enemyCount, spawners);
        }

        StartCoroutine(SpawnWaveRoutine());
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;

        for (int i = 0; i < enemyCount; i++)
        {
            int spawnerIndex = i % spawners.Length;

            if (spawners[spawnerIndex] != null)
            {
                spawners[spawnerIndex].SpawnOne();
            }

            yield return new WaitForSeconds(spawnInterval);
        }

        isSpawning = false;
    }
}
