/*
 * File: WaveEnemyEntry.cs
 *
 * Purpose:
 * Implements WaveEnemyEntry for the enemy layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for WaveEnemyEntry within the enemy system.
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
 * - Verify WaveEnemyEntry in the scene or prefab where it is used and confirm the main happy path still works.
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
