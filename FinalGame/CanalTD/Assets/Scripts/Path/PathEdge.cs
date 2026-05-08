/*
 * File: PathEdge.cs
 *
 * Purpose:
 * This serializable data class represents one directed connection from a PathNode
 * to another PathNode. It can optionally be controlled by a gate, which means
 * the path is only usable when that gate is open.
 *
 * Runtime behavior:
 * - targetNode is the node reached by following this edge.
 * - linkedGate is optional. If it is null, the edge is always open.
 * - If linkedGate is assigned, IsOpen() returns false when the gate is blocking.
 * - priority is stored for possible path ordering or designer preference.
 *
 * Inspector setup:
 * - PathNode exposes a List<PathEdge>, so these fields appear inside each node.
 * - Assign targetNode for every edge.
 * - Assign linkedGate only when a gate should control that connection.
 *
 * Dependency notes:
 * - PathNode owns lists of PathEdge objects.
 * - Pathfinder checks IsOpen() before searching through an edge.
 * - GateFrameAnimation provides the IsBlocking() state used here.
 */
using UnityEngine;

[System.Serializable]
public class PathEdge
{
    public PathNode targetNode;

    [Header("Optional Gate")]
    public GateFrameAnimation linkedGate;

    [Header("Priority")]
    public int priority = 0;

    public bool IsOpen()
    {
        if (targetNode == null)
        {
            return false;
        }

        if (linkedGate == null)
        {
            return true;
        }

        return !linkedGate.IsBlocking();
    }
}
