/*
 * File: GatePathBlocker.cs
 *
 * Purpose:
 * This script links one GateFrameAnimation to the two PathNode objects that sit
 * on both sides of that gate. It makes the path graph treat those two nodes as
 * disconnected whenever the linked gate is currently blocking.
 *
 * Why this exists:
 * - Pathfinder searches the node graph as an undirected graph.
 * - That means one Inspector edge between two neighboring nodes can be used from
 *   either direction.
 * - A gate still needs to cut that connection when it is blocking.
 * - This component gives the gate an explicit "this blocks node A <-> node B"
 *   relationship, independent from the direction of the PathEdge list.
 *
 * Inspector setup:
 * - linkedGate: the GateFrameAnimation that opens/closes.
 * - nodeA: the path node on one side of the gate.
 * - nodeB: the path node on the other side of the gate.
 *
 * Runtime behavior:
 * - If linkedGate is null, this blocker does nothing.
 * - If nodeA or nodeB is missing, this blocker does nothing.
 * - If linkedGate.IsBlocking() is true, Pathfinder cannot move between nodeA
 *   and nodeB in either direction.
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
