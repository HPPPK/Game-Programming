/*
 * File: EnemyMover.cs
 *
 * Purpose:
 * Implements EnemyMover for the enemy layer of CanalTD and supports the playable vertical slice of the project.
 *
 * Attached GameObject:
 * Enemy prefabs, wave helpers, or enemy-related scene managers.
 *
 * Main responsibilities:
 * - Provide the runtime behaviour for EnemyMover within the enemy system.
 * - Update the owning object state and react to gameplay events during play.
 * - Support enemy spawning, pathing, targeting, combat, or wave pressure behaviour.
 *
 * Inputs:
 * - Inspector references configured in Unity.
 * - Runtime state from connected managers, scene objects, or event callbacks.
 * - Path graph data, wave settings, target selection, or movement parameters.
 *
 * Outputs or effects:
 * - Changes scene state, gameplay data, or visual feedback in the active match.
 * - Moves enemies, adjusts combat results, or influences castle pressure and wave outcomes.
 *
 * Authorship or assistance:
 * - Core gameplay design, Unity setup, and project integration were developed by Jingyu Pan.
 * - This documentation header was expanded with AI assistance to match the assessment comment standard.
 *
 * Testing notes:
 * - Verify EnemyMover in the scene or prefab where it is used and confirm the main happy path still works.
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
    private EnemyStats enemyStats;
    private int appliedRound = 1;
    private float appliedSpeedScalePerRound = 0f;

    private bool initialized = false;

    /// <summary>
    /// Finds and stores enemy mover references before scene gameplay begins.
    /// </summary>
    void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        ResolveVisualRoot();
        ApplyVisualOffset();
        spriteRenderer = visualRoot != null
            ? visualRoot.GetComponent<SpriteRenderer>()
            : GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Handles init for enemy movement, health, waves, or routing.
    /// </summary>
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

        ApplyStatsSpeed();
    }

    /// <summary>
    /// Applies stats speed to gameplay data and updates visible feedback.
    /// </summary>
    public void ApplyStatsSpeed(int round, float speedScalePerRound)
    {
        appliedRound = Mathf.Max(1, round);
        appliedSpeedScalePerRound = speedScalePerRound;
        ApplyStatsSpeed();
    }

    /// <summary>
    /// Handles copy path progress from for enemy movement, health, waves, or routing.
    /// </summary>
    public void CopyPathProgressFrom(EnemyMover source, Vector3 spawnPosition)
    {
        if (source == null || source.path == null || source.path.Count == 0)
        {
            return;
        }

        path = source.path;
        currentPathIndex = Mathf.Clamp(source.currentPathIndex, 1, path.Count - 1);
        castleDamage = source.castleDamage;
        transform.position = spawnPosition;
        initialized = true;
        ApplyStatsSpeed();
    }

    /// <summary>
    /// Checks enemy mover input, timing, animation, or UI state once per frame.
    /// </summary>
    void Update()
    {
        if (!initialized) return;
        if (path == null || currentPathIndex >= path.Count) return;

        MoveAlongPath();
    }

    /// <summary>
    /// Keeps enemy mover visuals aligned after normal frame updates finish.
    /// </summary>
    void LateUpdate()
    {
        if (!enforceVisualOffsetEveryFrame)
        {
            return;
        }

        ResolveVisualRoot();
        ApplyVisualOffset();
    }

    /// <summary>
    /// Moves the enemy toward the next node on its assigned path.
    /// </summary>
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

    /// <summary>
    /// Applies stats speed to gameplay data and updates visible feedback.
    /// </summary>
    private void ApplyStatsSpeed()
    {
        enemyStats = enemyStats != null ? enemyStats : GetComponent<EnemyStats>();

        if (enemyStats == null)
        {
            return;
        }

        moveSpeed = enemyStats.GetFinalSpeed(appliedRound, appliedSpeedScalePerRound);
    }

    /// <summary>
    /// Looks up the target for visual root and applies the resolved gameplay result.
    /// </summary>
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

    /// <summary>
    /// Applies visual offset to gameplay data and updates visible feedback.
    /// </summary>
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

    /// <summary>
    /// Handles align sprite center to path center for enemy movement, health, waves, or routing.
    /// </summary>
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

    /// <summary>
    /// Returns node position used by enemy spawning, movement, waves, or routing.
    /// </summary>
    private Vector3 GetNodePosition(PathNode node)
    {
        if (node == null)
        {
            return transform.position;
        }

        return node.transform.position + pathWorldOffset;
    }
}
