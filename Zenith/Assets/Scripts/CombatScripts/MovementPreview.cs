using UnityEngine;
using System.Collections.Generic;

public class MovementPreview : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private GameObject pathPrefab;
    [SerializeField] private GameObject enemyHighlightPrefab;
    [SerializeField] private int maxRange = 3; // Default move range if not specified
    [SerializeField] private GameObject populateMap;

    private GridData gridData;

    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0)
    };

    private readonly List<GameObject> activeHighlights = new();
    private readonly List<GameObject> activePath = new();
    private HashSet<Vector3Int> reachableTiles = new();
    private HashSet<Vector3Int> attackableTiles = new();
    private Vector3Int startTilePos;
    private int startTeam = -1;

    void Start()
    {
        gridData = populateMap.GetComponent<PopulateMap>().objectsData;
    }

    public void ShowMovementRange(Vector3Int startPos, int moveRange = -1)
    {
        // print(moveRange);
        if (moveRange < 0)
            moveRange = maxRange;

        startTilePos = startPos;

        TileData startTile = gridData.GetTileAt(startPos);
        if (startTile?.PlacedObject is CharacterObject sc)
            startTeam = sc.Team;


        reachableTiles = BFSReachable(startPos, moveRange);

        foreach (var tile in reachableTiles)
            SpawnHighlight(tile, highlightPrefab);
    }

    public void ShowAttackableEnemies(Vector3Int startPos, int attackRange = 1)
    {
        startTilePos = startPos;
        TileData startTile = gridData.GetTileAt(startPos);
        if (startTile?.PlacedObject is not CharacterObject sc)
            return;

        startTeam = sc.Team;

        for (int x = -attackRange; x <= attackRange; x++)
        {
            for (int y = -attackRange; y <= attackRange; y++)
            {
                if (x == 0 && y == 0) continue;                
                if (Mathf.Abs(x) + Mathf.Abs(y) > attackRange) continue;   

                Vector3Int tilePos = startPos + new Vector3Int(x, y, 0);

                if (!gridData.IsWithinBounds(tilePos)) continue;

                TileData tile = gridData.GetTileAt(tilePos);
                if (tile?.PlacedObject is CharacterObject target && target.Team != startTeam)
                {
                    if (HasLineOfSight(startPos, tilePos))
                    {
                        attackableTiles.Add(tilePos);
                        SpawnHighlight(tilePos, enemyHighlightPrefab != null ? enemyHighlightPrefab : highlightPrefab);
                    }
                }
            }
        }
    }

    private bool HasLineOfSight(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> line = GetLine(start, end);

        for (int i = 1; i < line.Count - 1; i++)
        {
            TileData tile = gridData.GetTileAt(line[i]);
            if (tile == null)
                continue;

            if (tile.PlacedObject != null)
                return false;
        }

        return true;
    }
    
    private List<Vector3Int> GetLine(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> line = new();

        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector3Int(x0, y0, 0));
            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        return line;
    }
    
    public void ShowPathPreview(Vector3Int targetTile)
    {
        ClearPath();

        if (!reachableTiles.Contains(targetTile))
            return;

        List<Vector3Int> path = FindPathAStar(startTilePos, targetTile);

        if (path.Count == 0)
        {
            Debug.Log("No path found to target.");
            return;
        }

        foreach (var step in path)
            SpawnHighlight(step, pathPrefab, activePath);
    }

    public void ClearAll()
    {
        ClearHighlights();
        ClearPath();
        reachableTiles.Clear();
        attackableTiles.Clear();
    }

    private HashSet<Vector3Int> BFSReachable(Vector3Int startPos, int moveRange)
    {
        Queue<Vector3Int> frontier = new();
        Dictionary<Vector3Int, int> distance = new();
        HashSet<Vector3Int> visited = new();

        frontier.Enqueue(startPos);
        visited.Add(startPos);
        distance[startPos] = 0;

        TileData startTile = gridData.GetTileAt(startPos);

        if (startTile?.PlacedObject is CharacterObject startChar)
            startTeam = startChar.Team;
        
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            int currentDist = distance[current];

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;

                if (visited.Contains(next)) continue;
                if (currentDist + 1 > moveRange) continue;
                if (!gridData.IsWithinBounds(next)) continue;
                
                TileData tile = gridData.GetTileAt(next);
                PlacedObject placed = tile?.PlacedObject;

                if (placed == null)
                {
                    frontier.Enqueue(next);
                    visited.Add(next);
                    distance[next] = currentDist + 1;
                    continue;
                }

                if (placed.ObjectType == ObjectType.Static || placed.ObjectType == ObjectType.RandomProp)
                    continue;
                
                if (placed is CharacterObject) continue;
            }
        }

        visited.Remove(startPos);
        reachableTiles = visited;
        return visited;
    }

    private List<Vector3Int> FindPathAStar(Vector3Int start, Vector3Int goal)
    {
        PriorityQueue<Vector3Int> openSet = new();
        openSet.Enqueue(start, 0);

        Dictionary<Vector3Int, Vector3Int> cameFrom = new();
        Dictionary<Vector3Int, int> costSoFar = new();
        cameFrom[start] = start;
        costSoFar[start] = 0;

        while (openSet.Count > 0)
        {
            Vector3Int current = openSet.Dequeue();

            if (current == goal)
                break;

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;

                if (!reachableTiles.Contains(next)) continue;
                int newCost = costSoFar[current] + 1;

                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    int priority = newCost + Heuristic(next, goal);
                    openSet.Enqueue(next, priority);
                    cameFrom[next] = current;
                }
            }
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int start, Vector3Int goal)
    {
        List<Vector3Int> path = new();
        if (!cameFrom.ContainsKey(goal)) return path;

        Vector3Int current = goal;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    private int Heuristic(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void SpawnHighlight(Vector3Int gridPos, GameObject prefab, List<GameObject> list = null)
    {
        Vector3 worldPos = grid.CellToWorld(gridPos);
        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        if (list == null)
            activeHighlights.Add(obj);
        else
            list.Add(obj);
    }

    public bool IsTileReachable(Vector3Int tilePos)
    {
        return reachableTiles.Contains(tilePos);
    }

    public bool IsTileAttackable(Vector3Int tilePos)
    {
        return attackableTiles.Contains(tilePos);
    }

    public HashSet<Vector3Int> GetReachableTiles()
    {
        return reachableTiles;
    }

    public void ClearHighlights()
    {
        foreach (var h in activeHighlights)
        {
            if (h != null) Destroy(h);
        }
        activeHighlights.Clear();
    }

    private void ClearPath()
    {
        foreach (var p in activePath)
            if (p != null) Destroy(p);
        activePath.Clear();
    }
}

public class PriorityQueue<T>
{
    private readonly List<(T item, int priority)> elements = new();

    public int Count => elements.Count;

    public void Enqueue(T item, int priority)
    {
        elements.Add((item, priority));
    }

    public T Dequeue()
    {
        int bestIndex = 0;
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].priority < elements[bestIndex].priority)
                bestIndex = i;
        }

        T bestItem = elements[bestIndex].item;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}
