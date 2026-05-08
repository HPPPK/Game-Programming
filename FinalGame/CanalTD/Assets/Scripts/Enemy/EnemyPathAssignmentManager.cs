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
 * 1. WaveManager calls ResetAssignments() at the start of a wave.
 * 2. EnemyMover calls GetPathToLeastAssignedReachableCastle(startNode).
 * 3. The manager finds castles that can currently be reached from startNode.
 * 4. It picks the reachable castle with the fewest assigned enemies.
 * 5. It chooses one path for that castle, preferring longer path groups first.
 * 6. It returns a List<PathNode> for EnemyMover to follow.
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

    private const int MaxPathDepth = 40;
    private const int MaxPaths = 50;

    [Header("Castle End Nodes")]
    public CastleEndNode[] castleEnds;

    private Dictionary<CastleEndNode, int> assignedCounts = new Dictionary<CastleEndNode, int>();
    private Dictionary<string, int> pathGroupCounts = new Dictionary<string, int>();
    private Dictionary<string, List<List<PathNode>>> cachedPaths = new Dictionary<string, List<List<PathNode>>>();
    private int cachedVersion = -1;

    void Awake()
    {
        Instance = this;
        ResetAssignments();
    }

    public void ResetAssignments()
    {
        assignedCounts.Clear();
        pathGroupCounts.Clear();

        foreach (CastleEndNode castle in castleEnds)
        {
            if (castle != null)
            {
                assignedCounts[castle] = 0;
            }
        }
    }

    public List<PathNode> GetPathToLeastAssignedReachableCastle(PathNode startNode)
    {
        if (startNode == null)
        {
            return null;
        }

        InvalidateCacheIfNeeded();

        CastleEndNode bestCastle = ChooseLeastAssignedReachableCastle(startNode);

        if (bestCastle == null)
        {
            Debug.LogWarning("No reachable castle from " + startNode.name);
            return null;
        }

        assignedCounts[bestCastle]++;

        List<PathNode> selectedPath = ChooseWeightedPathForCastle(startNode, bestCastle);

        if (selectedPath == null || selectedPath.Count == 0)
        {
            Debug.LogWarning("No cached path available from " + startNode.name + " to " + bestCastle.name);
            return null;
        }

        Debug.Log(
            "Assigned enemy to " + bestCastle.name +
            " | castle count = " + assignedCounts[bestCastle] +
            " | path length = " + selectedPath.Count
        );

        return selectedPath;
    }

    private List<CastleEndNode> GetReachableCastles(PathNode startNode)
    {
        List<CastleEndNode> reachableCastles = new List<CastleEndNode>();

        foreach (CastleEndNode castle in castleEnds)
        {
            if (castle == null) continue;

            List<List<PathNode>> validPaths = GetValidPaths(startNode, castle);

            if (validPaths.Count == 0)
            {
                continue;
            }

            int longest = validPaths[0].Count;
            int shortest = validPaths[validPaths.Count - 1].Count;

            Debug.Log(
                "Reachable castle " + castle.name +
                " | valid paths = " + validPaths.Count +
                " | longest = " + longest +
                " | shortest = " + shortest
            );

            reachableCastles.Add(castle);
        }

        return reachableCastles;
    }

    private CastleEndNode ChooseLeastAssignedReachableCastle(PathNode startNode)
    {
        List<CastleEndNode> reachableCastles = GetReachableCastles(startNode);

        CastleEndNode bestCastle = null;
        int bestCount = int.MaxValue;

        foreach (CastleEndNode castle in reachableCastles)
        {
            int count = assignedCounts.ContainsKey(castle) ? assignedCounts[castle] : 0;

            if (count < bestCount)
            {
                bestCount = count;
                bestCastle = castle;
            }
        }

        return bestCastle;
    }

    private bool IsCastleReachable(PathNode startNode, CastleEndNode castle)
    {
        return GetValidPaths(startNode, castle).Count > 0;
    }

    private List<PathNode> ChooseWeightedPathForCastle(PathNode startNode, CastleEndNode castle)
    {
        List<List<PathNode>> allPaths = GetValidPaths(startNode, castle);

        if (allPaths.Count == 0)
        {
            return null;
        }

        Dictionary<int, List<List<PathNode>>> lengthGroups = new Dictionary<int, List<List<PathNode>>>();

        foreach (List<PathNode> path in allPaths)
        {
            int length = path.Count;

            if (!lengthGroups.ContainsKey(length))
            {
                lengthGroups[length] = new List<List<PathNode>>();
            }

            lengthGroups[length].Add(path);
        }

        List<int> lengths = new List<int>(lengthGroups.Keys);
        lengths.Sort((a, b) => b.CompareTo(a)); // longest first

        float roll = Random.value;
        int selectedGroupIndex = SelectWeightedLengthGroupIndex(roll, lengths.Count);

        int selectedLength = lengths[selectedGroupIndex];
        List<List<PathNode>> group = lengthGroups[selectedLength];

        string groupKey = startNode.name + "_" + castle.name + "_len_" + selectedLength;

        if (!pathGroupCounts.ContainsKey(groupKey))
        {
            pathGroupCounts[groupKey] = 0;
        }

        int pathIndex = pathGroupCounts[groupKey] % group.Count;
        pathGroupCounts[groupKey]++;

        return group[pathIndex];
    }

    private int SelectWeightedLengthGroupIndex(float roll, int groupCount)
    {
        if (groupCount <= 1)
        {
            return 0;
        }

        if (roll < 0.60f)
        {
            return 0;
        }

        if (groupCount == 2)
        {
            return 1;
        }

        if (roll < 0.85f)
        {
            return 1;
        }

        if (groupCount == 3)
        {
            return 2;
        }

        if (roll < 0.95f)
        {
            return 2;
        }

        int remainingGroupCount = groupCount - 3;
        int remainingIndex = Random.Range(0, remainingGroupCount);
        return 3 + remainingIndex;
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

        string key = startNode.name + "_" + castle.name;

        if (!cachedPaths.ContainsKey(key))
        {
            cachedPaths[key] = Pathfinder.FindAllPaths(startNode, castle, MaxPathDepth, MaxPaths);
        }

        return cachedPaths[key];
    }

    private void InvalidateCacheIfNeeded()
    {
        if (cachedVersion != PathGraphState.Version)
        {
            cachedPaths.Clear();
            cachedVersion = PathGraphState.Version;
        }
    }
}
