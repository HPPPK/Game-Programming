/*
 * File: OnlineEnemyIdentity.cs
 *
 * Purpose:
 * Stores the stable online enemy id assigned by the Photon wave/combat sync manager.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This helper was added with AI assistance for the online synchronization pass.
 */
using UnityEngine;

public class OnlineEnemyIdentity : MonoBehaviour
{
    public string enemyId;
    public string enemyTypeId;
    public int spawnOrder = -1;
    public int round = 1;
}
