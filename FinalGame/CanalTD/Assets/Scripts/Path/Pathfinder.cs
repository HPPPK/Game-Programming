/*
 * File: Pathfinder.cs
 *
 * Purpose:
 * Implements Pathfinder for the path layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for Pathfinder within the path system.
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
 * - Verify Pathfinder in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    public static List<PathNode> FindPath(PathNode startNode, CastleEndNode targetEnd)
    {
        List<List<PathNode>> paths = FindAllPaths(startNode, targetEnd, 80, 300);

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
        PathNode[] allNodes = UnityEngine.Object.FindObjectsOfType<PathNode>();
        GatePathBlocker[] gateBlockers = UnityEngine.Object.FindObjectsOfType<GatePathBlocker>();

        DFS(
            startNode,
            targetEnd,
            visited,
            currentPath,
            allPaths,
            allNodes,
            gateBlockers,
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
        PathNode[] allNodes,
        GatePathBlocker[] gateBlockers,
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
            SearchNeighborNodes(
                current,
                target,
                visited,
                currentPath,
                allPaths,
                allNodes,
                gateBlockers,
                maxDepth,
                maxPaths
            );
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        visited.Remove(current);
    }

    private static void SearchNeighborNodes(
        PathNode current,
        CastleEndNode target,
        HashSet<PathNode> visited,
        List<PathNode> currentPath,
        List<List<PathNode>> allPaths,
        PathNode[] allNodes,
        GatePathBlocker[] gateBlockers,
        int maxDepth,
        int maxPaths
    )
    {
        List<PathNode> neighbors = GetOpenUndirectedNeighbors(current, allNodes, gateBlockers);

        foreach (PathNode next in neighbors)
        {
            if (next == null) continue;
            if (visited.Contains(next)) continue;

            DFS(
                next,
                target,
                visited,
                currentPath,
                allPaths,
                allNodes,
                gateBlockers,
                maxDepth,
                maxPaths
            );
        }
    }

    private static List<PathNode> GetOpenUndirectedNeighbors(
        PathNode current,
        PathNode[] allNodes,
        GatePathBlocker[] gateBlockers
    )
    {
        List<PathNode> neighbors = new List<PathNode>();
        HashSet<PathNode> addedNodes = new HashSet<PathNode>();

        AddOutgoingNeighbors(current, neighbors, addedNodes, gateBlockers);
        AddIncomingNeighbors(current, allNodes, neighbors, addedNodes, gateBlockers);

        return neighbors;
    }

    private static void AddOutgoingNeighbors(
        PathNode current,
        List<PathNode> neighbors,
        HashSet<PathNode> addedNodes,
        GatePathBlocker[] gateBlockers
    )
    {
        if (current.edges == null)
        {
            return;
        }

        foreach (PathEdge edge in current.edges)
        {
            if (edge == null) continue;
            if (!edge.IsOpen()) continue;
            if (edge.targetNode == null) continue;
            if (GatePathBlocker.IsConnectionBlocked(current, edge.targetNode, gateBlockers)) continue;
            if (addedNodes.Contains(edge.targetNode)) continue;

            neighbors.Add(edge.targetNode);
            addedNodes.Add(edge.targetNode);
        }
    }

    private static void AddIncomingNeighbors(
        PathNode current,
        PathNode[] allNodes,
        List<PathNode> neighbors,
        HashSet<PathNode> addedNodes,
        GatePathBlocker[] gateBlockers
    )
    {
        if (allNodes == null)
        {
            return;
        }

        foreach (PathNode possibleNeighbor in allNodes)
        {
            if (possibleNeighbor == null) continue;
            if (possibleNeighbor == current) continue;
            if (possibleNeighbor.edges == null) continue;

            foreach (PathEdge edge in possibleNeighbor.edges)
            {
                if (edge == null) continue;
                if (edge.targetNode != current) continue;
                if (!edge.IsOpen()) continue;
                if (GatePathBlocker.IsConnectionBlocked(current, possibleNeighbor, gateBlockers)) continue;
                if (addedNodes.Contains(possibleNeighbor)) continue;

                neighbors.Add(possibleNeighbor);
                addedNodes.Add(possibleNeighbor);
            }
        }
    }
}
