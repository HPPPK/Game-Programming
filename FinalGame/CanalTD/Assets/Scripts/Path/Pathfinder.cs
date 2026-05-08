/*
 * File: Pathfinder.cs
 *
 * Purpose:
 * This static utility searches the PathNode graph for valid paths from a start
 * node to a specific CastleEndNode. It uses depth-first search and respects
 * gate-controlled edges by checking PathEdge.IsOpen().
 *
 * Public methods:
 * - FindPath(startNode, targetEnd) returns one path, currently the longest path
 *   found after sorting all valid paths by length.
 * - FindAllPaths(startNode, targetEnd, maxDepth, maxPaths) returns every valid
 *   path found within safety limits.
 *
 * Search behavior:
 * - DFS prevents loops by tracking visited nodes in the current branch.
 * - maxDepth prevents runaway recursion in very large or accidental cyclic graphs.
 * - maxPaths prevents the search from collecting too many path variations.
 * - Edges that are null, closed by a gate, or point to already visited nodes are
 *   skipped.
 *
 * Dependency notes:
 * - PathNode provides outgoing edges.
 * - PathEdge.IsOpen() decides whether an edge is currently usable.
 * - CastleEndNode marks the search target.
 * - EnemyPathAssignmentManager uses this class to build path options.
 */
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    public static List<PathNode> FindPath(PathNode startNode, CastleEndNode targetEnd)
    {
        List<List<PathNode>> paths = FindAllPaths(startNode, targetEnd, 40, 50);

        if (paths.Count == 0)
        {
            return null;
        }

        paths.Sort((a, b) => b.Count.CompareTo(a.Count));
        return paths[0];
    }

    public static List<List<PathNode>> FindAllPaths(
        PathNode startNode,
        CastleEndNode targetEnd,
        int maxDepth = 40,
        int maxPaths = 50
    )
    {
        List<List<PathNode>> allPaths = new List<List<PathNode>>();

        if (startNode == null || targetEnd == null)
        {
            return allPaths;
        }

        List<PathNode> currentPath = new List<PathNode>();
        HashSet<PathNode> visited = new HashSet<PathNode>();

        DFS(
            startNode,
            targetEnd,
            visited,
            currentPath,
            allPaths,
            maxDepth,
            maxPaths
        );

        allPaths.Sort((a, b) => b.Count.CompareTo(a.Count));
        return allPaths;
    }

    private static void DFS(
        PathNode current,
        CastleEndNode target,
        HashSet<PathNode> visited,
        List<PathNode> currentPath,
        List<List<PathNode>> allPaths,
        int maxDepth,
        int maxPaths
    )
    {
        if (current == null) return;
        if (allPaths.Count >= maxPaths) return;
        if (currentPath.Count > maxDepth) return;

        visited.Add(current);
        currentPath.Add(current);

        if (current == target)
        {
            allPaths.Add(new List<PathNode>(currentPath));
        }
        else
        {
            foreach (PathEdge edge in current.edges)
            {
                if (edge == null) continue;
                if (!edge.IsOpen()) continue;

                PathNode next = edge.targetNode;

                if (next == null) continue;
                if (visited.Contains(next)) continue;

                DFS(
                    next,
                    target,
                    visited,
                    currentPath,
                    allPaths,
                    maxDepth,
                    maxPaths
                );
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        visited.Remove(current);
    }
}
