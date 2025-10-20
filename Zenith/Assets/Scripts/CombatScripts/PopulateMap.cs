using System.Collections.Generic;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField] private ObjectDatabaseSO database;
    [SerializeField] private Grid grid;

    public GridData objectsData;
    public List<GameObject> placedGameObjects = new();

    // Sementara manual isinya, nanti kedepan pakai json
    void Start()
    {
        objectsData = new();
        PlaceObject(new Vector3Int(1, 1, 0), 0, placedGameObjects.Count - 1);
    }
    
    private void PlaceObject(Vector3Int gridPos, int ID, int placedObjectIndex)
    {
        GameObject newObject = Instantiate(database.objectsData[ID].Prefab);
        newObject.transform.position = grid.CellToWorld(gridPos);
        objectsData.AddObjectAt(gridPos, ID, placedObjectIndex);
    }
}
