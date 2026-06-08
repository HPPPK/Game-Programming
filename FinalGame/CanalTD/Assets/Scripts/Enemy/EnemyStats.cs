/*
 * File: EnemyStats.cs
 *
 * Purpose:
 * Implements EnemyStats for the enemy layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Enemy prefabs, wave helpers, or enemy-related scene managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemyStats within the enemy system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify EnemyStats in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Type")]
    public EnemyType enemyType = EnemyType.Normal;

    [Header("Base Stats")]
    public int baseHP = 3;
    public float baseSpeed = 2f;
    public int rewardGold = 1;
    public int scoreReward = 2;
    public float armor = 0f;
    public int priority = 0;

    [Header("Special Behavior")]
    public bool canSplit = false;
    public GameObject splitEnemyPrefab;
    public int splitCount = 2;
    public bool isBoss = false;

    public int GetFinalHP(int round, float hpScalePerRound)
    {
        int safeRound = Mathf.Max(1, round);
        float hpMultiplier = 1f + Mathf.Max(0, safeRound - 1) * Mathf.Max(0f, hpScalePerRound);
        float bossHpMultiplier = isBoss || enemyType == EnemyType.Boss ? 2.5f : 1f;
        return Mathf.Max(1, Mathf.CeilToInt(baseHP * hpMultiplier * bossHpMultiplier));
    }

    public float GetFinalSpeed(int round, float speedScalePerRound)
    {
        int safeRound = Mathf.Max(1, round);
        float speedMultiplier = 1f + Mathf.Max(0, safeRound - 1) * Mathf.Max(0f, speedScalePerRound);
        float bossSlowdown = isBoss || enemyType == EnemyType.Boss ? 0.75f : 1f;
        return Mathf.Max(0.01f, baseSpeed * speedMultiplier * bossSlowdown);
    }

    public int GetFinalReward(int round)
    {
        int safeRound = Mathf.Max(1, round);
        int roundBonus = Mathf.Max(0, (safeRound - 1) / 5);
        int bossBonus = isBoss || enemyType == EnemyType.Boss ? rewardGold : 0;
        return Mathf.Max(0, rewardGold + roundBonus + bossBonus);
    }

    public int GetFinalScoreReward(int round)
    {
        int bossBonus = isBoss || enemyType == EnemyType.Boss ? scoreReward : 0;
        return Mathf.Max(0, scoreReward + bossBonus);
    }

    public int GetFinalDamage(int rawDamage)
    {
        int safeDamage = Mathf.Max(1, rawDamage);
        int reducedDamage = Mathf.CeilToInt(safeDamage - Mathf.Max(0f, armor));
        return Mathf.Max(1, reducedDamage);
    }
}
