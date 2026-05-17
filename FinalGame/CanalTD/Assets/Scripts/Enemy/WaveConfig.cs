using UnityEngine;

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
