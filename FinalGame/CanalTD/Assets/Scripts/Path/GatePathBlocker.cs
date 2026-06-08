/*
 * File: GatePathBlocker.cs
 *
 * Purpose:
 * Implements GatePathBlocker for the path layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Path nodes, edges, gates, or path-debug objects placed in the gameplay scene.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for GatePathBlocker within the path system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Path graph data, wave settings, target selection, or movement parameters.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify GatePathBlocker in the scene or prefab where it is used and confirm the main happy path still works.
 */
using UnityEngine;

public class GatePathBlocker : MonoBehaviour
{
    [Header("Gate")]
    public GateFrameAnimation linkedGate;

    [Header("Blocked Path Segment")]
    public PathNode nodeA;
    public PathNode nodeB;

    public bool BlocksConnection(PathNode firstNode, PathNode secondNode)
    {
        if (linkedGate == null || nodeA == null || nodeB == null)
        {
            return false;
        }

        if (!linkedGate.IsBlocking())
        {
            return false;
        }

        return IsSameConnection(firstNode, secondNode);
    }

    private bool IsSameConnection(PathNode firstNode, PathNode secondNode)
    {
        return (firstNode == nodeA && secondNode == nodeB) ||
               (firstNode == nodeB && secondNode == nodeA);
    }

    public static bool IsConnectionBlocked(PathNode firstNode, PathNode secondNode)
    {
        return IsConnectionBlocked(firstNode, secondNode, FindObjectsOfType<GatePathBlocker>());
    }

    public static bool IsConnectionBlocked(
        PathNode firstNode,
        PathNode secondNode,
        GatePathBlocker[] blockers
    )
    {
        if (firstNode == null || secondNode == null)
        {
            return false;
        }

        if (blockers == null)
        {
            return false;
        }

        foreach (GatePathBlocker blocker in blockers)
        {
            if (blocker != null && blocker.BlocksConnection(firstNode, secondNode))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        if (nodeA == null || nodeB == null)
        {
            return;
        }

        Gizmos.color = linkedGate != null && linkedGate.IsBlocking() ? Color.red : Color.green;
        Gizmos.DrawLine(nodeA.transform.position, nodeB.transform.position);
    }
}
