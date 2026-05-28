/*
 * File: EnemyType.cs
 *
 * Purpose:
 * Shared enemy category list used by EnemyStats and WaveManager. The enum keeps
 * type identity data-driven so new prefabs can be configured in the Inspector.
 */
public enum EnemyType
{
    Normal,
    Tank,
    Fast,
    Splitter,
    Boss
}
