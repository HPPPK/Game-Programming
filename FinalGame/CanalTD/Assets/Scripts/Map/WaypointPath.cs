/*
 * File: WaypointPath.cs
 *
 * Purpose:
 * Implements WaypointPath for the map layer of Rail Rumble and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Map props, gate visuals, or waypoint/path presentation objects.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for WaypointPath within the map system.
 * - Update the owning object state and react to gameplay events during play.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Panjingyu and teammates.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify WaypointPath in the scene or prefab where it is used and confirm the main happy path still works.
 */
using System.Collections.Generic;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private string pathId = "Path_01";
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Future Branching (Optional)")]
    [SerializeField] private WaypointPath nextPathForDirectionA;
    [SerializeField] private WaypointPath nextPathForDirectionB;

    public string PathId => pathId;
    public int Count => waypoints.Count;

    public Transform GetWaypoint(int index)
    {
        if (index < 0 || index >= waypoints.Count)
        {
            return null;
        }

        return waypoints[index];
    }

    public Vector3 GetWaypointPosition(int index)
    {
        Transform point = GetWaypoint(index);
        return point != null ? point.position : transform.position;
    }

    public WaypointPath GetNextPathForDirectionA()
    {
        return nextPathForDirectionA;
    }

    public WaypointPath GetNextPathForDirectionB()
    {
        return nextPathForDirectionB;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform current = waypoints[i];
            if (current == null)
            {
                continue;
            }

            Gizmos.DrawSphere(current.position, 0.12f);

            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(current.position, waypoints[i + 1].position);
            }
        }
    }
}
