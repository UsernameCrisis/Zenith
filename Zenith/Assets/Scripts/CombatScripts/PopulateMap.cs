using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PopulateMap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Transform spawnedObjectContainer;
    [SerializeField] private Grid grid;

    [Header("Generation mode")]
    [SerializeField] private bool loadFromSave = true;
    [SerializeField] private bool forTrainingAgent = false;

    [Header("Training generation settings")]
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
        foreach (var obj in placedGameObjects)
            if (obj != null) Destroy(obj);

        placedGameObjects.Clear();
        objectsData.Clear();
    }

    public void HandleCharacterDeath(CharacterObject character)
    {
        Vector3Int? pos = objectsData.GetPositionOf(character);
        if (pos == null) return;

        TileData tile = objectsData.GetTileAt(pos.Value);
        
        if (!forTrainingAgent)
            HandleDeathPlayerOnly(character, tile);

        if (tile.PlacedGameObject != null)
            Destroy(tile.PlacedGameObject);

        objectsData.RemoveObjectAt(pos.Value);
    }

    private void GenerateTrainingMap()
    {
        var generator = new TrainingMapGenerator(
            width, height,
            minX, maxX, minY, maxY,
            teamDist, obstacleID,
            obstacleDensity, clusterLimit,
            minTraversablePaths,
            useMaxFlow, isFullyRandom,
            isOccupied: pos => objectsData.GetTileAt(pos) != null,
            isWalkable: IsWalkable,
            placeObject: (pos, id) => PlaceObject(pos, id),
            removeLastObject: pos =>
            {
                objectsData.RemoveObjectAt(pos);
                Destroy(placedGameObjects[^1]);
                placedGameObjects.RemoveAt(placedGameObjects.Count - 1);
            });

        generator.Generate();
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

                if (id < 0) continue; // empty tile

                Vector3Int pos = new Vector3Int(x - offsetX, offsetY - y - 1, 0);

                PlaceObject(pos, id);
            }
        }
    }

    private void PopulateManually()
    {
        PlaceObject(new Vector3Int(-3, 1, 0), 0);
        PlaceObject(new Vector3Int(2, -1, 0), 1);
        PlaceObject(new Vector3Int(-1, -2, 0), 2);
        PlaceObject(new Vector3Int(-1, -3, 0), 3);

        //Obstacle
        PlaceObject(new Vector3Int(0, 0, 0), 7);
        PlaceObject(new Vector3Int(0, -1, 0), 7);
        PlaceObject(new Vector3Int(-1, -1, 0), 7);
        PlaceObject(new Vector3Int(-1, 0, 0), 7);

        PlaceObject(new Vector3Int(3, 3, 0), 7);
        PlaceObject(new Vector3Int(3, 2, 0), 7);
        PlaceObject(new Vector3Int(2, 3, 0), 7);

        PlaceObject(new Vector3Int(-4, 3, 0), 7);
        PlaceObject(new Vector3Int(-3, 3, 0), 7);
        PlaceObject(new Vector3Int(-4, 2, 0), 7);

        PlaceObject(new Vector3Int(3, -4, 0), 7);
        PlaceObject(new Vector3Int(3, -3, 0), 7);
        PlaceObject(new Vector3Int(2, -4, 0), 7);
        
        PlaceObject(new Vector3Int(-4, -4, 0), 7);
        PlaceObject(new Vector3Int(-4, -3, 0), 7);
        PlaceObject(new Vector3Int(-3, -4, 0), 7);
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
        GameObject newObject = Instantiate(data.Prefab, spawnedObjectContainer);
        newObject.transform.position = grid.CellToWorld(gridPos);
        placedGameObjects.Add(newObject);
        PlacedObject placedObj = CreatePlacedObjectFromData(data);

        if (placedObj is CharacterObject character)
        {
            character.BindGameObject(newObject);
            CharacterView view = newObject.GetComponent<CharacterView>();
            if (view != null)
                view.Bind(character);
            else
                Debug.LogWarning($"{newObject.name} has no CharacterView!");
        }

        objectsData.AddObjectAt(gridPos, placedObj, placedGameObjects.Count - 1, newObject);
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

    // Death handling

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
