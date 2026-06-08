/*
 * File: GameResultData.cs
 *
 * Purpose:
 * Implements GameResultData for the core layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for GameResultData within the core system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify GameResultData in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections.Generic;

[System.Serializable]
public class PlayerResultEntry
{
    public int playerId;
    public string displayName;
    public int score;
    public int money;
    public int castleHp;
    public int rank;
    public bool isEliminated;

    public PlayerResultEntry(int playerId, string displayName, int score, int money, int castleHp, bool isEliminated = false)
    {
        this.playerId = playerId;
        this.displayName = displayName;
        this.score = score;
        this.money = money;
        this.castleHp = castleHp;
        this.isEliminated = isEliminated;
        rank = 0;
    }
}

public static class GameResultData
{
    public static List<PlayerResultEntry> Results = new List<PlayerResultEntry>();

    public static void SetResults(List<PlayerResultEntry> results)
    {
        Results = results != null ? new List<PlayerResultEntry>(results) : new List<PlayerResultEntry>();
    }

    public static void Clear()
    {
        Results.Clear();
    }
}
