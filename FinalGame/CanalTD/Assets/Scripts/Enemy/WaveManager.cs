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
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Spawners")]
    public EnemySpawner[] spawners;

    [Header("Phase Manager")]
    public GamePhaseManager gamePhaseManager;
    public MonoBehaviour toastMessage;

    [Header("Wave Settings")]
    public int currentWaveIndex = 1;
    public int currentWaveNumber = 1;
    public int enemyCount = 8;
    public int enemiesPerWave = 8;
    public int waveEnemyHp = 3;
    public float waveHpMultiplier = 1f;
    public float spawnInterval = 0.5f;
    public bool isWaveRunning = false;
    public int enemyCountIncreasePerWave = 4;
    public int enemyHpIncreasePerWave = 1;
    public float waveClearedDelay = 2f;
    public List<WaveConfig> waveConfigs = new List<WaveConfig>
    {
        new WaveConfig(1, 12, 3, 0.8f, 1, 2, 1, 1),
        new WaveConfig(2, 16, 4, 0.75f, 1, 2, 1, 1),
        new WaveConfig(3, 20, 5, 0.7f, 1, 2, 1, 1),
        new WaveConfig(4, 24, 6, 0.65f, 1, 2, 1, 1),
        new WaveConfig(5, 30, 8, 0.6f, 1, 2, 1, 1)
    };

    private bool isSpawning = false;
    private WaveConfig activeWaveConfig;

    public bool IsSpawning
    {
        get { return isSpawning; }
    }

    private void Awake()
    {
        EnsureDefaultWaveConfigs();
    }

    private void Reset()
    {
        EnsureDefaultWaveConfigs();
    }

    void Update()
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

    private bool IsWaveFinished()
    {
        if (isSpawning)
        {
            return false;
        }

        EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
        return enemies.Length == 0;
    }

    public void StartWave()
    {
        if (isSpawning || isWaveRunning)
        {
            return;
        }

        isWaveRunning = true;

        if (spawners == null || spawners.Length == 0)
        {
            Debug.LogWarning("No spawners assigned.");
            ShowToast("Wave " + GetCurrentWaveIndex() + " cleared.");
            isWaveRunning = false;
            NotifyWaveFinished();
            return;
        }

        int waveIndex = GetCurrentWaveIndex();
        activeWaveConfig = GetWaveConfig(waveIndex);
        ApplyActiveWaveTracking(activeWaveConfig);
        spawnInterval = activeWaveConfig.spawnInterval;

        ShowToast("Wave " + currentWaveNumber + " started.");

        if (EnemyPathAssignmentManager.Instance != null)
        {
            EnemyPathAssignmentManager.Instance.ResetAssignmentsForWave(activeWaveConfig.enemyCount, spawners);
        }

        StartCoroutine(SpawnWaveRoutine());
    }

    public void ShowWaveIncoming(int waveNumber)
    {
        currentWaveNumber = Mathf.Max(1, waveNumber);
        currentWaveIndex = currentWaveNumber;
        ShowToast("Wave " + currentWaveNumber + " Incoming!");
    }

    IEnumerator SpawnWaveRoutine()
    {
        isSpawning = true;

        for (int i = 0; i < activeWaveConfig.enemyCount; i++)
        {
            int spawnerIndex = i % spawners.Length;

            if (spawners[spawnerIndex] != null)
            {
                spawners[spawnerIndex].SpawnOne(activeWaveConfig);
            }

            yield return new WaitForSeconds(activeWaveConfig.spawnInterval);
        }

        isSpawning = false;
        // Wait for all enemies to be destroyed
        yield return new WaitUntil(IsWaveFinished);

        ShowToast("Wave " + currentWaveNumber + " cleared.");

        if (waveClearedDelay > 0f)
        {
            yield return new WaitForSeconds(waveClearedDelay);
        }

        isWaveRunning = false;
        NotifyWaveFinished();
    }

    private void NotifyWaveFinished()
    {
        if (gamePhaseManager != null)
        {
            gamePhaseManager.OnWaveFinished();
        }
    }

    private int GetCurrentWaveIndex()
    {
        if (gamePhaseManager != null)
        {
            return Mathf.Max(1, gamePhaseManager.currentWaveIndex);
        }

        return Mathf.Max(1, currentWaveIndex);
    }

    private WaveConfig GetWaveConfig(int waveIndex)
    {
        if (waveConfigs == null)
        {
            waveConfigs = new List<WaveConfig>();
        }

        EnsureDefaultWaveConfigs();

        foreach (WaveConfig config in waveConfigs)
        {
            if (config != null && config.waveIndex == waveIndex)
            {
                return config;
            }
        }

        if (waveConfigs.Count == 0)
        {
            return GenerateFallbackConfig(waveIndex, null);
        }

        WaveConfig lastConfig = GetLastValidConfig();
        return GenerateFallbackConfig(waveIndex, lastConfig);
    }

    private WaveConfig GetLastValidConfig()
    {
        WaveConfig lastConfig = null;

        foreach (WaveConfig config in waveConfigs)
        {
            if (config == null)
            {
                continue;
            }

            if (lastConfig == null || config.waveIndex > lastConfig.waveIndex)
            {
                lastConfig = config;
            }
        }

        return lastConfig;
    }

    private WaveConfig GenerateFallbackConfig(int waveIndex, WaveConfig baseConfig)
    {
        if (baseConfig == null)
        {
            int fallbackEnemyCount = enemyCount + Mathf.Max(0, waveIndex - 1) * enemyCountIncreasePerWave;
            int fallbackEnemyHp = 3 + Mathf.Max(0, waveIndex - 1) * enemyHpIncreasePerWave;
            return new WaveConfig(waveIndex, fallbackEnemyCount, fallbackEnemyHp, spawnInterval, 1, 2, 1, 1);
        }

        int extraWaves = Mathf.Max(0, waveIndex - baseConfig.waveIndex);
        int scaledEnemyCount = baseConfig.enemyCount + extraWaves * enemyCountIncreasePerWave;
        int scaledEnemyMaxHP = baseConfig.enemyMaxHP + extraWaves * enemyHpIncreasePerWave;
        float scaledSpawnInterval = Mathf.Max(0.35f, baseConfig.spawnInterval - extraWaves * 0.03f);

        return new WaveConfig(
            waveIndex,
            scaledEnemyCount,
            scaledEnemyMaxHP,
            scaledSpawnInterval,
            baseConfig.goldReward,
            baseConfig.killerScoreReward,
            baseConfig.assistScoreReward,
            baseConfig.castleDamage
        );
    }

    private void EnsureDefaultWaveConfigs()
    {
        if (waveConfigs == null)
        {
            waveConfigs = new List<WaveConfig>();
        }

        if (waveConfigs.Count > 0)
        {
            return;
        }

        waveConfigs.Add(new WaveConfig(1, 12, 3, 0.8f, 1, 2, 1, 1));
        waveConfigs.Add(new WaveConfig(2, 16, 4, 0.75f, 1, 2, 1, 1));
        waveConfigs.Add(new WaveConfig(3, 20, 5, 0.7f, 1, 2, 1, 1));
        waveConfigs.Add(new WaveConfig(4, 24, 6, 0.65f, 1, 2, 1, 1));
        waveConfigs.Add(new WaveConfig(5, 30, 8, 0.6f, 1, 2, 1, 1));
    }

    private void ApplyActiveWaveTracking(WaveConfig config)
    {
        if (config == null)
        {
            return;
        }

        currentWaveIndex = config.waveIndex;
        currentWaveNumber = config.waveIndex;
        enemyCount = config.enemyCount;
        enemiesPerWave = config.enemyCount;
        waveEnemyHp = config.enemyMaxHP;
        waveHpMultiplier = Mathf.Max(1f, waveEnemyHp / 3f);
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
