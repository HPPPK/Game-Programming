using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialEnemyDemoSpawner : MonoBehaviour
{
    [Header("Spawn Setup")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform enemiesParent;
    [SerializeField] private float spawnIntervalSeconds = 0.35f;
    [SerializeField] private bool notifyTutorialManagerWhenCleared = true;

    private readonly List<GameObject> activeTutorialEnemies = new List<GameObject>();
    private Coroutine trackRoutine;

    public void SpawnTutorialWeakEnemies(int count)
    {
        ClearTutorialEnemies();

        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("TutorialEnemyDemoSpawner is missing prefab or spawn points.");
            return;
        }

        StartCoroutine(SpawnRoutine(Mathf.Max(1, count)));
    }

    public void NotifyTutorialWaveCompleted()
    {
        TutorialManager.Instance?.NotifyTutorialWaveCompleted();
    }

    public void ClearTutorialEnemies()
    {
        if (trackRoutine != null)
        {
            StopCoroutine(trackRoutine);
            trackRoutine = null;
        }

        for (int i = 0; i < activeTutorialEnemies.Count; i++)
        {
            if (activeTutorialEnemies[i] != null)
            {
                Destroy(activeTutorialEnemies[i]);
            }
        }

        activeTutorialEnemies.Clear();
    }

    private IEnumerator SpawnRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];

            if (spawnPoint != null)
            {
                GameObject spawnedEnemy = enemiesParent != null
                    ? Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation, enemiesParent)
                    : Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

                activeTutorialEnemies.Add(spawnedEnemy);
            }

            if (spawnIntervalSeconds > 0f)
            {
                yield return new WaitForSeconds(spawnIntervalSeconds);
            }
        }

        trackRoutine = StartCoroutine(TrackWaveCompletionRoutine());
    }

    private IEnumerator TrackWaveCompletionRoutine()
    {
        while (true)
        {
            activeTutorialEnemies.RemoveAll(enemy => enemy == null);

            if (activeTutorialEnemies.Count == 0)
            {
                trackRoutine = null;

                if (notifyTutorialManagerWhenCleared)
                {
                    NotifyTutorialWaveCompleted();
                }

                yield break;
            }

            yield return null;
        }
    }
}
