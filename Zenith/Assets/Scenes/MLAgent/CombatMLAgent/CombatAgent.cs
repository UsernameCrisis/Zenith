using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;

public class CombatAgent : Agent
{
    [SerializeField] private GridData _gridData;
    [SerializeField] private float _maxTurn;
    [SerializeField] private TurnManager _turnmanager;

    [HideInInspector] public int currEp = 0;
    [HideInInspector] public float cumulativeReward = 0f;

    private Dictionary<int, CharacterObject> _characterDict = new();
    

    public override void Initialize()
    {
        currEp = 0;
        cumulativeReward = 0f;

        for (int i = 0; i < 3; i++)
        {
            
        }
    }

    public override void OnEpisodeBegin()
    {
        
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        int mapMin = -5;
        int mapMax = 5;
        int mapSize = 10;
        int activeUnitIndex = 0;
    
        // --- 1. GLOBAL GRID ---
        for (int x = mapMin; x < mapMax; x++)
        {
            for (int y = mapMin; y < mapMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
    
                TileData tile = _gridData.GetTileAt(pos);
    
                if (tile == null)
                {
                    // empty tile
                    sensor.AddObservation(0f); // obstacle
                    sensor.AddObservation(0f); // ally
                    sensor.AddObservation(0f); // enemy
                }
                else if (tile.PlacedObject is CharacterObject character)
                {
                    sensor.AddObservation(0f); // obstacle
    
                    if (character.Team == 1)
                    {
                        sensor.AddObservation(1f); // ally
                        sensor.AddObservation(0f);
                    }
                    else
                    {
                        sensor.AddObservation(0f);
                        sensor.AddObservation(1f); // enemy
                    }
                }
                else
                {
                    // static object / obstacle
                    sensor.AddObservation(1f);
                    sensor.AddObservation(0f);
                    sensor.AddObservation(0f);
                }
            }
        }
    
        // --- 2. ALLY UNIT DATA (TEAM NPC) ---
        var allies = _gridData.GetTeamNPC();
    
        for (int i = 0; i < 3; i++)
        {
            if (i >= allies.Count)
            {
                // padding if fewer units
                for (int j = 0; j < 7; j++)
                    sensor.AddObservation(0f);
            }
            else
            {
                var (pos, character) = allies[i];
    
                sensor.AddObservation((float)character.HP / character.MaxHp);
                sensor.AddObservation((float)character.Damage / 20f);
                sensor.AddObservation((float)character.Defense / 20f);
                sensor.AddObservation((float)character.AtkRange / 5f);
    
                sensor.AddObservation((pos.x + 5) / 10f);
                sensor.AddObservation((pos.y + 5) / 10f);
    
                sensor.AddObservation(character.HP > 0 ? 1f : 0f);
            }
        }
    
        // --- 3. ENEMY UNIT DATA ---
        var enemies = _gridData.GetAllEnemies();
    
        for (int i = 0; i < 3; i++)
        {
            if (i >= enemies.Count)
            {
                for (int j = 0; j < 7; j++)
                    sensor.AddObservation(0f);
            }
            else
            {
                var (pos, character) = enemies[i];
    
                sensor.AddObservation((float)character.HP / character.MaxHp);
                sensor.AddObservation((float)character.Damage / 20f);
                sensor.AddObservation((float)character.Defense / 20f);
                sensor.AddObservation((float)character.AtkRange / 5f);
    
                sensor.AddObservation((pos.x + 5) / 10f);
                sensor.AddObservation((pos.y + 5) / 10f);
    
                sensor.AddObservation(character.HP > 0 ? 1f : 0f);
            }
        }
    
        // --- 4. WHICH UNIT IS ACTING ---
        for (int i = 0; i < 3; i++)
        {
            sensor.AddObservation(i == activeUnitIndex ? 1f : 0f);
        }
    
        // --- 5. TURN INFO ---
        sensor.AddObservation(_turnmanager.currentTurn / _maxTurn);
    
        sensor.AddObservation(allies.Count / 3f);
        sensor.AddObservation(enemies.Count / 3f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        base.OnActionReceived(actions);
    }
}
