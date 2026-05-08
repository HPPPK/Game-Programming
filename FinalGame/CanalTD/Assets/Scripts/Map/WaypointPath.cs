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
