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
