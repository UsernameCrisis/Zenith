using System.Collections.Generic;
using UnityEngine;

public class PopulateMap : MonoBehaviour
{
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Grid grid;
    [SerializeField] private bool loadFromSave = false;

    public GridData objectsData;
    public List<GameObject> placedGameObjects = new();

    void Awake()
    {
        objectsData = new();
    }
    void Start()
    {
        if (loadFromSave && SaveSystem.HasSaveFile())
        {
            LoadFromSave();
        }
        else
        {
            PopulateManually();
        }
    }

    private void PopulateManually()
    {
        PlaceObject(new Vector3Int(-3, 1, 0), 0, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(2, -1, 0), 1, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, -2, 0), 2, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, -3, 0), 3, placedGameObjects.Count - 1);

        //Obstacle
        PlaceObject(new Vector3Int(0, 0, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(0, -1, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, -1, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, 0, 0), 4, placedGameObjects.Count - 1);

        PlaceObject(new Vector3Int(3, 3, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(3, 2, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(2, 3, 0), 4, placedGameObjects.Count - 1);

        PlaceObject(new Vector3Int(-4, 3, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-3, 3, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-4, 2, 0), 4, placedGameObjects.Count - 1);

        PlaceObject(new Vector3Int(3, -4, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(3, -3, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(2, -4, 0), 4, placedGameObjects.Count - 1);
        
        PlaceObject(new Vector3Int(-4, -4, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-4, -3, 0), 4, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-3, -4, 0), 4, placedGameObjects.Count - 1);
    }
    
    private void LoadFromSave()
    {
        // Load GridData from JSON
        objectsData = SaveSystem.Load(database);

        foreach (var kvp in objectsData.GetAllTiles())
        {
            TileData tile = kvp.Value;
            Vector3Int gridPos = kvp.Key;
            PlacedObject placedObj = tile.PlacedObject;

            ObjectData data = database.objectsData.Find(d => d.Name == placedObj.Name);
            if (data == null)
            {
                Debug.LogWarning($"No prefab found in database for {placedObj.Name}");
                continue;
            }

            GameObject obj = Instantiate(data.Prefab, grid.CellToWorld(gridPos), Quaternion.identity);
            placedGameObjects.Add(obj);

            if (placedObj is CharacterObject character)
            {
                character.OnDied += HandleCharacterDeath;
                CharacterView view = obj.GetComponent<CharacterView>();
                if (view != null)
                    view.Bind(character);
            }
        }

        Debug.Log("Map populated from save file.");
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
