using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Start Node")]
    public PathNode startNode;

    public void SpawnOne()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(name + " spawn failed: enemyPrefab is missing.");
            return;
        }

        if (startNode == null)
        {
            Debug.LogError(name + " spawn failed: startNode is missing.");
            return;
        }

        GameObject enemy = Instantiate(
            enemyPrefab,
            startNode.transform.position,
            Quaternion.identity
        );

        EnemyMover mover = enemy.GetComponent<EnemyMover>();

        if (mover == null)
        {
            Debug.LogError("Spawned enemy has no EnemyMover component.");
            Destroy(enemy);
            return;
        }

        mover.Init(startNode);
    }
}