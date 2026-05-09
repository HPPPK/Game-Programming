/*
 * File: EnemyPathAssignmentManager.cs
 *
 * Purpose:
 * This singleton chooses which castle an enemy should attack and which path it
 * should follow. It uses the pathfinder to find reachable castles, keeps track
 * of how many enemies have already been assigned to each castle, and rotates
 * through available paths so enemies do not all use the exact same route.
 *
 * Main gameplay flow:
 * 1. WaveManager calls ResetAssignmentsForWave() at the start of a wave.
 * 2. EnemyMover calls GetPathToLeastAssignedReachableCastle(startNode).
 * 3. The manager finds all castles that can currently be reached from startNode.
 * 4. It creates a wave plan so every reachable castle gets enemies when the
 *    wave has enough enemies to cover all reachable castles.
 * 5. Farther castles receive a larger quota through distance weighting.
 * 6. Each castle's valid paths are also weighted by path length, so longer
 *    paths are chosen more often.
 * 7. It returns a List<PathNode> for EnemyMover to follow.
 *
 * Cache behavior:
 * - cachedPaths stores all paths between a start node and a castle.
 * - PathGraphState.Version is checked before using the cache.
 * - When a gate opens/closes and marks the graph dirty, cached paths are cleared.
 *
 * Inspector setup:
 * - castleEnds must contain every CastleEndNode that enemies can attack.
 *
 * Dependency notes:
 * - Pathfinder performs the graph search.
 * - PathGraphState tells this manager when gate/path availability changed.
 * - EnemyMover consumes the final selected path.
 */
using System.Collections.Generic;
using UnityEngine;

public class EnemyPathAssignmentManager : MonoBehaviour
{
    public static EnemyPathAssignmentManager Instance;

    private const int MaxPathDepth = 80;
    private const int MaxPaths = 300;

    [Header("Castle End Nodes")]
    public CastleEndNode[] castleEnds;

    [Header("Weighted Distribution")]
    [SerializeField] private float castleDistanceWeightPower = 1f;
    [SerializeField] private float pathLengthWeightPower = 1f;

    private Dictionary<CastleEndNode, int> assignedCounts = new Dictionary<CastleEndNode, int>();
    private Dictionary<string, List<List<PathNode>>> cachedPaths = new Dictionary<string, List<List<PathNode>>>();
    private Dictionary<PathNode, int> plannedEnemyCountsByStart = new Dictionary<PathNode, int>();
    private Dictionary<PathNode, Queue<List<PathNode>>> plannedPathsByStart = new Dictionary<PathNode, Queue<List<PathNode>>>();
    private int cachedVersion = -1;
    private int fallbackWaveEnemyCount = 1;

    void Awake()
    {
        Instance = this;
        ResetAssignments();
    }

    public void ResetAssignments()
    {
        assignedCounts.Clear();
        plannedEnemyCountsByStart.Clear();
        plannedPathsByStart.Clear();

        if (castleEnds == null)
        {
            return;
        }

        foreach (CastleEndNode castle in castleEnds)
        {
            if (castle != null)
            {
                assignedCounts[castle] = 0;
            }
        }
    }

    public void ResetAssignmentsForWave(int totalEnemyCount, EnemySpawner[] spawners)
    {
        ResetAssignments();

        fallbackWaveEnemyCount = Mathf.Max(1, totalEnemyCount);

        if (spawners == null || spawners.Length == 0)
        {
            return;
        }

        for (int i = 0; i < totalEnemyCount; i++)
        {
            EnemySpawner spawner = spawners[i % spawners.Length];

            if (spawner == null || spawner.startNode == null)
            {
                continue;
            }

            if (!plannedEnemyCountsByStart.ContainsKey(spawner.startNode))
            {
                plannedEnemyCountsByStart[spawner.startNode] = 0;
            }

            plannedEnemyCountsByStart[spawner.startNode]++;
        }
    }

