/*
 * File: EnemyMover.cs
 *
 * Purpose:
 * This script moves one spawned enemy along a selected path made of PathNode
 * objects. The enemy does not decide the path by itself; it asks
 * EnemyPathAssignmentManager for a path from the spawn node to a reachable castle.
 *
 * Main gameplay flow:
 * 1. EnemySpawner instantiates an enemy prefab and calls Init(startNode).
 * 2. Init() asks EnemyPathAssignmentManager for a valid List<PathNode>.
 * 3. The enemy starts at path[0], then moves toward path[1], path[2], and so on.
 * 4. MoveAlongPath() uses Vector3.MoveTowards() every frame.
 * 5. When the enemy reaches a CastleEndNode, the castle endpoint applies damage
 *    and destroys the enemy.
 *
 * Inspector setup:
 * - moveSpeed controls movement speed in world units per second.
 * - reachDistance controls how close the enemy must be to count as arrived.
 * - The enemy prefab should have a SpriteRenderer if horizontal flipping is desired.
 *
 * Dependency notes:
 * - EnemyPathAssignmentManager chooses the path and balances targets.
 * - PathNode and PathEdge define the graph used by the pathfinder.
 * - CastleEndNode handles final damage when the enemy reaches a castle.
 */
using System.Collections.Generic;
using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float reachDistance = 0.05f;

    [Header("Castle Damage")]
    public int castleDamage = 1;

    private List<PathNode> path;
    private int currentPathIndex = 0;
    private SpriteRenderer spriteRenderer;

    private bool initialized = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Init(PathNode startNode)
    {
        if (startNode == null)
        {
            Debug.LogWarning(name + " init failed: startNode is null.");
            Destroy(gameObject);
            return;
        }

        if (EnemyPathAssignmentManager.Instance == null)
        {
            Debug.LogWarning("No EnemyPathAssignmentManager found.");
            Destroy(gameObject);
            return;
        }

        path = EnemyPathAssignmentManager.Instance.GetPathToLeastAssignedReachableCastle(startNode);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning(name + " cannot find path.");
            Destroy(gameObject);
            return;
        }

        transform.position = startNode.transform.position;

        // path[0] is startNode, so movement begins at path[1].
        currentPathIndex = 1;
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        if (path == null || currentPathIndex >= path.Count) return;

        MoveAlongPath();
    }

    void MoveAlongPath()
    {
        PathNode targetNode = path[currentPathIndex];
        Vector3 targetPosition = targetNode.transform.position;
        Vector3 direction = targetPosition - transform.position;

        if (spriteRenderer != null)
        {
            if (direction.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= reachDistance)
        {
            CastleEndNode castleEndNode = targetNode as CastleEndNode;

            if (castleEndNode != null)
            {
                castleEndNode.OnEnemyArrive(gameObject);
                return;
            }

            currentPathIndex++;
        }
    }
}
