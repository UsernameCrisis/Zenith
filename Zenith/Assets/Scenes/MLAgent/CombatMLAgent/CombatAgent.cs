using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
using System;

public class CombatAgent : Agent
{
    [SerializeField] private float _maxTurn;
    [SerializeField] private Grid grid;
    [SerializeField] private bool isManualMode = false;
    [SerializeField] private MovementPreview previewSystem;
    [SerializeField] private CombatExecutor combatExecutor;
    [SerializeField] private PlayerSystem playerSystem;

    [HideInInspector] public int currEp = 0;
    [HideInInspector] public float cumulativeReward = 0f;

    private List<CharacterObject> allySlots = new();
    private List<CharacterObject> enemySlots = new();
    private int chosenActionType = 2;
    private int chosenTileIndex = 55;
    private int _currentAgentTeam = 1;
    private GridData _gridData;
    private int activeUnitIndex;
    private int mapMin = -5;
    private int mapMax = 5;
    private bool hasAction = false;
    public Action OnTurnEnded;

    public override void Initialize()
    {
        currEp = 0;
        cumulativeReward = 0f;
    }

    public override void OnEpisodeBegin()
    {
        allySlots.Clear();
        enemySlots.Clear();

        var allies = _gridData.GetUnitsByTeam(_currentAgentTeam);
        var enemies = _gridData.GetUnitsByTeam(_currentAgentTeam == 1 ? 2 : 1);

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

        // this code should not run (should already be handled by turn manager and turn queue)
        if (allySlots[activeUnitIndex] == null)
        {
            Debug.Log("<color=red>WARNING:</color> null slot is selected, changing to another unit!");
            activeUnitIndex = allySlots.FindIndex(u => u != null);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Debug.Log("OBS CALLED");
        // fallback if the active unit is null
        CharacterObject active = allySlots[activeUnitIndex];
        Vector3Int currPos = active != null ? active.Position : Vector3Int.zero;

        // GRID
        for (int x = mapMin; x < mapMax; x++)
        {
            for (int y = mapMin; y < mapMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileData tile = _gridData.GetTileAt(pos);
                float occupation;

                if (tile == null)
                    // empty tile
                    occupation = 0f;
                else if (tile.PlacedObject is CharacterObject character)
                    occupation = (character.Team == _currentAgentTeam) ? 2f : 3f;
                else
                    occupation = 1f; // obstacle

                sensor.AddObservation(occupation / 3f);
                Debug.Log("Grid loop");
            }
        }

        // ALLY UNIT DATA
        for (int i = 0; i < 3; i++)
        {
            CharacterObject character = allySlots[i];
            if (character == null) 
            {
                // padding if fewer units
                for (int j = 0; j < 8; j++)
                    sensor.AddObservation(0f);
            }
            else
            {
                sensor.AddObservation((float)character.HP / character.MaxHp);
                sensor.AddObservation((float)character.Damage / 20f);
                sensor.AddObservation((float)character.Defense / 20f);
                sensor.AddObservation((float)character.AtkRange / 5f);

                sensor.AddObservation(character.CurrentATB / 100f);
                sensor.AddObservation(character.Speed / 20f);

                sensor.AddObservation((character.Position.x - currPos.x) / 10f); // ubah jadi relative pos
                sensor.AddObservation((character.Position.y - currPos.y) / 10f);
            }
        }
    
        //  ENEMY UNIT DATA
        for (int i = 0; i < 3; i++)
        {
            CharacterObject character = enemySlots[i];
            if (character == null)
            {
                for (int j = 0; j < 8; j++)
                    sensor.AddObservation(0f);
            }
            else
            {
                sensor.AddObservation((float)character.HP / character.MaxHp);
                sensor.AddObservation((float)character.Damage / 20f);
                sensor.AddObservation((float)character.Defense / 20f);
                sensor.AddObservation((float)character.AtkRange / 5f);

                sensor.AddObservation(character.CurrentATB / 100f);
                sensor.AddObservation(character.Speed / 20f);

                sensor.AddObservation((character.Position.x - currPos.x) / 10f); // ubah jadi relative pos
                sensor.AddObservation((character.Position.y - currPos.y) / 10f);
            }
        }

        // WHICH UNIT IS ACTING
        for (int i = 0; i < 3; i++)
            sensor.AddObservation(i == activeUnitIndex ? 1f : 0f);

        // TURN INFO
        sensor.AddObservation(TurnManager.Instance.currentTurn / _maxTurn);

        sensor.AddObservation(GetAliveAllies() / 3f); // num allies alive
        sensor.AddObservation(GetAliveEnemies() / 3f); // num enemies alive
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Debug.Log("Inside heuristic0");
        var discrete = actionsOut.DiscreteActions;

        if (!hasAction)
        {
            discrete[0] = 2; // EndTurn
            discrete[1] = 55;
            return;
        }

        discrete[0] = chosenActionType;
        discrete[1] = chosenTileIndex;
        hasAction = false;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-0.01f);
        Debug.Log("ACTION RECEIVED");
        // BELUM CEK ACTION MASKING BISA APA TIDAK
        int actionType = actions.DiscreteActions[0];
        int tileIndex = actions.DiscreteActions[1]; 

        int x = (tileIndex % 10) - 5; // -5 karena range x dan y -5 hingga 4
        int y = (tileIndex / 10) - 5;
        Vector3Int targetPos = new Vector3Int(x, y, 0);

        var allies = _gridData.GetUnitsByTeam(_currentAgentTeam);

        if (activeUnitIndex < 0 || activeUnitIndex >= allySlots.Count)
        {
            TurnManager.Instance.EndTurn();
            return;
        }

        var (currentPos, character) = allies[activeUnitIndex];

        switch (actionType)
        {
            case 0:
                if (!previewSystem.BFSReachables(currentPos, character.RemainingMoveRange).Contains(targetPos))
                {
                    AddReward(-0.1f);
                    break;
                }
                // AddReward(0.05f);
                combatExecutor.ExecuteMove(character, currentPos, targetPos, _gridData);
                StartCoroutine(WaitForMoveThenContinue());
                break;

            case 1:
                if (!previewSystem.GetAttackableTiles(currentPos, character.AtkRange).Contains(targetPos))
                {
                    AddReward(-0.1f);
                    break;
                }
                combatExecutor.ExecuteAttack(character, currentPos, targetPos, _gridData);
                AddReward(0.05f);
                OnActionFinished();
                break;

            case 2:
                HandleEndTurn(character);
                break;
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        var allies = _gridData.GetUnitsByTeam(_currentAgentTeam);

        if (activeUnitIndex < 0 || activeUnitIndex >= allies.Count)
            return;

        var (currentPos, character) = allies[activeUnitIndex];

        HashSet<Vector3Int> reachable = previewSystem.BFSReachables(currentPos, character.RemainingMoveRange);
        HashSet<Vector3Int> attackable = previewSystem.GetAttackableTiles(currentPos, character.AtkRange);

        // MASK ACTION TYPE
        bool canMove = reachable.Count > 0;
        bool canAttack = attackable.Count > 0;

        if (!canMove)
            actionMask.SetActionEnabled(0, 0, false); // disable MOVE

        if (!canAttack)
            actionMask.SetActionEnabled(0, 1, false); // disable ATTACK

        // MASK X/Y
        // Build allowed positions
        HashSet<int> validTiles = new();

        foreach (Vector3Int pos in reachable)
        {
            int index = (pos.y + 5) * 10 + (pos.x + 5);
            validTiles.Add(index);
        }
    
        foreach (Vector3Int pos in attackable)
        {
            int index = (pos.y + 5) * 10 + (pos.x + 5);
            validTiles.Add(index);
        }
    
        // ALWAYS include current position as fallback
        int currentIndex = (currentPos.y + 5) * 10 + (currentPos.x + 5);
        validTiles.Add(currentIndex);
    
        // Apply mask
        for (int i = 0; i < 100; i++)
        {
            if (!validTiles.Contains(i))
                actionMask.SetActionEnabled(1, i, false);
        }
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

    public void SetManualAction(int actionType, Vector3Int targetPos)
    {
        chosenActionType = actionType;
        chosenTileIndex = (targetPos.y + 5) * 10 + (targetPos.x + 5);
        hasAction = true;

        RequestDecision();
    }
    
    public void SetAgentTeam(int team)
    {
        _currentAgentTeam = team;
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

    private IEnumerator WaitForMoveThenContinue()
    {
        while (combatExecutor.IsMoving)
            yield return null;

        OnActionFinished(); // continue same turn
    }

    private void HandleEndTurn(CharacterObject character)
    {
        character.ResetMovement();
        character.EnableAttack();
        Debug.Log("inside handle end turn");
        if (isManualMode)
        {
            OnTurnEnded?.Invoke();
        }
        

        TurnManager.Instance.EndTurn();
    }

    private void OnActionFinished()
    {
        // reset action mode if needed
        // (optional if agent doesn't use UI)

        if (!isManualMode)
            RequestDecision();
    }

    public void setGridData(GridData data)
    {
        _gridData = data;
    }
}
