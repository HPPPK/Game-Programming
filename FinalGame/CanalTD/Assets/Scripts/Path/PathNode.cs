/*
 * File: PathNode.cs
 *
 * Purpose:
 * Implements PathNode for the path layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Path nodes, edges, gates, or path-debug objects placed in the gameplay scene.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PathNode within the path system.
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
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify PathNode in the scene or prefab where it is used and confirm the main happy path still works.
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
