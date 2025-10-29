using System;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    Dictionary<Vector3Int, TileData> placedObjects = new();

    public void AddObjectAt(Vector3Int gridPos, PlacedObject placedObject, int placedObjectIndex)
    {
        if (placedObjects.ContainsKey(gridPos))
            throw new Exception($"{gridPos} already occupied");
        TileData data = new TileData(gridPos, placedObject, placedObjectIndex);
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
            tempData.PlacedObject.OnPlaced(End); // Update position for the placed object
        }
    }

    public void AttackObject(Vector3Int attackerPos, Vector3Int targetPos)
    {
        if (!placedObjects.ContainsKey(attackerPos))
        {
            Debug.LogWarning($"No attacker found at {attackerPos}");
            return;
        }

        if (!placedObjects.ContainsKey(targetPos))
        {
            Debug.LogWarning($"No target found at {targetPos}");
            return;
        }

        TileData attackerData = placedObjects[attackerPos];
        TileData targetData = placedObjects[targetPos];

        if (attackerData.PlacedObject == null || targetData.PlacedObject == null)
            return;

        if (attackerData.PlacedObject is CharacterObject attacker &&
            targetData.PlacedObject is CharacterObject target)
        {
            attacker.Attack(target);
        }
        else
        {
            Debug.LogWarning("One of the selected tiles does not contain a CharacterObject.");
        }
    }

    public TileData GetTileAt(Vector3Int pos)
    {
        placedObjects.TryGetValue(pos, out TileData data);
        return data;
    }

    public void RemoveObjectAt(Vector3Int gridPos)
    {
        if (placedObjects.TryGetValue(gridPos, out TileData data))
        {
            data.PlacedObject?.OnRemoved();
            placedObjects.Remove(gridPos);
        }
    }
}

public class TileData
{
    public Vector3Int occupiedPos;
    public PlacedObject PlacedObject { get; private set; }
    public int PlacedObjectsIndex { get; private set; }

    public TileData(Vector3Int occupiedPos, PlacedObject placedObject, int placedObjectsIndex)
    {
        this.occupiedPos = occupiedPos;
        PlacedObject = placedObject;
        PlacedObjectsIndex = placedObjectsIndex;
    }
}
