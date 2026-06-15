/*
 * File: GameResultData.cs
 *
 * Purpose:
 * Implements GameResultData for the core layer of CanalTD and supports the playable vertical slice of the project.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
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

    /// <summary>
    /// Plays player result entry in the current scene context.
    /// </summary>
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

    /// <summary>
    /// Sets results and immediately updates the related state, UI, or visuals.
    /// </summary>
    public static void SetResults(List<PlayerResultEntry> results)
    {
        Results = results != null ? new List<PlayerResultEntry>(results) : new List<PlayerResultEntry>();
    }

    /// <summary>
    /// Clears clear and removes its temporary gameplay or visual effect.
    /// </summary>
    public static void Clear()
    {
        Results.Clear();
    }
}

public static class GameResultBuilder
{
    public static List<PlayerResultEntry> BuildRankedResults(PlayerManager playerManager, bool skipEmptyPlayers)
    {
        List<PlayerResultEntry> results = BuildResultEntries(playerManager, skipEmptyPlayers);
        SortResultsByScore(results);
        AssignRanks(results);
        return results;
    }

    private static List<PlayerResultEntry> BuildResultEntries(PlayerManager playerManager, bool skipEmptyPlayers)
    {
        List<PlayerResultEntry> results = new List<PlayerResultEntry>();

        if (playerManager == null || playerManager.players == null)
        {
            return results;
        }

        foreach (PlayerResource player in playerManager.players)
        {
            if (player == null)
            {
                continue;
            }

            if (skipEmptyPlayers && player.playerType == PlayerType.Empty)
            {
                continue;
            }

            CastleBase castle = GetCastleForPlayer(playerManager, player);
            int castleHp = castle != null ? castle.currentHP : 0;

            results.Add(new PlayerResultEntry(
                player.playerId,
                player.GetDisplayName(),
                player.score,
                player.money,
                castleHp,
                player.isEliminated
            ));
        }

        return results;
    }

    private static CastleBase GetCastleForPlayer(PlayerManager playerManager, PlayerResource player)
    {
        if (player == null)
        {
            return null;
        }

        CastleBase castle = player.GetComponent<CastleBase>();

        if (castle != null)
        {
            return castle;
        }

        if (player.statusPanel != null && player.statusPanel.linkedCastle != null)
        {
            return player.statusPanel.linkedCastle;
        }

        PlayerVisualConfig visualConfig = playerManager != null
            ? playerManager.GetVisualConfig(player.playerId)
            : null;

        if (visualConfig != null && visualConfig.castleReference != null)
        {
            return visualConfig.castleReference.GetComponent<CastleBase>();
        }

        return null;
    }

    private static void SortResultsByScore(List<PlayerResultEntry> results)
    {
        results.Sort((a, b) =>
        {
            if (a.isEliminated != b.isEliminated)
            {
                return a.isEliminated ? 1 : -1;
            }

            if (a.isEliminated && b.isEliminated)
            {
                return a.playerId.CompareTo(b.playerId);
            }

            int scoreCompare = b.score.CompareTo(a.score);

            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            int hpCompare = b.castleHp.CompareTo(a.castleHp);

            if (hpCompare != 0)
            {
                return hpCompare;
            }

            int moneyCompare = b.money.CompareTo(a.money);

            if (moneyCompare != 0)
            {
                return moneyCompare;
            }

            return a.playerId.CompareTo(b.playerId);
        });
    }

    private static void AssignRanks(List<PlayerResultEntry> results)
    {
        int activeRank = 1;
        int eliminatedRank = results != null ? results.Count : 0;

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].isEliminated)
            {
                results[i].rank = eliminatedRank;
            }
            else
            {
                results[i].rank = activeRank;
                activeRank += 1;
            }
        }
    }
}
