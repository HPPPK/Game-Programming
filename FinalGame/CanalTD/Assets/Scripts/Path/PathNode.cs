using UnityEngine;
using System.Collections.Generic;

public class PathNode : MonoBehaviour
{
    [System.Serializable]
    public class PathConnection
    {
        public PathNode targetNode;
    }

    [Header("Outgoing Paths")]
    public List<PathConnection> edges = new List<PathConnection>();

    private int nextEdgeIndex = 0;

    public PathNode GetNextNode(PathNode previousNode)
    {
        if (edges == null || edges.Count == 0)
        {
            return null;
        }

        List<PathNode> candidates = new List<PathNode>();

        foreach (PathConnection edge in edges)
        {
            if (edge == null || edge.targetNode == null)
            {
                continue;
            }

            if (edge.targetNode == previousNode)
            {
                continue;
            }

            candidates.Add(edge.targetNode);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        PathNode selectedNode = candidates[nextEdgeIndex % candidates.Count];
        nextEdgeIndex++;

        return selectedNode;
    }

    private void OnDrawGizmos()
    {
        if (edges == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.1f);

        foreach (PathConnection edge in edges)
        {
            if (edge == null || edge.targetNode == null) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, edge.targetNode.transform.position);
        }
    }
}