    public List<PathNode> GetPathToLeastAssignedReachableCastle(PathNode startNode)
    {
        if (startNode == null)
        {
            return null;
        }

        InvalidateCacheIfNeeded();

        if (!plannedPathsByStart.ContainsKey(startNode) || plannedPathsByStart[startNode].Count == 0)
        {
            BuildPathPlanForStartNode(startNode);
        }

        if (!plannedPathsByStart.ContainsKey(startNode) || plannedPathsByStart[startNode].Count == 0)
        {
            Debug.LogWarning("No reachable castle from " + startNode.name);
            return null;
        }

        List<PathNode> selectedPath = plannedPathsByStart[startNode].Dequeue();
        CastleEndNode selectedCastle = GetPathCastle(selectedPath);

        if (selectedCastle != null)
        {
            assignedCounts[selectedCastle]++;
        }

        Debug.Log(
            "Assigned enemy to " + (selectedCastle != null ? selectedCastle.name : "UnknownCastle") +
            " | castle count = " + (selectedCastle != null ? assignedCounts[selectedCastle] : 0) +
            " | path length = " + selectedPath.Count
        );

        return selectedPath;
    }

    private void BuildPathPlanForStartNode(PathNode startNode)
    {
        List<CastlePathOptions> reachableOptions = GetReachableCastlePathOptions(startNode);

        if (reachableOptions.Count == 0)
        {
            plannedPathsByStart[startNode] = new Queue<List<PathNode>>();
            return;
        }

        int plannedEnemyCount = GetPlannedEnemyCountForStartNode(startNode);
        List<float> castleWeights = new List<float>();

        foreach (CastlePathOptions option in reachableOptions)
        {
            castleWeights.Add(Mathf.Pow(Mathf.Max(1f, option.AveragePathLength), castleDistanceWeightPower));
        }

        List<int> castleQuotas = AllocateQuotas(plannedEnemyCount, castleWeights, true);
        List<List<PathNode>> plannedPaths = new List<List<PathNode>>();
        List<Queue<List<PathNode>>> castlePathQueues = new List<Queue<List<PathNode>>>();

        for (int i = 0; i < reachableOptions.Count; i++)
        {
            CastlePathOptions option = reachableOptions[i];
            int castleQuota = castleQuotas[i];

            if (castleQuota <= 0)
            {
                continue;
            }

            List<float> pathWeights = new List<float>();

            foreach (List<PathNode> path in option.Paths)
            {
                pathWeights.Add(Mathf.Pow(Mathf.Max(1f, path.Count), pathLengthWeightPower));
            }

            List<int> pathQuotas = AllocateQuotas(castleQuota, pathWeights, false);
            List<List<PathNode>> plannedCastlePaths = new List<List<PathNode>>();

            for (int pathIndex = 0; pathIndex < option.Paths.Count; pathIndex++)
            {
                for (int copy = 0; copy < pathQuotas[pathIndex]; copy++)
                {
                    plannedCastlePaths.Add(option.Paths[pathIndex]);
                }
            }

            castlePathQueues.Add(new Queue<List<PathNode>>(plannedCastlePaths));

            Debug.Log(
                "Wave plan from " + startNode.name +
                " -> " + option.Castle.name +
                " | quota = " + castleQuota +
                " | paths = " + option.Paths.Count +
                " | shortest = " + option.ShortestPathLength +
                " | longest = " + option.LongestPathLength
            );
        }

        bool addedPath = true;

        while (addedPath)
        {
            addedPath = false;

            foreach (Queue<List<PathNode>> castleQueue in castlePathQueues)
            {
                if (castleQueue.Count == 0)
                {
                    continue;
                }

                plannedPaths.Add(castleQueue.Dequeue());
                addedPath = true;
            }
        }

        plannedPathsByStart[startNode] = new Queue<List<PathNode>>(plannedPaths);
    }

    private List<CastlePathOptions> GetReachableCastlePathOptions(PathNode startNode)
    {
        List<CastlePathOptions> reachableOptions = new List<CastlePathOptions>();

        if (castleEnds == null || castleEnds.Length == 0)
        {
            Debug.LogWarning("EnemyPathAssignmentManager has no castleEnds assigned.");
            return reachableOptions;
        }

        foreach (CastleEndNode castle in castleEnds)
        {
            if (castle == null) continue;

            List<List<PathNode>> validPaths = GetValidPaths(startNode, castle);

            if (validPaths.Count == 0)
            {
                continue;
            }

            CastlePathOptions option = new CastlePathOptions(castle, validPaths);

            Debug.Log(
                "Reachable castle " + castle.name +
                " | valid paths = " + validPaths.Count +
                " | longest = " + option.LongestPathLength +
                " | shortest = " + option.ShortestPathLength
            );

            reachableOptions.Add(option);
        }

        return reachableOptions;
    }

