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
 * - visualRoot is the child object that contains the enemy SpriteRenderer.
 * - visualLocalOffset moves only the sprite/animation, not the path-following
 *   root position. Use this when a sprite's pivot makes the enemy look too high
 *   or too low on the path.
 * - alignSpriteCenterToPathCenter forces the visible sprite bounds center to sit
 *   on the enemy root/path center. This is useful for sprites with bad pivots.
 * - pathWorldOffset moves the whole enemy along the path. Use this when the
 *   prefab should walk slightly above/below the route itself.
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

    [Header("Visual Alignment")]
    public Transform visualRoot;
    public Vector3 visualLocalOffset = Vector3.zero;
    public bool enforceVisualOffsetEveryFrame = true;
    public bool alignSpriteCenterToPathCenter = false;
    public Vector3 spriteCenterWorldOffset = Vector3.zero;

    [Header("Path Alignment")]
    public Vector3 pathWorldOffset = Vector3.zero;

    private List<PathNode> path;
    private int currentPathIndex = 0;
    private SpriteRenderer spriteRenderer;

    private bool initialized = false;

    void Awake()
    {
        ResolveVisualRoot();
        ApplyVisualOffset();
        spriteRenderer = visualRoot != null
            ? visualRoot.GetComponent<SpriteRenderer>()
            : GetComponent<SpriteRenderer>();
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

        transform.position = GetNodePosition(startNode);

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

    void LateUpdate()
    {
        if (!enforceVisualOffsetEveryFrame)
        {
            return;
        }

        ResolveVisualRoot();
        ApplyVisualOffset();
    }

    void MoveAlongPath()
    {
        PathNode targetNode = path[currentPathIndex];
        Vector3 targetPosition = GetNodePosition(targetNode);
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

    private void ResolveVisualRoot()
    {
        if (visualRoot != null)
        {
            return;
        }

        Transform namedVisual = transform.Find("Visual");

        if (namedVisual != null)
        {
            visualRoot = namedVisual;
            return;
        }

        SpriteRenderer childSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (childSpriteRenderer != null && childSpriteRenderer.transform != transform)
        {
            visualRoot = childSpriteRenderer.transform;
        }
    }

    private void ApplyVisualOffset()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition = visualLocalOffset;

        if (alignSpriteCenterToPathCenter)
        {
            AlignSpriteCenterToPathCenter();
        }
    }

    private void AlignSpriteCenterToPathCenter()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        Vector3 desiredCenter = transform.position + spriteCenterWorldOffset;
        Vector3 currentCenter = spriteRenderer.bounds.center;
        Vector3 correction = desiredCenter - currentCenter;
        visualRoot.position += correction;
    }

    private Vector3 GetNodePosition(PathNode node)
    {
        if (node == null)
        {
            return transform.position;
        }

        return node.transform.position + pathWorldOffset;
    }
}
