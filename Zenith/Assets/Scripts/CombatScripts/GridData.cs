using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    Dictionary<Vector3Int, TileData> placedObjects = new();

    public void AddObjectAt(Vector3Int gridPos, PlacedObject placedObject, int placedObjectIndex, GameObject obj = null)
    {
        if (placedObjects.ContainsKey(gridPos))
            throw new Exception($"{gridPos} already occupied");
        TileData data = new TileData(gridPos, placedObject, placedObjectIndex);
        data.PlacedGameObject = obj;
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
            tempData.occupiedPos = End; // Update position in the actual grid data
            placedObjects[End] = tempData;
            RemoveObjectAt(Start);
            tempData.PlacedObject.OnPlaced(End); // Update position for the placed object

            if (tempData.PlacedGameObject != null)
                tempData.PlacedGameObject.transform.position = new Vector3(End.x, 0, End.y);
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
        var result = new List<(Vector3Int, CharacterObject)>();
        result.AddRange(GetAllPlayers());
        result.AddRange(GetTeamNPC());
        result.AddRange(GetAllEnemies());
        return result;
    }

    public List<(Vector3Int pos, CharacterObject character)> GetAllFriendlies()
    {
        var result = new List<(Vector3Int, CharacterObject)>();
        result.AddRange(GetAllPlayers());
        result.AddRange(GetTeamNPC());
        return result;
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
        List<(Vector3Int, CharacterObject)> list = new();

        foreach (var kvp in placedObjects)
        {
            TileData tile = kvp.Value;
            if (tile.PlacedObject is CharacterObject c && c.Team == 2 && !c.IsPlayer)
            {
                list.Add((kvp.Key, c));
            }
        }

        return list;
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

    public void RemoveObjectAt(Vector3Int gridPos)
    {
        if (placedObjects.TryGetValue(gridPos, out TileData data))
        {
            data.PlacedObject?.OnRemoved();
            placedObjects.Remove(gridPos);
        }
    }

    public GridSaveData ToSaveData()
    {
        GridSaveData saveData = new GridSaveData();

        foreach (var kvp in placedObjects)
        {
            Vector3Int pos = kvp.Key;
            TileData tile = kvp.Value;
            PlacedObject obj = tile.PlacedObject;
            if (obj == null) continue;

            // Example: assuming your PlacedObject has fields like ID, Team, HP
            TileSaveData tileSave = new TileSaveData
            {
                x = pos.x,
                y = pos.y,
                z = pos.z,
                name = obj.Name,
                type = obj.ObjectType
            };

            if (obj is CharacterObject character)
            {
                tileSave.hp = character.HP;
                tileSave.maxHp = character.MaxHp;
                tileSave.damage = character.Damage;
                tileSave.defense = character.Defense;
                tileSave.team = character.Team;
                tileSave.isPlayer = character.IsPlayer;
                tileSave.atkRange = character.AtkRange;
            }

            saveData.tiles.Add(tileSave);
        }
        return saveData;
    }

    public static GridData FromSaveData(GridSaveData saveData, ObjectDatabaseSO database)
    {
        GridData grid = new GridData();

        foreach (TileSaveData tileSave in saveData.tiles)
        {
            Vector3Int pos = new Vector3Int(tileSave.x, tileSave.y, tileSave.z);
            PlacedObject placedObject = null;

            switch (tileSave.type)
            {
                case ObjectType.Static:
                    placedObject = new StaticObject(tileSave.name);
                    break;

                case ObjectType.RandomProp:
                    placedObject = new RandomObject(tileSave.name);
                    break;

                case ObjectType.Character:
                    placedObject = new CharacterObject(
                        tileSave.name,
                        tileSave.hp,
                        tileSave.damage,
                        tileSave.defense,
                        tileSave.team,
                        tileSave.atkRange,
                        tileSave.isPlayer
                    );
                    break;
            }

            if (placedObject != null)
            {
                grid.AddObjectAt(pos, placedObject, 0);
            }
        }

        return grid;
    }
}

[System.Serializable]
public class GridSaveData
{
    public List<TileSaveData> tiles = new();
}

[System.Serializable]
public class TileSaveData
{
    public int x, y, z;
    public string name;
    public ObjectType type;
    public int hp;
    public int maxHp;
    public int damage;
    public int defense;
    public int team;
    public bool isPlayer;
    public int atkRange;
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
