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
    [SerializeField] private Grid grid;
    [SerializeField] private MovementPreview previewSystem;

    [HideInInspector] public int currEp = 0;
    [HideInInspector] public float cumulativeReward = 0f;

    private List<CharacterObject> allySlots = new();
    private List<CharacterObject> enemySlots = new();
    private int activeUnitIndex = 0;
    private int mapMin = -5;
    private int mapMax = 5;
    private bool isMoving;

    public override void Initialize()
    {
        currEp = 0;
        cumulativeReward = 0f;
    }

    public override void OnEpisodeBegin()
    {
        allySlots.Clear();
        enemySlots.Clear();

        // sementara hard code team 1 untuk agent team 2 untuk enemy
        var allies = _gridData.GetUnitsByTeam(1);
        var enemies = _gridData.GetUnitsByTeam(2);

        for (int i = 0; i < 3; i++)
        {
            if (i < allies.Count)
                allySlots.Add(allies[i].character);
            else
                allySlots.Add(null);
        }

        for (int i = 0; i < 3; i++)
        {
            if (i < enemies.Count)
                enemySlots.Add(enemies[i].character);
            else
                enemySlots.Add(null);
        }

        activeUnitIndex = 0; // sementara pakai ini (belum sesuai dengan ATB)
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // GRID
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
    
        // ALLY UNIT DATA
        for (int i = 0; i < 3; i++)
        {
            CharacterObject character = allySlots[i];
            if (i >= allySlots.Count) //character == null || character.HP <= 0
            {
                // padding if fewer units
                for (int j = 0; j < 9; j++)
                    sensor.AddObservation(0f);
            }
            else
            {
                Vector3Int pos = character.Position;
    
                sensor.AddObservation((float)character.HP / character.MaxHp);
                sensor.AddObservation((float)character.Damage / 20f);
                sensor.AddObservation((float)character.Defense / 20f);
                sensor.AddObservation((float)character.AtkRange / 5f);

                sensor.AddObservation(character.CurrentATB / 100f);
                sensor.AddObservation(character.Speed / 20f);
    
                sensor.AddObservation((pos.x + 5) / 10f);
                sensor.AddObservation((pos.y + 5) / 10f);
    
                sensor.AddObservation(character.HP > 0 ? 1f : 0f);
            }
        }
    
        //  ENEMY UNIT DATA
        for (int i = 0; i < 3; i++)
        {
            CharacterObject character = enemySlots[i];
            if (i >= enemySlots.Count)
            {
                for (int j = 0; j < 9; j++)
                    sensor.AddObservation(0f);
            }
            else
            {
                Vector3Int pos = character.Position;
    
                sensor.AddObservation((float)character.HP / character.MaxHp);
                sensor.AddObservation((float)character.Damage / 20f);
                sensor.AddObservation((float)character.Defense / 20f);
                sensor.AddObservation((float)character.AtkRange / 5f);

                sensor.AddObservation(character.CurrentATB / 100f);
                sensor.AddObservation(character.Speed / 20f);
    
                sensor.AddObservation((pos.x + 5) / 10f);
                sensor.AddObservation((pos.y + 5) / 10f);
    
                sensor.AddObservation(character.HP > 0 ? 1f : 0f);
            }
        }
    
        // WHICH UNIT IS ACTING
        for (int i = 0; i < 3; i++)
        {
            sensor.AddObservation(i == activeUnitIndex ? 1f : 0f);
        }
    
        // TURN INFO
        sensor.AddObservation(_turnmanager.currentTurn / _maxTurn);
    
        sensor.AddObservation(GetAliveAllies() / 3f); // num allies alive
        sensor.AddObservation(GetAliveEnemies() / 3f); // num enemies alive
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // BELUM ADA ACTION MASKING
        // ACTION MASKING MENGGUNAKAN PREVIEW SYSTEM (reachable dan attackable tile)
        int actionType = actions.DiscreteActions[0];
        int targetX = actions.DiscreteActions[1] - 5; // -5 karena range x dan y -5 hingga 4
        int targetY = actions.DiscreteActions[2] - 5;

        Vector3Int targetPos = new Vector3Int(targetX, targetY, 0);

        var allies = _gridData.GetUnitsByTeam(1); // hard code agent

        if (activeUnitIndex < 0 || activeUnitIndex >= allySlots.Count)
        {
            _turnmanager.EndTurn();
            return;
        }

        var (currentPos, character) = allies[activeUnitIndex];

        switch (actionType)
        {
            case 0: // MOVE
                HandleMove(currentPos, targetPos, character); 
                break;

            case 1: // ATTACK
                HandleAttack(currentPos, targetPos, character);
                break;

            case 2: // WAIT
                break;
        }

        _turnmanager.EndTurn();
    }
    
    private float GetAliveAllies()
    {
        int alive = 0;

        foreach (var unit in allySlots)
        {
            if (unit != null && unit.HP > 0)
                alive++;
        }

        return alive;
    }

    private float GetAliveEnemies()
    {
        int alive = 0;

        foreach (var unit in enemySlots)
        {
            if (unit != null && unit.HP > 0)
                alive++;
        }

        return alive;
    }

    public void SetActiveUnitIndex(int index)
    {
        activeUnitIndex = index;
    }

    void HandleMove(Vector3Int start, Vector3Int target, CharacterObject character)
    {
        var path = previewSystem.FindPathAStar(start, target);

        if (path == null || path.Count == 0)
            return;

        if (path.Count > character.RemainingMoveRange)
        {
            Debug.Log("Not enough movement points!");
            return;
        }
            
        if (_gridData.CanPlaceObjectAt(target))
        {
            StartCoroutine(WalkPath(path, start, character));
        }
    }

    void HandleAttack(Vector3Int attackerPos, Vector3Int targetPos, CharacterObject character)
    {
        if (attackerPos == targetPos)
            return;

        _gridData.AttackObject(attackerPos, targetPos);
    }

    public IEnumerator WalkPath(List<Vector3Int> path, Vector3Int currPos, CharacterObject character)
    {
        if (path == null || path.Count == 0)
            yield break;

        Transform enemyTransform =
            _gridData.GetTileAt(currPos).PlacedGameObject.transform;

        isMoving = true;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 start = enemyTransform.position;
            Vector3 end = grid.CellToWorld(path[i]);

            float t = 0f;
            float speed = 2f;

            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                enemyTransform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }

        Vector3Int finalPos = path[^1];
        // latestPos = finalPos;

        _gridData.MoveObject(currPos, finalPos);

        character.UseMovement(path.Count);
        isMoving = false;
    }
}
