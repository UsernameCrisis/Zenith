using System.Collections.Generic;
using UnityEngine;

public class PopulateMap : MonoBehaviour
{
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Grid grid;
    [SerializeField] private bool loadFromSave = true;
    [SerializeField] private bool forTrainingAgent = false;
    [SerializeField] private bool useMaxFlow = false;
    [SerializeField] private int minTraversablePaths = 3;
    [SerializeField] private float obstacleDensity = 0.1f;
    [SerializeField] private int obstacleID = 7;
    [SerializeField] private int teamDist = 6;
    private int minX, maxX, minY, maxY, width, height;
    private Vector3Int team1Center, team2Center;

    public GridData objectsData;
    public List<GameObject> placedGameObjects = new();

    void Awake()
    {
        objectsData = new();
    }
    void Start()
    {
        if (loadFromSave)
        {
            PopulateFromGridJSON();
        }
        else if (forTrainingAgent == true)
        {
            PopulateFromGridJSON();
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
        //Mungkin lebih bagus kalau disuruh coba spawn sampai targetobstacle count tercapai?

        int attempts = 150;

        for (int i = 0; i < attempts; i++)
        {
            if (Random.value > obstacleDensity)
                continue;

            Vector3Int pos = GetRandomEmptyTile();

            if (!IsInsideBounds(pos))
                continue;
            
            if (pos == team1Center || pos == team2Center)
                continue;

            PlaceObject(pos, obstacleID, placedGameObjects.Count - 1);

            int pathCount = useMaxFlow ? MaxFlow(team1Center, team2Center, width, height) : CountPaths(team1Center, team2Center);

            Debug.Log("Path count: " + pathCount);
            if (pathCount < minTraversablePaths)
            {
                objectsData.RemoveObjectAt(pos);

                Destroy(placedGameObjects[^1]);
                placedGameObjects.RemoveAt(placedGameObjects.Count - 1);
            }
        }
    }

    private int MaxFlow(Vector3Int start, Vector3Int goal, int width, int height)
    {
        Debug.Log("Using Max Flow");
        int n = width * height;
        int[,] capacity = new int[n, n];

        Vector3Int[] dirs =
        {
            Vector3Int.up,
            Vector3Int.down,
            Vector3Int.left,
            Vector3Int.right
        };

        Shuffle(dirs);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x + minX, y + minY, 0);

                if (!IsWalkable(pos))
                    continue;

                int u = NodeIndex(x, y, width);

                foreach (var d in dirs)
                {
                    Vector3Int npos = pos + d;

                    if (!IsWalkable(npos))
                        continue;

                    int nx = npos.x - minX;
                    int ny = npos.y - minY;

                    int v = NodeIndex(nx, ny, width);

                    capacity[u, v] = 1;
                }
            }

        int source = NodeIndex(start.x - minX, start.y - minY, width);
        int sink = NodeIndex(goal.x - minX, goal.y - minY, width);

        int flow = 0;

        while (true)
        {
            //BFS
            int[] parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = -1;

            Queue<int> q = new();
            q.Enqueue(source);
            parent[source] = source;

            while (q.Count > 0 && parent[sink] == -1)
            {
                int u = q.Dequeue();

                for (int v = 0; v < n; v++)
                {
                    if (parent[v] == -1 && capacity[u, v] > 0)
                    {
                        parent[v] = u;
                        q.Enqueue(v);
                    }
                }
            }

            if (parent[sink] == -1)
                break;

            int vtx = sink;

            while (vtx != source)
            {
                int u = parent[vtx];
                capacity[u, vtx]--;
                capacity[vtx, u]++;
                vtx = u;
            }

            flow++;
        }

        return flow;
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
        Debug.Log("Using BFS");
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

        do
        {
            team2Center = GetRandomEmptyTile();
        }
        while (Vector3Int.Distance(team1Center, team2Center) < teamDist);

        SpawnTeam(team1Center, 4, 6); // team 1 IDs
        SpawnTeam(team2Center, 1, 3); // team 2 IDs
    }

    private void SpawnTeam(Vector3Int center, int minID, int maxID)
    {
        int units = 3;
        int attempts = 0;

        while (units > 0 && attempts < 50)
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

        return Vector3Int.zero;
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

        height = map.grid.Length;
        width = map.grid[0].row.Length;

        int offsetX = width / 2;
        int offsetY = height / 2;

        minX = -offsetX; maxX = offsetX - 1; minY = -offsetY; maxY = offsetY - 1;

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
        GameObject newObject = Instantiate(data.Prefab);
        newObject.transform.position = grid.CellToWorld(gridPos);
        placedGameObjects.Add(newObject);
        PlacedObject placedObj = CreatePlacedObjectFromData(data);

        if (placedObj is CharacterObject character)
        {
            character.OnDied += HandleCharacterDeath;
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

    private void HandleCharacterDeath(CharacterObject character)
    {
        Vector3Int? pos = objectsData.GetPositionOf(character);
        if (pos == null)
            return;

        TileData tile = objectsData.GetTileAt(pos.Value);
    
        if (tile.PlacedGameObject != null)
            Destroy(tile.PlacedGameObject);

        // Remove from grid data
        objectsData.RemoveObjectAt(pos.Value);
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
                return new CharacterObject(data.Name, 100, data.Damage, data.Defense, data.Speed, 
                                            data.currentATB, data.Portrait, data.Team, data.AtkRange, 
                                            data.IsPlayer);
            default:
                return new StaticObject(data.Name);
        }
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
