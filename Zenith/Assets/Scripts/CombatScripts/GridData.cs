using System;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    Dictionary<Vector3Int, TileData> placedObjects = new();

    public void AddObjectAt(Vector3Int gridPos, int ID, int placedObjectIndex)
    {
        TileData data = new TileData(gridPos, ID, placedObjectIndex);
        if (placedObjects.ContainsKey(gridPos))
            throw new Exception($"{gridPos} already occupied");
        placedObjects[gridPos] = data;
    }

    public bool CanPlaceObjectAt(Vector3Int gridPos)
    {
        if (placedObjects.ContainsKey(gridPos))
        {
            return false;
        }
        return true;
    }

    public void MoveObject(Vector3Int Start, Vector3Int End)
    {
        if (CanPlaceObjectAt(End))
        {
            TileData tempData = placedObjects[Start];
            tempData.occupiedPos = End;
            placedObjects[End] = tempData;
            placedObjects.Remove(Start);
        }
    }
}

public class TileData
{
    public Vector3Int occupiedPos;
    public int ID { get; private set; }
    public int PlacedObjectsIndex { get; private set; }

    public TileData(Vector3Int occupiedPos, int iD, int placedObjectsIndex)
    {
        this.occupiedPos = occupiedPos;
        ID = iD;
        PlacedObjectsIndex = placedObjectsIndex;
    }
}
