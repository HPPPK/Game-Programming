/*
 * File: PathEdge.cs
 *
 * Purpose:
 * Implements PathEdge for the path layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * None. This script defines shared data or types and is not attached directly to a GameObject.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for PathEdge within the path system.
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
 * - Verify PathEdge in the scene or prefab where it is used and confirm the main happy path still works.
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

    /// <summary>
    /// Checks the current state to decide whether open is true.
    /// </summary>
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
