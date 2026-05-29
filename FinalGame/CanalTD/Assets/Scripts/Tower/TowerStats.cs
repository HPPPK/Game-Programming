/*
 * File: TowerStats.cs
 *
 * Purpose:
 * Stores tower identity, cost, level, targeting, and special attack values.
 * This component is optional but enables multiple tower types on the existing
 * CannonTower attack script without breaking old prefabs.
 */
using UnityEngine;

public class TowerStats : MonoBehaviour
{
    [Header("Identity")]
    public TowerType towerType = TowerType.Cannon;
    public int ownerPlayerId = -1;
    public int level = 1;
    public int maxLevel = 3;

    [Header("Cost")]
    public int baseCost = 6;
    public int upgradeCostLevel2 = 8;
    public int upgradeCostLevel3 = 12;
    public int totalGoldInvested = 0;

    [Header("Combat")]
    public float damage = 1f;
    public float range = 3f;
    public float attackInterval = 0.6f;
    public float splashRadius = 0f;
    public float slowPercent = 0.35f;
    public float slowDuration = 1.5f;
    public int chainCount = 0;
    public float chainRange = 1.5f;
    public TargetPriority targetPriority = TargetPriority.Closest;

    public bool CanUpgrade()
    {
        return level < maxLevel;
    }

    public int GetUpgradeCost()
    {
        if (!CanUpgrade())
        {
            return 0;
        }

        if (level == 1)
        {
            return upgradeCostLevel2;
        }

        if (level == 2)
        {
            return upgradeCostLevel3;
        }

        return upgradeCostLevel3;
    }

    public bool Upgrade()
    {
        if (!CanUpgrade())
        {
            return false;
        }

        int upgradeCost = GetUpgradeCost();
        level += 1;
        totalGoldInvested += upgradeCost;

        damage *= 1.5f;
        range += 0.25f;
        attackInterval = Mathf.Max(0.12f, attackInterval * 0.92f);
        splashRadius *= 1.15f;
        slowPercent = Mathf.Clamp01(slowPercent + 0.05f);
        slowDuration += 0.25f;
        chainCount += towerType == TowerType.Shock ? 1 : 0;
        chainRange += towerType == TowerType.Shock ? 0.15f : 0f;
        return true;
    }

    public int GetSellValue()
    {
        return Mathf.FloorToInt(Mathf.Max(0, totalGoldInvested) * 0.8f);
    }
}
