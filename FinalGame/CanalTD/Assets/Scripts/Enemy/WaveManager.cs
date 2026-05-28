/*
 * File: WaveManager.cs
 *
 * Purpose:
 * Controls enemy wave spawning with the new EnemyStats-driven architecture.
 * WaveManager only decides how many enemies spawn and which prefabs are chosen.
 * HP, speed, reward, armor, split behavior, and boss stats live on EnemyStats.
 */
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private const float SpawnIntervalSeconds = 0.5f;
    private const float WaveClearedDelaySeconds = 2f;

    [Header("Spawners")]
    public EnemySpawner[] spawners;

    [Header("Round Scaling")]
    public int currentRound = 1;
    public int maxWaveCount = 5;
    public int baseEnemyCount = 12;
    public int enemyCountIncreasePerRound = 4;
    public float hpScalePerRound = 0.12f;
    public float speedScalePerRound = 0.02f;

    [Header("Spawn Director")]
    [Tooltip("Weighted enemy entries. EnemyStats on each prefab supplies HP, speed, rewards, armor, and special behavior.")]
    public List<WaveEnemyEntry> enemyEntries = new List<WaveEnemyEntry>();

    [Header("Boss Settings")]
    public bool spawnBossEveryTenRounds = true;

    [Header("Phase Manager")]
    public GamePhaseManager gamePhaseManager;
    public AIPrototypeTurnManager aiPrototypeTurnManager;
    public MonoBehaviour toastMessage;

    [Header("Wave UI")]
    [Tooltip("Optional UI text shown as Round: current/max, for example Round: 1/5.")]
    public TMP_Text waveCounterText;

    private bool isSpawning = false;
    private bool isWaveRunning = false;
    private int activeEnemyCount = 0;
    private int activeWaveNumber = 1;
    private List<GameObject> activeEnemySpawnPlan = new List<GameObject>();

    public bool IsSpawning
    {
        get { return isSpawning; }
    }

    public bool IsWaveRunning
    {
        get { return isWaveRunning; }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (gamePhaseManager != null)
            {
            
                gamePhaseManager.OnEndTurnButtonClicked();
            }
            else
            {
                StartWave();
            }
        }
    }

    private void Start()
    {
        RefreshWaveCounterUI();
    }

    public void ShowWaveIncoming(int waveNumber)
    {
        activeWaveNumber = Mathf.Max(1, waveNumber);
        currentRound = activeWaveNumber;
        RefreshWaveCounterUI();
        ShowToast("Wave " + activeWaveNumber + " Incoming!");
    }

    public void StartWave()
    {
        if (isSpawning || isWaveRunning)
        {
            return;
        }

        activeWaveNumber = GetCurrentWaveIndex();
        currentRound = activeWaveNumber;
        RefreshWaveCounterUI();
        activeEnemyCount = GetEnemyCountForRound(currentRound);
        activeEnemySpawnPlan = BuildEnemySpawnPlan(currentRound, activeEnemyCount);

        isWaveRunning = true;

        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("No spawners assigned.");
            ShowToast("Wave " + activeWaveNumber + " cleared.");
            isWaveRunning = false;
            NotifyWaveFinished();
            return;
        }

        ShowToast("Wave " + activeWaveNumber + " started.");
        Debug.Log("Wave round = " + currentRound + ", enemy count = " + activeEnemyCount);
        LogEnemyTypeCounts(activeEnemySpawnPlan, currentRound);

        if (EnemyPathAssignmentManager.Instance != null)
        {
            EnemyPathAssignmentManager.Instance.ResetAssignmentsForWave(activeEnemyCount, spawners);
        }

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;

        for (int i = 0; i < activeEnemyCount; i++)
        {
            int spawnerIndex = i % spawners.Length;

            if (spawners[spawnerIndex] != null)
            {
                spawners[spawnerIndex].SpawnOne(
                    GetEnemyPrefabForSpawn(i),
                    currentRound,
                    hpScalePerRound,
                    speedScalePerRound
                );
            }

            yield return new WaitForSeconds(SpawnIntervalSeconds);
        }

        isSpawning = false;
        yield return new WaitUntil(IsWaveFinished);

        ShowToast("Wave " + activeWaveNumber + " cleared.");
        RefreshWaveCounterUI();

        if (WaveClearedDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(WaveClearedDelaySeconds);
        }

        isWaveRunning = false;
        NotifyWaveFinished();
    }

    private bool IsWaveFinished()
    {
        if (isSpawning)
        {
            return false;
        }

        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        return enemies.Length == 0;
    }

    private void NotifyWaveFinished()
    {
        if (aiPrototypeTurnManager != null)
        {
            aiPrototypeTurnManager.OnWaveFinished();
            return;
        }

        if (gamePhaseManager != null)
        {
            gamePhaseManager.OnWaveFinished();
        }
    }

    private int GetCurrentWaveIndex()
    {
        if (aiPrototypeTurnManager != null)
        {
            return Mathf.Max(1, aiPrototypeTurnManager.currentWaveIndex);
        }

        if (gamePhaseManager != null)
        {
            return Mathf.Max(1, gamePhaseManager.currentWaveIndex);
        }

        return Mathf.Max(1, currentRound);
    }

    // Keeps the optional round text synchronized in both local and AI prototype scenes.
    public void RefreshWaveCounterUI()
    {
        if (waveCounterText == null)
        {
            return;
        }

        int currentWave = Mathf.Clamp(GetCurrentWaveIndex(), 1, GetMaxWaveCount());
        waveCounterText.text = "Round: " + currentWave + "/" + GetMaxWaveCount();
    }

    private int GetMaxWaveCount()
    {
        if (aiPrototypeTurnManager != null)
        {
            return Mathf.Max(1, aiPrototypeTurnManager.maxWaves);
        }

        if (gamePhaseManager != null)
        {
            return Mathf.Max(1, gamePhaseManager.maxWaves);
        }

        return Mathf.Max(1, maxWaveCount);
    }

    private int GetEnemyCountForRound(int round)
    {
        int safeRound = Mathf.Max(1, round);
        return Mathf.Max(1, baseEnemyCount + (safeRound - 1) * Mathf.Max(0, enemyCountIncreasePerRound));
    }

    private GameObject GetEnemyPrefabForSpawn(int spawnIndex)
    {
        if (activeEnemySpawnPlan != null &&
            spawnIndex >= 0 &&
            spawnIndex < activeEnemySpawnPlan.Count)
        {
            return activeEnemySpawnPlan[spawnIndex];
        }

        return null;
    }

    private List<GameObject> BuildEnemySpawnPlan(int round, int enemyCount)
    {
        List<GameObject> spawnPlan = new List<GameObject>();
        List<WaveEnemyEntry> validEntries = GetValidEnemyEntries(round, false);
        bool bossWave = IsBossRound(round);
        WaveEnemyEntry bossEntry = bossWave ? GetBossEntry(round) : null;

        if (bossWave)
        {
            Debug.Log("Boss wave detected for round " + round + ". Boss entry found = " + (bossEntry != null));
        }

        if (bossEntry != null)
        {
            spawnPlan.Add(bossEntry.enemyPrefab);
        }

        if (validEntries.Count == 0)
        {
            Debug.LogWarning("No valid Enemy Entries for round " + round + ". EnemySpawner fallback prefab will be used.");
        }

        while (spawnPlan.Count < enemyCount)
        {
            GameObject selectedPrefab = validEntries.Count > 0
                ? GetWeightedEnemyPrefab(validEntries, round)
                : null;

            spawnPlan.Add(selectedPrefab);
        }

        ShuffleSpawnPlan(spawnPlan);
        return spawnPlan;
    }

    private List<WaveEnemyEntry> GetValidEnemyEntries(int round, bool bossOnly)
    {
        List<WaveEnemyEntry> validEntries = new List<WaveEnemyEntry>();

        if (enemyEntries == null)
        {
            return validEntries;
        }

        foreach (WaveEnemyEntry entry in enemyEntries)
        {
            if (IsValidEnemyEntry(entry, round, bossOnly))
            {
                validEntries.Add(entry);
            }
        }

        return validEntries;
    }

    private bool IsValidEnemyEntry(WaveEnemyEntry entry, int round, bool bossOnly)
    {
        if (entry == null || entry.enemyPrefab == null || entry.weight <= 0f)
        {
            return false;
        }

        int safeMinRound = Mathf.Max(1, entry.minRound);

        if (round < safeMinRound)
        {
            return false;
        }

        if (entry.maxRound > 0 && round > entry.maxRound)
        {
            return false;
        }

        bool isBossEntry = entry.isBossEntry || entry.enemyType == EnemyType.Boss;

        if (bossOnly)
        {
            return isBossEntry && IsBossRound(round);
        }

        if (isBossEntry)
        {
            return false;
        }

        // Round gates keep early waves readable even if advanced entries are already assigned.
        if (round <= 3)
        {
            return entry.enemyType == EnemyType.Normal;
        }

        if (round <= 6)
        {
            return entry.enemyType == EnemyType.Normal ||
                entry.enemyType == EnemyType.Fast;
        }

        if (round <= 9)
        {
            return entry.enemyType == EnemyType.Normal ||
                entry.enemyType == EnemyType.Fast ||
                entry.enemyType == EnemyType.Tank;
        }

        return true;
    }

    private bool IsBossRound(int round)
    {
        return spawnBossEveryTenRounds && round > 0 && round % 10 == 0;
    }

    private WaveEnemyEntry GetBossEntry(int round)
    {
        List<WaveEnemyEntry> bossEntries = GetValidEnemyEntries(round, true);

        if (bossEntries.Count == 0)
        {
            return null;
        }

        return bossEntries[Random.Range(0, bossEntries.Count)];
    }

    private GameObject GetWeightedEnemyPrefab(List<WaveEnemyEntry> validEntries, int round)
    {
        float totalWeight = 0f;

        foreach (WaveEnemyEntry entry in validEntries)
        {
            totalWeight += GetEffectiveWeight(entry, round);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        float cursor = 0f;

        foreach (WaveEnemyEntry entry in validEntries)
        {
            cursor += GetEffectiveWeight(entry, round);

            if (roll <= cursor)
            {
                return entry.enemyPrefab;
            }
        }

        return validEntries[validEntries.Count - 1].enemyPrefab;
    }

    private float GetEffectiveWeight(WaveEnemyEntry entry, int round)
    {
        if (entry == null)
        {
            return 0f;
        }

        int roundsSinceUnlock = Mathf.Max(0, round - Mathf.Max(1, entry.minRound));
        float bonus = Mathf.Max(0, entry.countBonusPerRound) * roundsSinceUnlock;
        return Mathf.Max(0f, entry.weight + bonus);
    }

    private void LogEnemyTypeCounts(List<GameObject> spawnPlan, int round)
    {
        Dictionary<EnemyType, int> typeCounts = new Dictionary<EnemyType, int>();
        int fallbackCount = 0;

        foreach (GameObject prefab in spawnPlan)
        {
            if (prefab == null)
            {
                fallbackCount++;
                continue;
            }

            EnemyType type = GetEnemyTypeFromPrefab(prefab);

            if (!typeCounts.ContainsKey(type))
            {
                typeCounts[type] = 0;
            }

            typeCounts[type]++;
        }

        foreach (KeyValuePair<EnemyType, int> pair in typeCounts)
        {
            Debug.Log("Round " + round + " spawns " + pair.Value + " " + pair.Key + " enemies.");
        }

        if (fallbackCount > 0)
        {
            Debug.Log("Round " + round + " uses EnemySpawner fallback prefab for " + fallbackCount + " enemies.");
        }
    }

    private EnemyType GetEnemyTypeFromPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return EnemyType.Normal;
        }

        EnemyStats stats = prefab.GetComponent<EnemyStats>();
        return stats != null ? stats.enemyType : EnemyType.Normal;
    }

    private void ShuffleSpawnPlan(List<GameObject> spawnPlan)
    {
        if (spawnPlan == null)
        {
            return;
        }

        for (int i = spawnPlan.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            GameObject temp = spawnPlan[i];
            spawnPlan[i] = spawnPlan[swapIndex];
            spawnPlan[swapIndex] = temp;
        }
    }

    private void ShowToast(string message)
    {
        if (TryCallToastMethod(message))
        {
            return;
        }

        CardDrawManager cardManager = FindObjectOfType<CardDrawManager>();

        if (cardManager != null)
        {
            cardManager.ShowWarningMessage(message);
            return;
        }

        Debug.Log(message);
    }

    private bool TryCallToastMethod(string message)
    {
        if (toastMessage == null && gamePhaseManager != null)
        {
            toastMessage = gamePhaseManager.toastMessage;
        }

        if (toastMessage == null)
        {
            return false;
        }

        string[] methodNames =
        {
            "ShowMessage",
            "Show",
            "ShowToast",
            "Display",
            "ShowWarningMessage"
        };

        foreach (string methodName in methodNames)
        {
            MethodInfo method = toastMessage.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new System.Type[] { typeof(string) },
                null
            );

            if (method == null)
            {
                continue;
            }

            method.Invoke(toastMessage, new object[] { message });
            return true;
        }

        return false;
    }
}
