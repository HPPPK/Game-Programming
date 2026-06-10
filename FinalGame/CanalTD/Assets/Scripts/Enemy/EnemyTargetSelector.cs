/*
 * File: EnemyTargetSelector.cs
 *
 * Purpose:
 * Implements EnemyTargetSelector for the enemy layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemyTargetSelector within the enemy system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify EnemyTargetSelector in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections.Generic;
using UnityEngine;

public static class EnemyTargetSelector
{
    /// <summary>
    /// Handles choose target for card state, hand state, or targeting.
    /// </summary>
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
