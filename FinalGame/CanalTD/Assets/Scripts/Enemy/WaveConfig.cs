/*
 * File: WaveConfig.cs
 *
 * Purpose:
 * Serializable data container for one wave's difficulty and reward values. It
 * lets WaveManager configure enemy count, HP, spawn interval, rewards, castle
 * damage, and which enemy prefabs can appear in this wave.
 */
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveEnemyPrefabEntry
{
    [Tooltip("Enemy prefab to spawn for this wave. It should have EnemyMover and EnemyHealth.")]
    public GameObject enemyPrefab;

    [Tooltip("Spawn ratio for the whole wave. Example: 3 and 1 means about 75% / 25%.")]
    public int weight = 1;
}

[System.Serializable]
public class WaveConfig
{
    public int waveIndex = 1;
    public int enemyCount = 12;
    public int enemyMaxHP = 3;
    public float spawnInterval = 0.8f;
    public int goldReward = 1;
    public int killerScoreReward = 2;
    public int assistScoreReward = 1;
    public int castleDamage = 1;
    public List<WaveEnemyPrefabEntry> enemyPrefabs = new List<WaveEnemyPrefabEntry>();

    public WaveConfig()
    {
    }

    public WaveConfig(
        int waveIndex,
        int enemyCount,
        int enemyMaxHP,
        float spawnInterval,
        int goldReward,
        int killerScoreReward,
        int assistScoreReward,
        int castleDamage
    )
    {
        this.waveIndex = waveIndex;
        this.enemyCount = enemyCount;
        this.enemyMaxHP = enemyMaxHP;
        this.spawnInterval = spawnInterval;
        this.goldReward = goldReward;
        this.killerScoreReward = killerScoreReward;
        this.assistScoreReward = assistScoreReward;
        this.castleDamage = castleDamage;
    }
}
