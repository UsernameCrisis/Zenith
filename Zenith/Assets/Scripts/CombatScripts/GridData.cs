using System;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    Dictionary<Vector3Int, TileData> placedObjects = new();
    Dictionary<int, List<(Vector3Int pos, CharacterObject character)>> teamUnits = new();

    public void AddObjectAt(Vector3Int gridPos, PlacedObject placedObject, int placedObjectIndex, GameObject obj = null)
    {
        if (placedObjects.ContainsKey(gridPos))
            throw new Exception($"{gridPos} already occupied");
        TileData data = new TileData(gridPos, placedObject, placedObjectIndex)
        {
            PlacedGameObject = obj
        };

        placedObjects[gridPos] = data;

        if (placedObject is CharacterObject character)
        {
            if (!teamUnits.ContainsKey(character.Team))
                teamUnits[character.Team] = new List<(Vector3Int, CharacterObject)>();
    
            teamUnits[character.Team].Add((gridPos, character));
        }
    }

    public bool CanPlaceObjectAt(Vector3Int gridPos)
    {
        if (placedObjects.ContainsKey(gridPos))
        {
            return false;
        }
        return true;
    }

    public void MoveObject(Vector3Int start, Vector3Int end)
    {
        if (!CanPlaceObjectAt(end))
            return;

        if (!placedObjects.TryGetValue(start, out TileData tempData))
            return;
        
        RemoveObjectAt(start, true);
        tempData.occupiedPos = end; // Update position in the actual grid data
        placedObjects[end] = tempData;
        

        if (tempData.PlacedObject is CharacterObject character)
        {
            var teamList = teamUnits[character.Team];

            for (int i = 0; i < teamList.Count; i++)
            {
                if (teamList[i].character == character)
                {
                    teamList[i] = (end, character);
                    break;
                }
            }
        }

        tempData.PlacedObject.OnPlaced(end); // Update position variable inside placed object
        if (tempData.PlacedGameObject != null)
            tempData.PlacedGameObject.transform.position = new Vector3(end.x, 0, end.y);
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

    public GameObject GetObjectAt(Vector3Int pos)
    {
        var tile = GetTileAt(pos);
        return tile?.PlacedGameObject;
    }
    
    public Vector3Int? GetPositionOf(CharacterObject character)
    {
        foreach (var kvp in placedObjects)
        {
            TileData tile = kvp.Value;

            if (tile.PlacedObject == character)
            {
                return kvp.Key;
            }
        }

        return null; // Character not found on the grid
    }

    public Dictionary<Vector3Int, TileData> GetAllTiles()
    {
        return placedObjects;
    }

    public List<(Vector3Int pos, CharacterObject character)> GetAllUnits()
    {
        List<(Vector3Int, CharacterObject)> result = new();

        foreach (var team in teamUnits.Values)
            result.AddRange(team);

        return result;
    }

    public List<(Vector3Int pos, CharacterObject character)> GetEnemyTeamUnit(int team)
    {
        if (team == 1) 
            return GetUnitsByTeam(2);
        else if (team == 2)
            return GetUnitsByTeam(1);
        else
            return GetUnitsByTeam(1);
        
    }

    public List<(Vector3Int pos, CharacterObject character)> GetUnitsByTeam(int team)
    {
        if (teamUnits.TryGetValue(team, out var list))
            return new List<(Vector3Int, CharacterObject)>(list);

        return new List<(Vector3Int, CharacterObject)>();
    }

    public List<(Vector3Int pos, CharacterObject character)> GetAllFriendlies()
    {
        return GetUnitsByTeam(1);
    }

    public List<(Vector3Int pos, CharacterObject character)> GetAllPlayers()
    {
        List<(Vector3Int, CharacterObject)> list = new();

        foreach (var kvp in placedObjects)
        {
            TileData tile = kvp.Value;
            if (tile.PlacedObject is CharacterObject c && c.IsPlayer)
            {
                list.Add((kvp.Key, c));
            }
        }

        return list;
    }

    public List<(Vector3Int pos, CharacterObject character)> GetTeamNPC()
    {
        List<(Vector3Int, CharacterObject)> list = new();

        foreach (var kvp in placedObjects)
        {
            TileData tile = kvp.Value;
            if (tile.PlacedObject is CharacterObject c && c.Team == 1 && !c.IsPlayer)
            {
                list.Add((kvp.Key, c));
            }
        }

        return list;
    }

    public List<(Vector3Int pos, CharacterObject character)> GetAllEnemies()
    {
        return GetUnitsByTeam(2);
    }

    public bool IsWithinBounds(Vector3Int pos)
    {
        // Replace with your actual grid limits if you have them stored
        return pos.x >= -5 && pos.y >= -5 && pos.x < 5 && pos.y < 5;
    }

    public int GetTeamAt(Vector3Int pos)
    {
        TileData tile = GetTileAt(pos);

        if (tile?.PlacedObject is CharacterObject character)
            return character.Team;
        return -1;
    }

    public void RemoveObjectAt(Vector3Int gridPos, bool isMoving = false)
    {
        if (!placedObjects.TryGetValue(gridPos, out TileData data))
            return;
        
        if (data.PlacedObject is CharacterObject character)
        {
            var teamList = teamUnits[character.Team];

            if (!isMoving)
            {
                teamList.RemoveAll(x => x.character == character);
            }
        }
        if (!isMoving)
        {
            data.PlacedObject?.OnRemoved();
        }
        placedObjects.Remove(gridPos);
        
    }

    public void Clear()
    {
        placedObjects.Clear();
        teamUnits.Clear();
    }
}

public class TileData
{
    public Vector3Int occupiedPos;
    public PlacedObject PlacedObject { get; private set; }
    public int PlacedObjectsIndex { get; private set; }
    public GameObject PlacedGameObject { get; set; }

    public TileData(Vector3Int occupiedPos, PlacedObject placedObject, int placedObjectsIndex)
    {
        this.occupiedPos = occupiedPos;
        PlacedObject = placedObject;
        PlacedObjectsIndex = placedObjectsIndex;
    }
}
