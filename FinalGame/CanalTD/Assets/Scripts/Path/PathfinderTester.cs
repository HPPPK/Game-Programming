/*
 * File: PathfinderTester.cs
 *
 * Purpose:
 * This is a debug-only helper for checking whether the path graph can reach each
 * castle endpoint from a chosen start node. It does not move enemies or affect
 * gameplay state; it only logs pathfinder results to the Console.
 *
 * Runtime behavior:
 * - Press T while the scene is running to call TestPaths().
 * - TestPaths() checks every CastleEndNode in castleEnds.
 * - It logs whether each castle is reachable from startNode.
 * - If reachable, it also logs the selected path length.
 *
 * Inspector setup:
 * - startNode should be the path node where the test begins.
 * - castleEnds should contain the castle endpoints you want to verify.
 *
 * Dependency notes:
 * - Uses Pathfinder.FindPath().
 * - Safe to disable or remove in final gameplay builds if no longer needed.
 */
using System.Collections.Generic;
using UnityEngine;

public class PathfinderTester : MonoBehaviour
{
    public PathNode startNode;
    public CastleEndNode[] castleEnds;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestPaths();
        }
    }

    void TestPaths()
    {
        Debug.Log("===== PATHFINDER TEST START =====");

        foreach (CastleEndNode castle in castleEnds)
        {
            List<PathNode> path = Pathfinder.FindPath(startNode, castle);

            if (path == null)
            {
                Debug.LogWarning(startNode.name + " cannot reach " + castle.name);
            }
            else
            {
                Debug.Log(startNode.name + " can reach " + castle.name + 
                          " | path length = " + path.Count);
            }
        }

        Debug.Log("===== PATHFINDER TEST END =====");
    }
}
