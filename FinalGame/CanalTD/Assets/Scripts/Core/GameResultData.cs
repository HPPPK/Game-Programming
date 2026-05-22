/*
 * File: GameResultData.cs
 *
 * Purpose:
 * Stores final ranking data between GameScene and ResultScene. GamePhaseManager
 * fills this static data when the match ends, and ResultSceneManager reads it to
 * build the final ranking UI.
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

    public PlayerResultEntry(int playerId, string displayName, int score, int money, int castleHp)
    {
        this.playerId = playerId;
        this.displayName = displayName;
        this.score = score;
        this.money = money;
        this.castleHp = castleHp;
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
