using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class StatVarianceConfig
{
    public int unitID;

    [Header("HP range")]
    public int minHP;
    public int maxHP;

    [Header("Damage range")]
    public int minDamage;
    public int maxDamage;

    [Header("Defense range")]
    public int minDefense;
    public int maxDefense;
}

public class PopulateMap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Transform spawnedObjectContainer;
    [SerializeField] private Grid grid;

    [Header("Generation mode")]
    [SerializeField] private bool loadFromSave = true;
    [SerializeField] private bool forTrainingAgent = false;

    [Header("Multi-Agent Pool settings (requires forTrainingAgent = true)")]
    [SerializeField] private bool forMultiAgent = false;
    [SerializeField] private int poolSizePerType = 3;
    [SerializeField] private List<int> monsterTeamIDs = new() { 3, 4, 5 };

    [Header("Training generation settings")]
    [SerializeField] private bool useMaxFlow = false;
    [SerializeField] private bool isFullyRandom = false;
    [SerializeField] private int minTraversablePaths = 2;
    [SerializeField] private float obstacleDensity = 0.15f;
    [SerializeField] private int clusterLimit = 2;
    [SerializeField] private int obstacleID = 6;
    [SerializeField] private int teamDist = 6;
    [SerializeField] private int width, height;
    [SerializeField] private bool isStaticComposition = false;

    [Header("Player team stat variance (training only)")]
    [SerializeField] private List<StatVarianceConfig> playerTeamVariance = new();

    private int minX, maxX, minY, maxY, offsetX, offsetY;
    private TurnManager turnManager;

    public GridData objectsData;
    public List<GameObject> placedGameObjects = new();

    private Dictionary<int, List<GameObject>> unitPool = new();
    private Dictionary<int, int> poolNextIndex = new();
    private List<GameObject> activePooledObjects = new();

    void Awake()
    {
        objectsData = new();
        
        offsetX = width / 2;
        offsetY = height / 2;

        minX = -offsetX; maxX = offsetX - 1; minY = -offsetY; maxY = offsetY - 1;
        turnManager = GetComponentInParent<TurnManager>();
    }
    void Start()
    {
        if (forMultiAgent)
            InitialisePool();
    }

    private void InitialisePool()
    {
        foreach (int id in monsterTeamIDs)
        {
            ObjectData data = database.objectsData.Find(d => d.ID == id);
            if (data == null)
            {
                Debug.LogError($"InitialisePool: no ObjectData found for ID {id}");
                continue;
            }

            List<GameObject> pool = new List<GameObject>();

            for (int i = 0; i < poolSizePerType; i++)
            {
                GameObject go = Instantiate(data.Prefab, spawnedObjectContainer);
                go.SetActive(false);
                pool.Add(go);
            }

            unitPool[id] = pool;
            poolNextIndex[id] = 0;
        }

        Debug.Log($"[PopulateMap] Pool initialised: {monsterTeamIDs.Count} types × {poolSizePerType} = " +
                  $"{monsterTeamIDs.Count * poolSizePerType} pooled GameObjects.");
    }

    public void Generate()
    {
        ClearMap();

        if (forMultiAgent)
        {
            foreach (int id in monsterTeamIDs)
                poolNextIndex[id] = 0;
            activePooledObjects.Clear();
        }

        if (loadFromSave)
            PopulateFromGridJSON();
        else if (forTrainingAgent)
        {
            GenerateTrainingMap();
        }
        else
            PopulateManually();
    }

    public void ClearMap()
    {
        if (forMultiAgent)
        {
            foreach (var go in activePooledObjects)
            {
                if (go == null) continue;
 
                SetUnitVisualsActive(go, false);
                go.SetActive(false);
            }
            activePooledObjects.Clear();
 
            foreach (var go in placedGameObjects)
            {
                if (go == null) continue;
                if (IsPooledObject(go)) continue;
                Destroy(go);
            }
        }
        else
        {
            foreach (var obj in placedGameObjects)
                if (obj != null) Destroy(obj);
        }
        placedGameObjects.Clear();
        objectsData.Clear();
    }

    public void HandleCharacterDeath(CharacterObject character, System.Action onRemoved = null)
    {
        Vector3Int? pos = objectsData.GetPositionOf(character);
        if (pos == null) return;

        TileData tile = objectsData.GetTileAt(pos.Value);

        var view = character.View;

        if (forMultiAgent && IsMonsterTeamCharacter(character))
        {
            if (view != null && turnManager.GetUseAnimation())
                StartCoroutine(HandleDeathRoutineMultiAgent(character, pos.Value, tile, view, onRemoved));
            else
                HandleDeathInstantMultiAgent(character, pos.Value, tile, onRemoved);

            return;
        }

        if (view != null && turnManager.GetUseAnimation())
        {
            StartCoroutine(HandleDeathRoutine(character, pos.Value, tile, view, onRemoved));
        }
        else
        {
            if (!forTrainingAgent)
                HandleDeathPlayerOnly(character, tile);

            if (tile.PlacedGameObject != null)
                Destroy(tile.PlacedGameObject);

            objectsData.RemoveObjectAt(pos.Value);
            onRemoved?.Invoke();
        }
    }

    private IEnumerator HandleDeathRoutineMultiAgent(
        CharacterObject character, Vector3Int pos, TileData tile, UnitView view,
        System.Action onRemoved)
    {
        bool finished = false;
        void OnFinished() => finished = true;
        view.OnDeathFinished += OnFinished;

        // Death animation plays normally, the visual dies, the agent survives.
        yield return new WaitUntil(() => finished);
        view.OnDeathFinished -= OnFinished;

        FinaliseMultiAgentDeath(character, pos, tile, onRemoved);
    }

    private void HandleDeathInstantMultiAgent(
        CharacterObject character, Vector3Int pos, TileData tile,
        System.Action onRemoved)
    {
        FinaliseMultiAgentDeath(character, pos, tile, onRemoved);
    }

    private void FinaliseMultiAgentDeath(
        CharacterObject character, Vector3Int pos, TileData tile,
        System.Action onRemoved)
    {
        if (tile.PlacedGameObject != null)
            SetUnitVisualsActive(tile.PlacedGameObject, false);

        objectsData.RemoveObjectAt(pos);

        UnitAgentBase agent = tile.PlacedGameObject?.GetComponent<UnitAgentBase>();
        agent?.NotifyUnitDied();

        onRemoved?.Invoke();
    }

    private IEnumerator HandleDeathRoutine(CharacterObject character, Vector3Int pos, TileData tile, UnitView view, System.Action onRemoved)
    {
        bool finished = false;
    
        void OnFinished() => finished = true;
    
        view.OnDeathFinished += OnFinished;

        // Death animation triggered from OnDied inside UnitView
    
        yield return new WaitUntil(() => finished);
    
        view.OnDeathFinished -= OnFinished;
    
        if (!forTrainingAgent)
            HandleDeathPlayerOnly(character, tile);
    
        if (tile.PlacedGameObject != null)
            Destroy(tile.PlacedGameObject);
    
        objectsData.RemoveObjectAt(pos);
        onRemoved?.Invoke();
    }

    private void GenerateTrainingMap()
    {
        var generator = new TrainingMapGenerator(
            isStaticComposition,
            width, height,
            minX, maxX, minY, maxY,
            teamDist, obstacleID,
            obstacleDensity, clusterLimit,
            minTraversablePaths,
            useMaxFlow, isFullyRandom,
            isOccupied: pos => objectsData.GetTileAt(pos) != null,
            isWalkable: IsWalkable,
            placeObject: (pos, id) =>
            {
                if (IsPlayerTeamID(id) && forTrainingAgent)
                    PlaceObjectWithVariance(pos, id);
                else
                    PlaceObject(pos, id);
            },
            removeLastObject: pos =>
            {
                if (forMultiAgent)
                {
                    TileData tile = objectsData.GetTileAt(pos);
                    if (tile?.PlacedGameObject != null && IsPooledObject(tile.PlacedGameObject))
                    {
                        SetUnitVisualsActive(tile.PlacedGameObject, false);
                        tile.PlacedGameObject.SetActive(false);
                        activePooledObjects.Remove(tile.PlacedGameObject);

                        foreach (var kvp in unitPool)
                        {
                            int idx = kvp.Value.IndexOf(tile.PlacedGameObject);
                            if (idx >= 0)
                            {
                                poolNextIndex[kvp.Key] = Mathf.Max(0, poolNextIndex[kvp.Key] - 1);
                                break;
                            }
                        }
                    }
                    else
                    {
                        Destroy(placedGameObjects[^1]);
                    }
                }
                else
                {
                    Destroy(placedGameObjects[^1]);
                }
                objectsData.RemoveObjectAt(pos);
                placedGameObjects.RemoveAt(placedGameObjects.Count - 1);
            });

        generator.Generate();
    }

    private bool IsPlayerTeamID(int id) => id >= 0 && id <= 2;

    private void PlaceObjectWithVariance(Vector3Int gridPos, int id)
    {
        ObjectData data = database.objectsData.Find(d => d.ID == id);
        if (data == null)
        {
            Debug.LogError($"PlaceObjectWithVariance: no ObjectData found for ID {id}");
            return;
        }

        StatVarianceConfig config = playerTeamVariance.Find(c => c.unitID == id);

        if (config != null)
        {
            int origHP      = data.HP;
            int origDamage  = data.Damage;
            int origDefense = data.Defense;

            data.setHP(Random.Range(config.minHP, config.maxHP + 1));
            data.setDamage(Random.Range(config.minDamage, config.maxDamage + 1));
            data.setDefense(Random.Range(config.minDefense, config.maxDefense + 1));

            PlaceObject(gridPos, id);

            data.setHP(origHP);
            data.setDamage(origDamage);
            data.setDefense(origDefense);
        }
        else
            PlaceObject(gridPos, id);
    }

    private void PopulateFromGridJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("maps/map_5"); // ($"maps/map_{Random.Range(0,5)}");

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

                if (id < 0) continue; // empty tile

                Vector3Int pos = new Vector3Int(x - offsetX, offsetY - y - 1, 0);

                PlaceObject(pos, id);
            }
        }
    }

    private void PopulateManually()
    {
        PlaceObject(new Vector3Int(-3, 1, 0), 0);
        PlaceObject(new Vector3Int(-4, 1, 0), 1);
        PlaceObject(new Vector3Int(-3, 0, 0), 2);

        PlaceObject(new Vector3Int(2, -1, 0), 3);
        PlaceObject(new Vector3Int(-1, -2, 0), 4);
        PlaceObject(new Vector3Int(-1, -3, 0), 5);

        //Obstacle
        PlaceObject(new Vector3Int(0, 0, 0), 6);
        PlaceObject(new Vector3Int(0, -1, 0), 6);
        PlaceObject(new Vector3Int(-1, -1, 0), 6);
        PlaceObject(new Vector3Int(-1, 0, 0), 6);

        PlaceObject(new Vector3Int(3, 3, 0), 6);
        PlaceObject(new Vector3Int(3, 2, 0), 6);
        PlaceObject(new Vector3Int(2, 3, 0), 6);

        PlaceObject(new Vector3Int(-4, 3, 0), 6);
        PlaceObject(new Vector3Int(-3, 3, 0), 6);
        PlaceObject(new Vector3Int(-4, 2, 0), 6);

        PlaceObject(new Vector3Int(3, -4, 0), 6);
        PlaceObject(new Vector3Int(3, -3, 0), 6);
        PlaceObject(new Vector3Int(2, -4, 0), 6);
        
        PlaceObject(new Vector3Int(-4, -4, 0), 6);
        PlaceObject(new Vector3Int(-4, -3, 0), 6);
        PlaceObject(new Vector3Int(-3, -4, 0), 6);
    }

    // Core placement

    private void PlaceObject(Vector3Int gridPos, int ID)
    {
        ObjectData data = database.objectsData.Find(d => d.ID == ID);

        if (data == null)
        {
            Debug.LogError($"PlaceObject: no ObjectData found for ID {ID}");
            return;
        }
        // if (ID == 0)
        // {
        //     data.setDamage(GameManager.Instance.playerAtk);
        //     data.setDefense(GameManager.Instance.playerMaxHP);
        // }
        GameObject newObject;

        if (forMultiAgent && IsMonsterTeamID(ID))
        {
            // Pool path: retrieve a pre-existing GameObject instead of instantiating.
            newObject = GetFromPool(ID);
            if (newObject == null)
            {
                Debug.LogError($"PlaceObject (pool): pool exhausted for ID {ID}! " +
                                $"Increase poolSizePerType ({poolSizePerType}).");
                return;
            }

            newObject.transform.position = grid.CellToWorld(gridPos);
            SetUnitVisualsActive(newObject, true);
            newObject.SetActive(true);
            activePooledObjects.Add(newObject);
        }
        else
        {
            newObject = Instantiate(data.Prefab, spawnedObjectContainer);
            newObject.transform.position = grid.CellToWorld(gridPos);
        }


        placedGameObjects.Add(newObject);
        PlacedObject placedObj = CreatePlacedObjectFromData(data);

        if (placedObj is CharacterObject character)
        {
            character.BindGameObject(newObject);
            CharacterView view = newObject.GetComponent<CharacterView>();
            UnitView unitView = newObject.GetComponentInChildren<UnitView>();
            if (view != null && unitView != null)
            {
                view.Bind(character);
                unitView.Bind(character, turnManager);
            }
            else
                Debug.LogWarning($"{newObject.name} has no CharacterView or UnitView!");
        }

        objectsData.AddObjectAt(gridPos, placedObj, placedGameObjects.Count - 1, newObject);
    }

    private GameObject GetFromPool(int id)
    {
        if (!unitPool.TryGetValue(id, out var pool))
        {
            Debug.LogError($"GetFromPool: no pool exists for ID {id}. " +
                            $"Is {id} in monsterTeamIDs?");
            return null;
        }

        int index = poolNextIndex[id];
        if (index >= pool.Count)
        {
            Debug.LogError($"GetFromPool: pool exhausted for ID {id} at index {index}.");
            return null;
        }

        poolNextIndex[id]++;
        return pool[index];
    }

    private bool IsPooledObject(GameObject go)
    {
        foreach (var pool in unitPool.Values)
            if (pool.Contains(go)) return true;
        return false;
    }

    private void SetUnitVisualsActive(GameObject go, bool active)
    {
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = active;

        var anim = go.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = active;

        foreach (var col in go.GetComponentsInChildren<Collider>())
            col.enabled = active;
        foreach (var col2d in go.GetComponentsInChildren<Collider2D>())
            col2d.enabled = active;
    }

    private bool IsMonsterTeamID(int id) => monsterTeamIDs.Contains(id);
    private bool IsMonsterTeamCharacter(CharacterObject c) => monsterTeamIDs.Contains(c.ID);

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

    // Death handling

    private void HandleDeathPlayerOnly(CharacterObject character, TileData tile)
    {
        if (tile.PlacedGameObject.CompareTag("Player"))
        {
            // SceneManager.LoadScene("Tavern");
        } 
        else if (tile.PlacedGameObject.CompareTag("Enemy"))
        {
            turnManager.DefeatedEnemyNames.Add(tile.PlacedGameObject.name); 
        } 
        else if (tile.PlacedGameObject.CompareTag("Allies"))
        {
            // Do something for allies
        }
    }

    // Shared helpers

    private bool IsWalkable(Vector3Int pos)
    {
        if (!IsInsideBounds(pos)) return false;

        var tile = objectsData.GetTileAt(pos);

        if (tile == null) return true;

        return !(tile.PlacedObject is StaticObject || tile.PlacedObject is RandomObject);
    }

    private bool IsInsideBounds(Vector3Int pos) =>
        pos.x >= minX && pos.x <= maxX && 
        pos.y >= minY && pos.y <= maxY;
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
