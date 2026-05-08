using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float reachDistance = 0.05f;

    private PathNode previousNode;
    private PathNode currentNode;
    private PathNode targetNode;
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

        previousNode = null;
        currentNode = startNode;
        targetNode = currentNode.GetNextNode(previousNode);
        transform.position = startNode.transform.position;

        if (targetNode == null)
        {
            Debug.LogWarning(name + " has no next path node.");
            Destroy(gameObject);
            return;
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;
        if (targetNode == null) return;

        MoveToTargetNode();
    }

    void MoveToTargetNode()
    {
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
            transform.position = targetPosition;
            ArriveAtTargetNode();
        }
    }

    void ArriveAtTargetNode()
    {
        CastleEndNode castleEndNode = targetNode as CastleEndNode;

        if (castleEndNode != null)
        {
            castleEndNode.OnEnemyArrive(gameObject);
            initialized = false;
            return;
        }

        previousNode = currentNode;
        currentNode = targetNode;
        targetNode = currentNode.GetNextNode(previousNode);

        if (targetNode == null)
        {
            Debug.LogWarning(name + " reached a path node with no next node: " + currentNode.name);
            Destroy(gameObject);
        }
    }
}
