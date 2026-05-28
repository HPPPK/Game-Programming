/*
 * File: WaveEnemyEntry.cs
 *
 * Purpose:
 * Inspector data for the new wave spawn director. Each entry says when an enemy
 * prefab can appear and how likely it is relative to other valid entries.
 */
using UnityEngine;

[System.Serializable]
public class WaveEnemyEntry
{
    [Tooltip("Enemy prefab to spawn. The prefab should have EnemyMover, EnemyHealth, and preferably EnemyStats.")]
    public GameObject enemyPrefab;

    [Tooltip("Type label used for round filtering and debug logs.")]
    public EnemyType enemyType = EnemyType.Normal;

    [Tooltip("First round where this entry can appear.")]
    public int minRound = 1;

    [Tooltip("Last round where this entry can appear. Use 0 for no upper limit.")]
    public int maxRound = 0;

    [Tooltip("Weighted selection value. 0 disables this entry.")]
    public float weight = 1f;

    [Tooltip("Extra effective weight added for each round after minRound.")]
    public int countBonusPerRound = 0;

    [Tooltip("Marks this entry as the boss candidate for boss rounds.")]
    public bool isBossEntry = false;
}
