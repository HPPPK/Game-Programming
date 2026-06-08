/*
 * File: PathfinderTester.cs
 *
 * Purpose:
 * Implements PathfinderTester for the path layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Path nodes, edges, gates, or path-debug objects placed in the gameplay scene.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PathfinderTester within the path system.
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
 * - Verify PathfinderTester in the scene or prefab where it is used and confirm the main happy path still works.
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
