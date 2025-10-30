using System.Collections.Generic;
using UnityEngine;

public class PopulateMap : MonoBehaviour
{
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Grid grid;

    public GridData objectsData;
    public List<GameObject> placedGameObjects = new();

    // Sementara manual isinya, nanti kedepan pakai json

    void Awake()
    {
        objectsData = new();
    }
    void Start()
    {
        PlaceObject(new Vector3Int(1, 1, 0), 0, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(-1, -1, 0), 1, placedGameObjects.Count - 1);
        PlaceObject(new Vector3Int(0, 0, 0), 2, placedGameObjects.Count - 1);
    }

    private void PlaceObject(Vector3Int gridPos, int ID, int placedObjectIndex)
    {
        ObjectData data = database.objectsData.Find(d => d.ID == ID);
        GameObject newObject = Instantiate(data.Prefab);
        newObject.transform.position = grid.CellToWorld(gridPos);
        placedGameObjects.Add(newObject);
        PlacedObject placedObj = CreatePlacedObjectFromData(data);
        objectsData.AddObjectAt(gridPos, placedObj, placedObjectIndex);
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
                return new CharacterObject(data.Name, 100, data.Damage, data.Defense, data.Team, data.IsPlayer);
            default:
                return new StaticObject(data.Name);
        }
    }
}
