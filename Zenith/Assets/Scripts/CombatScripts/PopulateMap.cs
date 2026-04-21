using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PopulateMap : MonoBehaviour
{
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Transform spawnedObjectContainer;
    [SerializeField] private Grid grid;
    [SerializeField] private bool loadFromSave = true;
    [SerializeField] private bool forTrainingAgent = false;
    [SerializeField] private bool useMaxFlow = false;
    [SerializeField] private bool isFullyRandom = false;
    [SerializeField] private int minTraversablePaths = 2;
    [SerializeField] private float obstacleDensity = 0.15f;
    [SerializeField] private int clusterLimit = 2;
    [SerializeField] private int obstacleID = 7;
    [SerializeField] private int teamDist = 6;
    [SerializeField] private int width, height;
    private int minX, maxX, minY, maxY, offsetX, offsetY;
    private Vector3Int team1Center, team2Center;
    private TurnManager turnManager;

    public GridData objectsData;
    public List<GameObject> placedGameObjects = new();

    void Awake()
    {
        objectsData = new();
        
        offsetX = width / 2;
        offsetY = height / 2;

        minX = -offsetX; maxX = offsetX - 1; minY = -offsetY; maxY = offsetY - 1;
    }
    void Start()
    {
        turnManager = GetComponentInParent<TurnManager>();
    }

    public void Generate()
    {
        ClearMap();

        if (loadFromSave)
        {
            PopulateFromGridJSON();
        }
        else if (forTrainingAgent)
        {
            SpawnTeams();
            SpawnRandomObstacles();
        }
        else
        {
            PopulateManually();
        }
    }

    private void SpawnRandomObstacles()
    {
        int totalTiles = width * height;
        int targetObstacles = Mathf.RoundToInt(totalTiles * obstacleDensity);
        int placed = 0;
        int attempts = totalTiles * 3;

        for (int i = 0; i < attempts && placed < targetObstacles; i++)
        {
            Vector3Int pos = GetRandomEmptyTile();

            if (!IsInsideBounds(pos))
                continue;

            if (objectsData.GetTileAt(pos) != null)
                continue;

            if (Vector3Int.Distance(pos, team1Center) <= 2 || Vector3Int.Distance(pos, team2Center) <= 2)
                continue;

            if (isFullyRandom)
            {
                int neighborObstacles = CountObstacleNeighbors(pos);
                if (neighborObstacles > clusterLimit)
                    continue;
            }

            PlaceObject(pos, obstacleID, placedGameObjects.Count - 1);
            int pathCount = useMaxFlow ? MaxFlow(team1Center, team2Center, width, height)
                                            : CountPaths(team1Center, team2Center);
            
            if (pathCount < minTraversablePaths)
            {
                objectsData.RemoveObjectAt(pos);

                Destroy(placedGameObjects[^1]);
                placedGameObjects.RemoveAt(placedGameObjects.Count - 1);
                continue;
            }
            placed++;
        }
    }

    private int MaxFlow(Vector3Int start, Vector3Int goal, int width, int height)
    {
        int n = width * height;
        int totalNodes = 2 * n; 

        Dictionary<int, Dictionary<int, int>> cap = new();

        void AddEdge(int u, int v, int c)
        {
            if (!cap.ContainsKey(u)) cap[u] = new Dictionary<int, int>();
            if (!cap.ContainsKey(v)) cap[v] = new Dictionary<int, int>();
            cap[u][v] = cap[u].GetValueOrDefault(v) + c;
            if (!cap[v].ContainsKey(u)) cap[v][u] = 0; // reverse edge starts at 0
        }

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x + minX, y + minY, 0);
                if (!IsWalkable(pos)) continue;

                int idx = NodeIndex(x, y, width);
                int inNode  = idx;
                int outNode = idx + n;

                bool isEndpoint = pos == start || pos == goal;
                AddEdge(inNode, outNode, isEndpoint ? int.MaxValue : 1);

                foreach (var d in dirs)
                {
                    Vector3Int npos = pos + d;
                    if (!IsWalkable(npos)) continue;

                    int nx = npos.x - minX;
                    int ny = npos.y - minY;
                    int nIdx = NodeIndex(nx, ny, width);

                    AddEdge(outNode, nIdx, 1); // OUT(current) → IN(neighbour), capacity 1
                }
            }
        }

        int srcIdx  = NodeIndex(start.x - minX, start.y - minY, width);
        int sinkIdx = NodeIndex(goal.x  - minX, goal.y  - minY, width);

        int source = srcIdx;
        int sink   = sinkIdx + n;

        if (source == sink) return int.MaxValue;

        int flow = 0;

        // Edmonds-Karp (BFS augmenting paths)
        while (true)
        {
            // BFS to find augmenting path
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

            if (!parent.ContainsKey(sink))
                break; // no augmenting path

            // Find bottleneck capacity along path
            int pathFlow = int.MaxValue;
            int cur = sink;
            while (cur != source)
            {
                int prev = parent[cur];
                pathFlow = Mathf.Min(pathFlow, cap[prev][cur]);
                cur = prev;
            }

            // Update capacities along path
            cur = sink;
            while (cur != source)
            {
                int prev = parent[cur];
                cap[prev][cur] -= pathFlow;
                cap[cur][prev] = cap[cur].GetValueOrDefault(prev) + pathFlow;
                cur = prev;
            }

            flow += pathFlow;

            if (flow >= minTraversablePaths)
                return flow;
        }

        return flow;
    }

    private int CountObstacleNeighbors(Vector3Int pos)
    {
        int count = 0;

        Vector3Int[] dirs =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        foreach (var d in dirs)
        {
            var neighbor = pos + d;

            if (!IsInsideBounds(neighbor))
                continue;

            var tile = objectsData.GetTileAt(neighbor);

            if (tile != null && (tile.PlacedObject is StaticObject || tile.PlacedObject is RandomObject))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsWalkable(Vector3Int pos)
    {
        if (!IsInsideBounds(pos))
            return false;

        var tile = objectsData.GetTileAt(pos);

        if (tile == null)
            return true;

        return !(tile.PlacedObject is StaticObject || tile.PlacedObject is RandomObject);
    }

    private int NodeIndex(int x, int y, int width)
    {
        return y * width + x;
    }

    private int CountPaths(Vector3Int start, Vector3Int goal)
    {
        HashSet<Vector3Int> blocked = new();
        int paths = 0;

        while (true)
        {
            var path = FindPath(start, goal, blocked);

            if (path == null)
                break;

            paths++;

            for (int i = 1; i < path.Count - 1; i++)
            {
                blocked.Add(path[i]);
            }

            if (paths >= minTraversablePaths)
                break;
        }

        return paths;
    }

    private List<Vector3Int> FindPath(Vector3Int start, Vector3Int goal, HashSet<Vector3Int> blocked)
    {
        Queue<Vector3Int> q = new();
        Dictionary<Vector3Int, Vector3Int> parent = new();

        Vector3Int[] dirs =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        Shuffle(dirs);

        q.Enqueue(start);
        parent[start] = start;

        while (q.Count > 0)
        {
            var pos = q.Dequeue();

            if (pos == goal)
                break;

            foreach (var d in dirs)
            {
                var next = pos + d;

                if (!IsWalkable(next))
                    continue;

                if (blocked.Contains(next))
                    continue;

                if (parent.ContainsKey(next))
                    continue;

                parent[next] = pos;
                q.Enqueue(next);
            }
        }

        if (!parent.ContainsKey(goal))
            return null;

        List<Vector3Int> path = new();
        var cur = goal;

        while (cur != start)
        {
            path.Add(cur);
            cur = parent[cur];
        }

        path.Add(start);

        return path;
    }

    private void Shuffle(Vector3Int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private void SpawnTeams()
    {
        team1Center = GetRandomEmptyTile();
        if (team1Center.x == int.MinValue)
        {
            Debug.LogError("SpawnTeams: could not find empty tile for team 1!");
            return;
        }
    
        int maxAttempts = 200;
        int attempts = 0;
        do
        {
            team2Center = GetRandomEmptyTile();
            attempts++;

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("SpawnTeams: could not satisfy team distance, using closest available.");
                break;
            }
        }
        while (team2Center.x != int.MinValue && Vector3Int.Distance(team1Center, team2Center) < teamDist);
        if (team2Center.x == int.MinValue)
        {
            Debug.LogError("SpawnTeams: could not find empty tile for team 2!");
            return;
        }

        SpawnTeam(team1Center, 4, 6); // team 1 IDs
        SpawnTeam(team2Center, 1, 3); // team 2 IDs
    }

    private void SpawnTeam(Vector3Int center, int minID, int maxID)
    {
        int units = 3;
        int attempts = 0;
        int maxAttempts = 200;

        while (units > 0 && attempts < maxAttempts)
        {
            attempts++;

            int dx = Random.Range(-1, 2);
            int dy = Random.Range(-1, 2);

            Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, 0);

            if (!IsInsideBounds(pos))
                continue;

            if (objectsData.GetTileAt(pos) != null)
                continue;

            int id = Random.Range(minID, maxID + 1);

            PlaceObject(pos, id, placedGameObjects.Count - 1);

            units--;
        }
        if (units > 0)
            Debug.LogWarning($"SpawnTeam: only spawned {3 - units}/3 units after {maxAttempts} attempts.");
    }

    private Vector3Int GetRandomEmptyTile()
    {
        for (int i = 0; i < 100; i++)
        {
            int x = Random.Range(minX, maxX);
            int y = Random.Range(minY, maxY);

            Vector3Int pos = new Vector3Int(x, y, 0);

            if (!IsInsideBounds(pos))
                continue;

            if (objectsData.GetTileAt(pos) == null)
                return pos;
        }

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (objectsData.GetTileAt(pos) == null)
                    return pos;
            }
        Debug.LogError("GetRandomEmptyTile: no empty tile found!");
        return new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    private void PopulateManually()
    {
        PlaceObject(new Vector3Int(-3, 1, 0), 0, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(2, -1, 0), 1, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, -2, 0), 2, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, -3, 0), 3, placedGameObjects.Count - 1);

        //Obstacle
        PlaceObject(new Vector3Int(0, 0, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(0, -1, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, -1, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, 0, 0), 7, placedGameObjects.Count - 1);

        PlaceObject(new Vector3Int(3, 3, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(3, 2, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(2, 3, 0), 7, placedGameObjects.Count - 1);

        PlaceObject(new Vector3Int(-4, 3, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-3, 3, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-4, 2, 0), 7, placedGameObjects.Count - 1);

        PlaceObject(new Vector3Int(3, -4, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(3, -3, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(2, -4, 0), 7, placedGameObjects.Count - 1);
        
        PlaceObject(new Vector3Int(-4, -4, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-4, -3, 0), 7, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-3, -4, 0), 7, placedGameObjects.Count - 1);
    }
    
    private void PopulateFromGridJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("maps/map_1"); // ($"maps/map_{Random.Range(0,5)}");

        if (jsonFile == null)
        {
            Debug.LogError("Map JSON not found!");
            return;
        }
        
        MapGrid map = JsonUtility.FromJson<MapGrid>(jsonFile.text);

        if (map == null || map.grid == null)
        {
            Debug.LogError("Map JSON failed to parse!");
            return;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int id = map.grid[y].row[x];

                if (id < 0) 
                    continue; // empty tile

                Vector3Int pos = new Vector3Int(x - offsetX, offsetY - y - 1, 0);

                PlaceObject(pos, id, placedGameObjects.Count - 1);
            }
        }
    }

    private void PlaceObject(Vector3Int gridPos, int ID, int placedObjectIndex)
    {
        ObjectData data = database.objectsData.Find(d => d.ID == ID);
        // if (ID == 0)
        // {
        //     data.setDamage(GameManager.Instance.playerAtk);
        //     data.setDefense(GameManager.Instance.playerMaxHP);
        // }
        GameObject newObject = Instantiate(data.Prefab, spawnedObjectContainer);
        newObject.transform.position = grid.CellToWorld(gridPos);
        placedGameObjects.Add(newObject);
        PlacedObject placedObj = CreatePlacedObjectFromData(data);

        if (placedObj is CharacterObject character)
        {
            CharacterView view = newObject.GetComponent<CharacterView>();
            if (view != null)
            {
                view.Bind(character);
            }
            else
            {
                Debug.LogWarning($"{newObject.name} has no CharacterView!");
            }
        }

        objectsData.AddObjectAt(gridPos, placedObj, placedObjectIndex, newObject);
    }

    public void HandleCharacterDeath(CharacterObject character)
    {
        Vector3Int? pos = objectsData.GetPositionOf(character);
        if (pos == null)
            return;

        TileData tile = objectsData.GetTileAt(pos.Value);
        
        if (!forTrainingAgent)
        {
            HandleDeathPlayerOnly(character, tile);
        }

        if (tile.PlacedGameObject != null)
            Destroy(tile.PlacedGameObject);

        // Remove from grid data
        objectsData.RemoveObjectAt(pos.Value);
    }
    
    private void HandleDeathPlayerOnly(CharacterObject character, TileData tile)
    {
        if (tile.PlacedGameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene("Tavern");
        } 
        else if (tile.PlacedGameObject.CompareTag("Enemy"))
        {
            turnManager.defeatedEnemyNames.Add(tile.PlacedGameObject.name); 
        } 
        else if (tile.PlacedGameObject.CompareTag("Allies"))
        {
            // Do something for allies
        }
    }

    private PlacedObject CreatePlacedObjectFromData(ObjectData data)
    {
        switch (data.Type)
        {
            case ObjectType.Static:
                return new StaticObject(data.Name);
            case ObjectType.RandomProp:
                return new RandomObject(data.Name);
            case ObjectType.Character:
                return new CharacterObject(data.Name, data.ID, data.HP, data.Damage, data.Defense, data.Speed, 
                                            data.currentATB, data.Portrait, data.Team, data.AtkRange, 
                                            data.IsPlayer);
            default:
                return new StaticObject(data.Name);
        }
    }
    
    public void ClearMap()
    {
        // Destroy all spawned GameObjects
        foreach (var obj in placedGameObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        placedGameObjects.Clear();
        objectsData.Clear();
    }

    private bool IsInsideBounds(Vector3Int pos)
    {
        return pos.x >= minX && pos.x <= maxX &&
               pos.y >= minY && pos.y <= maxY;
    }
}

[System.Serializable]
public class GridRow
{
    public int[] row;
}

[System.Serializable]
public class MapGrid
{
    public GridRow[] grid;
}
