using UnityEngine;
using System.Collections.Generic;

public class PathNode : MonoBehaviour
{
    [System.Serializable]
    public class PathConnection
    {
        public PathNode targetNode;
        public GateFrameAnimation linkedGate;
        public int priority;

        public bool IsOpen()
        {
            return targetNode != null && (linkedGate == null || !linkedGate.IsBlocking());
        }
    }

    [Header("Outgoing Paths")]
    public List<PathConnection> edges = new List<PathConnection>();

    private int nextEdgeIndex = 0;

    public PathNode GetNextNode(PathNode previousNode)
    {
        List<PathConnection> availableEdges = new List<PathConnection>();

        foreach (PathConnection edge in edges)
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

        PathConnection selectedEdge = availableEdges[nextEdgeIndex % availableEdges.Count];
        nextEdgeIndex++;

        return selectedEdge.targetNode;
    }

    private void OnDrawGizmos()
    {
        if (edges == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.1f);

        foreach (PathConnection edge in edges)
        {
            if (edge == null || edge.targetNode == null) continue;

            Gizmos.color = edge.IsOpen() ? Color.cyan : Color.red;
            Gizmos.DrawLine(transform.position, edge.targetNode.transform.position);
        }
    }
}
