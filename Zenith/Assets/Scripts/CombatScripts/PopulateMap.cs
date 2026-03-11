using System.Collections.Generic;
using UnityEngine;

public class PopulateMap : MonoBehaviour
{
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Grid grid;
    [SerializeField] private bool loadFromSave = true;
    [SerializeField] private bool forTrainingAgent = false;

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
            PopulateForAgent();
        }
        else
        {
            PopulateManually();
        }
    }

    private void PopulateForAgent()
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

        int height = map.grid.Length;
        int width = map.grid[0].row.Length;

        int offsetX = width / 2;
        int offsetY = height / 2;

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
