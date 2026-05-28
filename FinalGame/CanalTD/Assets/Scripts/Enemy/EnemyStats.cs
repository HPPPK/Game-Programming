/*
 * File: EnemyStats.cs
 *
 * Purpose:
 * Optional prefab component for enemy type data. If an enemy prefab has this
 * component, EnemyHealth and EnemyMover read final HP, speed, reward, armor,
 * and special behavior from it. Runtime values are calculated by methods so the
 * Inspector only shows editable base configuration.
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
