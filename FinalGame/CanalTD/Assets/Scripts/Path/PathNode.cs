/*
 * File: PathNode.cs
 *
 * Purpose:
 * This script represents one point in the enemy path graph. Designers connect
 * nodes by filling the edges list in the Inspector. Enemy movement and pathfinder
 * logic use these connections to decide where enemies can travel.
 *
 * Runtime behavior:
 * - edges stores outgoing PathEdge connections from this node to other nodes.
 * - GetNextNode(previousNode) returns one open outgoing node for simple movement.
 * - Closed or blocked edges are ignored through PathEdge.IsOpen().
 * - The method avoids immediately returning to previousNode when another option
 *   exists, which helps prevent enemies from bouncing backward.
 * - nextEdgeIndex rotates through available edges to spread enemies across branches.
 *
 * Scene visualization:
 * - OnDrawGizmos() draws a small sphere at the node position.
 * - Always-open edges are drawn cyan.
 * - Gate-controlled edges are drawn red so they are easier to inspect.
 *
 * Dependency notes:
 * - PathEdge stores the target node and optional linked gate.
 * - Pathfinder searches through PathNode.edges.
 * - CastleEndNode inherits from PathNode and acts as a final destination.
 */
using UnityEngine;
using System.Collections.Generic;

public class PathNode : MonoBehaviour
{
    [Header("Outgoing Paths")]
    public List<PathEdge> edges = new List<PathEdge>();

    private int nextEdgeIndex = 0;

    public PathNode GetNextNode(PathNode previousNode)
    {
        List<PathEdge> availableEdges = new List<PathEdge>();

        foreach (PathEdge edge in edges)
        {
            if (edge == null || !edge.IsOpen())
            {
                continue;
            }

            if (edge.targetNode == previousNode)
            {
                continue;
            }

            availableEdges.Add(edge);
        }

        if (availableEdges.Count == 0)
        {
            return previousNode;
        }

        PathEdge selectedEdge = availableEdges[nextEdgeIndex % availableEdges.Count];
        nextEdgeIndex++;

        return selectedEdge.targetNode;
    }

    private void OnDrawGizmos()
    {
        if (edges == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.1f);

        foreach (PathEdge edge in edges)
        {
            if (edge == null || edge.targetNode == null) continue;

            Gizmos.color = edge.linkedGate == null ? Color.cyan : Color.red;
            Gizmos.DrawLine(transform.position, edge.targetNode.transform.position);
        }
    }
}
