/*
 * File: WaypointPath.cs
 *
 * Purpose:
 * This script stores an ordered list of Transform waypoints. It is useful for
 * simple waypoint movement where an object follows points in sequence instead
 * of using the PathNode/PathEdge graph system.
 *
 * Runtime behavior:
 * - PathId exposes a readable identifier for this waypoint path.
 * - Count exposes how many waypoint transforms are assigned.
 * - GetWaypoint(index) returns the Transform at an index or null if invalid.
 * - GetWaypointPosition(index) returns a waypoint position, falling back to this
 *   GameObject's position if the index is invalid.
 * - GetNextPathForDirectionA/B expose optional future branch paths.
 *
 * Inspector setup:
 * - waypoints should be ordered from first movement point to last movement point.
 * - nextPathForDirectionA and nextPathForDirectionB are optional branch links.
 *
 * Dependency notes:
 * - This script is separate from Pathfinder and PathNode.
 * - It can be used for simpler linear paths or future branching experiments.
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