    private List<List<PathNode>> GetValidPaths(PathNode startNode, CastleEndNode castle)
    {
        List<List<PathNode>> validPaths = GetCachedPaths(startNode, castle);
        validPaths.Sort((a, b) => b.Count.CompareTo(a.Count));
        return validPaths;
    }

    private List<List<PathNode>> GetCachedPaths(PathNode startNode, CastleEndNode castle)
    {
        InvalidateCacheIfNeeded();

        string key = startNode.GetInstanceID() + "_" + castle.GetInstanceID();

        if (!cachedPaths.ContainsKey(key))
        {
            cachedPaths[key] = Pathfinder.FindAllPaths(startNode, castle, MaxPathDepth, MaxPaths);
        }

        return cachedPaths[key];
    }

    private int GetPlannedEnemyCountForStartNode(PathNode startNode)
    {
        if (plannedEnemyCountsByStart.ContainsKey(startNode))
        {
            return Mathf.Max(1, plannedEnemyCountsByStart[startNode]);
        }

        return fallbackWaveEnemyCount;
    }

    private List<int> AllocateQuotas(int totalCount, List<float> weights, bool guaranteeOneIfPossible)
    {
        List<int> quotas = new List<int>();

        if (weights == null || weights.Count == 0)
        {
            return quotas;
        }

        for (int i = 0; i < weights.Count; i++)
        {
            quotas.Add(0);
        }

        if (totalCount <= 0)
        {
            return quotas;
        }

        int remainingCount = totalCount;

        if (guaranteeOneIfPossible && totalCount >= weights.Count)
        {
            for (int i = 0; i < quotas.Count; i++)
            {
                quotas[i] = 1;
            }

            remainingCount -= weights.Count;
        }

        if (remainingCount <= 0)
        {
            return quotas;
        }

        float totalWeight = 0f;

        foreach (float weight in weights)
        {
            totalWeight += Mathf.Max(0.01f, weight);
        }

        List<float> remainders = new List<float>();
        int assigned = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            float exactShare = remainingCount * (Mathf.Max(0.01f, weights[i]) / totalWeight);
            int wholeShare = Mathf.FloorToInt(exactShare);

            quotas[i] += wholeShare;
            assigned += wholeShare;
            remainders.Add(exactShare - wholeShare);
        }

        int leftovers = remainingCount - assigned;

        while (leftovers > 0)
        {
            int bestIndex = 0;
            float bestRemainder = float.MinValue;

            for (int i = 0; i < remainders.Count; i++)
            {
                if (remainders[i] > bestRemainder)
                {
                    bestRemainder = remainders[i];
                    bestIndex = i;
                }
            }

            quotas[bestIndex]++;
            remainders[bestIndex] = -1f;
            leftovers--;
        }

        return quotas;
    }

    private CastleEndNode GetPathCastle(List<PathNode> path)
    {
        if (path == null || path.Count == 0)
        {
            return null;
        }

        return path[path.Count - 1] as CastleEndNode;
    }

    private void InvalidateCacheIfNeeded()
    {
        if (cachedVersion != PathGraphState.Version)
        {
            cachedPaths.Clear();
            plannedPathsByStart.Clear();
            cachedVersion = PathGraphState.Version;
        }
    }

    private class CastlePathOptions
    {
        public CastleEndNode Castle { get; private set; }
        public List<List<PathNode>> Paths { get; private set; }
        public int LongestPathLength { get; private set; }
        public int ShortestPathLength { get; private set; }
        public float AveragePathLength { get; private set; }

        public CastlePathOptions(CastleEndNode castle, List<List<PathNode>> paths)
        {
            Castle = castle;
            Paths = paths;

            int totalLength = 0;

            LongestPathLength = paths[0].Count;
            ShortestPathLength = paths[paths.Count - 1].Count;

            foreach (List<PathNode> path in paths)
            {
                totalLength += path.Count;
            }

            AveragePathLength = (float)totalLength / paths.Count;
        }
    }
}
