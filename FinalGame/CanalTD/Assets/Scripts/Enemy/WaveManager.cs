/*
 * File: WaveManager.cs
 *
 * Purpose:
 * Implements WaveManager for the enemy layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Enemy prefabs, wave helpers, or enemy-related scene managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for WaveManager within the enemy system.
 * - Coordinate related objects, state changes, and cross-system communication.
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
 * - Verify WaveManager in the scene or prefab where it is used and confirm the main happy path still works.
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
    public int maxWaveCount = 10;
    public int baseEnemyCount = 16;
    public int enemyCountIncreasePerRound = 8;
    public float hpScalePerRound = 0.12f;
    public float speedScalePerRound = 0.02f;

    [Header("Spawn Director")]
    [Tooltip("Weighted enemy entries. EnemyStats on each prefab supplies HP, speed, rewards, armor, and special behavior.")]
    public List<WaveEnemyEntry> enemyEntries = new List<WaveEnemyEntry>();
    public bool logWaveSpawnPlan = true;

    [Header("Boss Settings")]
    public bool spawnBossEveryTenRounds = true;

    [Header("Phase Manager")]
    public GamePhaseManager gamePhaseManager;
    public AIPrototypeTurnManager aiPrototypeTurnManager;
    public MonoBehaviour toastMessage;

    [Header("Wave UI")]
    [Tooltip("Optional UI text shown as Round: current/max, for example Round: 1/10.")]
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

    /// <summary>
    /// Checks wave manager input, timing, animation, or UI state once per frame.
    /// </summary>
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

    /// <summary>
    /// Sets up wave manager when this scene object starts running.
    /// </summary>
    private void Start()
    {
        RefreshWaveCounterUI();
    }

    /// <summary>
    /// Shows wave incoming with the correct current context.
    /// </summary>
    public void ShowWaveIncoming(int waveNumber)
    {
        activeWaveNumber = Mathf.Max(1, waveNumber);
        currentRound = activeWaveNumber;
        RefreshWaveCounterUI();
        ShowToast("Wave " + activeWaveNumber + " Incoming!");
    }

    /// <summary>
    /// Starts wave and enables its related gameplay flow.
    /// </summary>
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
        LogWaveSpawnPlan(activeEnemySpawnPlan, currentRound);

        if (EnemyPathAssignmentManager.Instance != null)
        {
            EnemyPathAssignmentManager.Instance.ResetAssignmentsForWave(activeEnemyCount, spawners);
        }

        StartCoroutine(SpawnWaveRoutine());
    }

    /// <summary>
    /// Returns online enemy count for round used by enemy spawning, movement, waves, or routing.
    /// </summary>
    public int GetOnlineEnemyCountForRound(int round)
    {
        /// <summary>
        /// Returns enemy count for round needed by this gameplay system.
        /// </summary>
        return GetEnemyCountForRound(round);
    }

    /// <summary>
    /// Builds online enemy spawn plan from configured scene objects and runtime state.
    /// </summary>
    public List<GameObject> BuildOnlineEnemySpawnPlan(int round, int enemyCount)
    {
        /// <summary>
        /// Handles build enemy spawn plan for wave manager.
        /// </summary>
        return BuildEnemySpawnPlan(round, enemyCount);
    }

    /// <summary>
    /// Looks up the target for online enemy prefab and applies the resolved gameplay result.
    /// </summary>
    public GameObject ResolveOnlineEnemyPrefab(string enemyTypeId)
    {
        if (string.IsNullOrWhiteSpace(enemyTypeId))
        {
            return null;
        }

        if (enemyEntries != null)
        {
            foreach (WaveEnemyEntry entry in enemyEntries)
            {
                if (entry != null &&
                    entry.enemyPrefab != null &&
                    entry.enemyPrefab.name == enemyTypeId)
                {
                    return entry.enemyPrefab;
                }
            }
        }

        if (spawners != null)
        {
            foreach (EnemySpawner spawner in spawners)
            {
                if (spawner != null &&
                    spawner.enemyPrefab != null &&
                    spawner.enemyPrefab.name == enemyTypeId)
                {
                    return spawner.enemyPrefab;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Applies online wave started to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyOnlineWaveStarted(int waveIndex)
    {
        activeWaveNumber = Mathf.Max(1, waveIndex);
        currentRound = activeWaveNumber;
        isWaveRunning = true;
        isSpawning = true;
        RefreshWaveCounterUI();
        ShowToast("Enemy Wave Started");
    }

    /// <summary>
    /// Applies online wave ended to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyOnlineWaveEnded(int nextRound)
    {
        isSpawning = false;
        isWaveRunning = false;
        currentRound = Mathf.Max(1, nextRound);
        RefreshWaveCounterUI();
    }

    /// <summary>
    /// Spawns wave routine into the scene and initializes its runtime state.
    /// </summary>
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

            /// <summary>
            /// Handles wait for seconds for wave manager.
            /// </summary>
            yield return new WaitForSeconds(SpawnIntervalSeconds);
        }

        isSpawning = false;
        /// <summary>
        /// Handles wait until for wave manager.
        /// </summary>
        yield return new WaitUntil(IsWaveFinished);

        ShowToast("Wave " + activeWaveNumber + " cleared.");
        RefreshWaveCounterUI();

        if (WaveClearedDelaySeconds > 0f)
        {
            /// <summary>
            /// Handles wait for seconds for wave manager.
            /// </summary>
            yield return new WaitForSeconds(WaveClearedDelaySeconds);
        }

        isWaveRunning = false;
        NotifyWaveFinished();
    }

    /// <summary>
    /// Checks the current state to decide whether wave finished is true.
    /// </summary>
    private bool IsWaveFinished()
    {
        if (isSpawning)
        {
            return false;
        }

        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        return enemies.Length == 0;
    }

    /// <summary>
    /// Notifies connected systems that wave finished occurred.
    /// </summary>
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

    /// <summary>
    /// Returns current wave index used by enemy spawning, movement, waves, or routing.
    /// </summary>
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
    /// <summary>
    /// Refreshes wave counter UI from the latest gameplay data.
    /// </summary>
    public void RefreshWaveCounterUI()
    {
        if (waveCounterText == null)
        {
            return;
        }

        int currentWave = Mathf.Clamp(GetCurrentWaveIndex(), 1, GetMaxWaveCount());
        waveCounterText.text = "Round: " + currentWave + "/" + GetMaxWaveCount();
    }

    /// <summary>
    /// Returns max wave count used by enemy spawning, movement, waves, or routing.
    /// </summary>
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

    /// <summary>
    /// Returns enemy count for round used by enemy spawning, movement, waves, or routing.
    /// </summary>
    private int GetEnemyCountForRound(int round)
    {
        int safeRound = Mathf.Max(1, round);
        return Mathf.Max(1, baseEnemyCount + (safeRound - 1) * Mathf.Max(0, enemyCountIncreasePerRound));
    }

    /// <summary>
    /// Returns enemy prefab for spawn used by enemy spawning, movement, waves, or routing.
    /// </summary>
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

    /// <summary>
    /// Builds enemy spawn plan from configured scene objects and runtime state.
    /// </summary>
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

    /// <summary>
    /// Returns valid enemy entries used by enemy spawning, movement, waves, or routing.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether valid enemy entry is true.
    /// </summary>
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

    /// <summary>
    /// Checks the current state to decide whether boss round is true.
    /// </summary>
    private bool IsBossRound(int round)
    {
        return spawnBossEveryTenRounds && round > 0 && round % 10 == 0;
    }

    /// <summary>
    /// Returns boss entry used by enemy spawning, movement, waves, or routing.
    /// </summary>
    private WaveEnemyEntry GetBossEntry(int round)
    {
        List<WaveEnemyEntry> bossEntries = GetValidEnemyEntries(round, true);

        if (bossEntries.Count == 0)
        {
            return null;
        }

        return bossEntries[Random.Range(0, bossEntries.Count)];
    }

    /// <summary>
    /// Returns weighted enemy prefab used by enemy spawning, movement, waves, or routing.
    /// </summary>
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

    /// <summary>
    /// Returns effective weight used by enemy spawning, movement, waves, or routing.
    /// </summary>
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

    /// <summary>
    /// Writes enemy type counts details to the Unity Console for debugging and validation.
    /// </summary>
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

    /// <summary>
    /// Writes wave spawn plan details to the Unity Console for debugging and validation.
    /// </summary>
    private void LogWaveSpawnPlan(List<GameObject> spawnPlan, int round)
    {
        if (!logWaveSpawnPlan || spawnPlan == null)
        {
            return;
        }

        int spawnerCount = spawners != null ? spawners.Length : 0;
        Debug.Log("WavePlan round=" + round + " enemyCount=" + spawnPlan.Count + " spawnerCount=" + spawnerCount);

        for (int i = 0; i < spawnPlan.Count; i++)
        {
            GameObject prefab = spawnPlan[i];
            int spawnerIndex = spawnerCount > 0 ? i % spawnerCount : -1;
            string prefabName = prefab != null ? prefab.name : "EnemySpawnerFallback";
            EnemyType enemyType = GetEnemyTypeFromPrefab(prefab);

            Debug.Log(
                "WavePlan round=" + round +
                " index=" + i +
                " spawnerIndex=" + spawnerIndex +
                " prefab=" + prefabName +
                " type=" + enemyType
            );
        }
    }

    /// <summary>
    /// Returns enemy type from prefab used by enemy spawning, movement, waves, or routing.
    /// </summary>
    private EnemyType GetEnemyTypeFromPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return EnemyType.Normal;
        }

        EnemyStats stats = prefab.GetComponent<EnemyStats>();
        return stats != null ? stats.enemyType : EnemyType.Normal;
    }

    /// <summary>
    /// Shuffles spawn plan so future selections happen in random order.
    /// </summary>
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

    /// <summary>
    /// Shows toast with the correct current context.
    /// </summary>
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

    /// <summary>
    /// Attempts to call toast method and returns false if rules, resources, or references block it.
    /// </summary>
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
