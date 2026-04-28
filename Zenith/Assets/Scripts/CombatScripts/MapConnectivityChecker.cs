using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class MapConnectivityChecker
{
    private readonly Func<Vector3Int, bool> isWalkable;
    private readonly int width;
    private readonly int height;
    private readonly int minX;
    private readonly int minY;
    private readonly int minTraversablePaths;

    public MapConnectivityChecker(
        Func<Vector3Int, bool> isWalkable,
        int width, int height,
        int minX, int minY,
        int minTraversablePaths)
    {
        this.isWalkable = isWalkable;
        this.width = width;
        this.height = height;
        this.minX = minX;
        this.minY = minY;
        this.minTraversablePaths = minTraversablePaths;
    }

    public int MaxFlow(Vector3Int start, Vector3Int goal)
    {
        int n = width * height;
        Dictionary<int, Dictionary<int, int>> cap = new();

        void AddEdge(int u, int v, int c)
        {
            if (!cap.ContainsKey(u)) cap[u] = new();
            if (!cap.ContainsKey(v)) cap[v] = new();
            cap[u][v] = cap[u].GetValueOrDefault(v) + c;
            if (!cap[v].ContainsKey(u)) cap[v][u] = 0; // reverse edge starts at 0
        }

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        // Build the node-split flow graph: each walkable tile becomes
        // inNode -> outNode with capacity 1, except endpoints.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x + minX, y + minY, 0);
                if (!isWalkable(pos)) continue;

                int idx     = NodeIndex(x, y);
                int inNode  = idx;
                int outNode = idx + n;

                bool isEndpoint = (pos == start || pos == goal);
                AddEdge(inNode, outNode, isEndpoint ? int.MaxValue : 1);

                foreach (var d in dirs)
                {
                    Vector3Int npos = pos + d;
                    if (!isWalkable(npos)) continue;

                    int nIdx = NodeIndex(npos.x - minX, npos.y - minY);
                    AddEdge(outNode, nIdx, 1); // OUT(current) → IN(neighbour)
                }
            }
        }

        int source = NodeIndex(start.x - minX, start.y - minY);
        int sink   = NodeIndex(goal.x  - minX, goal.y  - minY) + n;

        if (source == sink) return int.MaxValue;

        int flow = 0;

        // Edmonds-Karp: find the shortest augmenting path via BFS, push flow,
        // repeat until no augmenting path exists.
        while (true)
        {
            Dictionary<int, int> parent = new();
            Queue<int> q = new();
            q.Enqueue(source);
            parent[source] = source;

            while (q.Count > 0 && !parent.ContainsKey(sink))
            {
                int u = q.Dequeue();
                if (!cap.ContainsKey(u)) continue;

                foreach (var kvp in cap[u])
                {
                    int v = kvp.Key;
                    if (!parent.ContainsKey(v) && kvp.Value > 0)
                    {
                        parent[v] = u;
                        q.Enqueue(v);
                    }
                }
            }

            if (!parent.ContainsKey(sink)) break;

            // Walk back from sink to source to find the bottleneck.
            int pathFlow = int.MaxValue;
            int cur = sink;
            while (cur != source)
            {
                int prev = parent[cur];
                pathFlow = Mathf.Min(pathFlow, cap[prev][cur]);
                cur = prev;
            }

            // Update forward and residual capacities along the path.
            cur = sink;
            while (cur != source)
            {
                int prev = parent[cur];
                cap[prev][cur] -= pathFlow;
                cap[cur][prev] = cap[cur].GetValueOrDefault(prev) + pathFlow;
                cur = prev;
            }

            flow += pathFlow;

            // Early exit — no need to find more paths than required.
            if (flow >= minTraversablePaths) return flow;
        }

        return flow;
    }

    // Counts the number of paths between start and goal by finding one path,
    // blocking its interior tiles, then searching again. This is cheaper than
    // MaxFlow but less precise — the paths it finds are not strictly
    // vertex-disjoint because BFS may still choose overlapping routes.
    //
    // Use this as the fast option when approximate connectivity is acceptable.
    public int CountPaths(Vector3Int start, Vector3Int goal)
    {
        HashSet<Vector3Int> blocked = new();
        int paths = 0;

        while (true)
        {
            List<Vector3Int> path = FindPath(start, goal, blocked);
            if (path == null) break;

            paths++;

            // Block interior tiles so the next search must use a different route.
            for (int i = 1; i < path.Count - 1; i++)
                blocked.Add(path[i]);

            if (paths >= minTraversablePaths) break;
        }

        return paths;
    }

    // BFS pathfinder that respects an additional set of blocked tiles on top
    // of the normal walkability check. Returns null if no path exists.
    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, HashSet<Vector3Int> blocked)
    {
        Queue<Vector3Int> q = new();
        Dictionary<Vector3Int, Vector3Int> parent = new();

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        ShuffleDirs(dirs); // randomise order so repeated calls find varied routes

        q.Enqueue(start);
        parent[start] = start;

        while (q.Count > 0)
        {
            var pos = q.Dequeue();
            if (pos == goal) break;

            foreach (var d in dirs)
            {
                var next = pos + d;
                if (!isWalkable(next)) continue;
                if (blocked.Contains(next)) continue;
                if (parent.ContainsKey(next)) continue;

                parent[next] = pos;
                q.Enqueue(next);
            }
        }

        if (!parent.ContainsKey(goal)) return null;

        // Reconstruct path from goal back to start, then reverse.
        List<Vector3Int> path = new();
        var cur = goal;
        while (cur != start)
        {
            path.Add(cur);
            cur = parent[cur];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    // Converts a 2D grid coordinate to a flat index for the flow graph.
    private int NodeIndex(int x, int y) => y * width + x;

    // Fisher-Yates shuffle for the direction array so BFS explores in a
    // random order each call, preventing CountPaths from always finding
    // the same two overlapping paths.
    private static void ShuffleDirs(Vector3Int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
