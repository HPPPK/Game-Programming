/*
 * File: EnemyTargetSelector.cs
 *
 * Purpose:
 * This static helper chooses a random reachable castle from a list of possible
 * CastleEndNode targets. It is a lightweight target-selection utility that uses
 * Pathfinder.FindPath() only to test whether each castle can be reached.
 *
 * Runtime behavior:
 * - ChooseTarget() loops through every castle candidate.
 * - If Pathfinder.FindPath(start, castle) returns a path, that castle is reachable.
 * - A random reachable castle is returned.
 * - If no castle is reachable, the method logs a warning and returns null.
 *
 * Dependency notes:
 * - Pathfinder owns the actual graph search.
 * - PathNode is the starting point in the path graph.
 * - CastleEndNode represents a valid endpoint/castle target.
 *
 * Current project note:
 * - EnemyPathAssignmentManager contains the richer assignment logic used by
 *   EnemyMover. This helper can still be useful for simpler/random target tests.
 */
using System.Collections.Generic;
using UnityEngine;

public static class EnemyTargetSelector
{
    public static CastleEndNode ChooseTarget(PathNode start, CastleEndNode[] castles)
    {
        List<CastleEndNode> reachable = new List<CastleEndNode>();

        foreach (var c in castles)
        {
            var path = Pathfinder.FindPath(start, c);
            if (path != null)
            {
                reachable.Add(c);
            }
        }

        if (reachable.Count == 0)
        {
            Debug.LogWarning("No reachable castle!");
            return null;
        }

        int index = Random.Range(0, reachable.Count);
        return reachable[index];
    }
}
