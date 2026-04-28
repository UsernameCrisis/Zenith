using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
using System;

public class CombatAgent : Agent, ITurnActor
{
    [SerializeField] private bool isManualMode = false;
    [SerializeField] private MovementPreview previewSystem;
    [SerializeField] private CombatExecutor combatExecutor;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Renderer groundRenderer;

    [HideInInspector] public int CurrEp = 0;
    [HideInInspector] public float CumulativeReward = 0f;

    private Color defaultGroundColor;
    private Coroutine flashGroundCoroutine;
    private List<CharacterObject> allySlots = new();
    private List<CharacterObject> enemySlots = new();
    private int chosenActionType = 2;
    private int chosenTileIndex = 55;
    private int _currentAgentTeam = 1;
    private GridData _gridData;
    private int activeUnitIndex;
    private int mapMin = -5;
    private int mapMax = 5;
    private float _maxTurn;
    private bool hasAction = false;
    private bool hasMoved;
    private bool hasAttacked;
    private bool isTurnComplete = false;

    public Action OnTurnEnded;
    public bool IsPlayer => false;
    public bool IsTurnComplete() => isTurnComplete;

    public override void Initialize()
    {
        CurrEp = 0;
        CumulativeReward = 0f;
        if (groundRenderer != null)
            defaultGroundColor = groundRenderer.material.color;
    }

    public void BeginTurn(GridData gridData, CharacterObject character)
    {
        _gridData = gridData;
        BeginTurn();
    }

    public void BeginTurn()
    {
        isTurnComplete = false;
        hasMoved = false;
        hasAttacked = false;

        RequestDecision();
    }

    public override void OnEpisodeBegin()
    {
        if (groundRenderer != null && CumulativeReward != 0f)
        {
            Color flashColor = (CumulativeReward > 0f) ? Color.green : Color.red;

            if (flashGroundCoroutine != null)
                StopCoroutine(flashGroundCoroutine);

            flashGroundCoroutine = StartCoroutine(FlashGround(flashColor, 3f));
        }
        CurrEp++;
        CumulativeReward = 0f;
        turnManager.ResetEnv();
        _maxTurn = turnManager.GetMaxTurn();
        // allySlots.Clear();
        enemySlots.Clear();

        if (activeUnitIndex < 0 || activeUnitIndex >= allySlots.Count || allySlots[activeUnitIndex] == null)
        {
            activeUnitIndex = allySlots.FindIndex(u => u != null);
            Debug.Log("<color=red>WARNING:</color> null slot is selected, changing to another unit!");
            if (activeUnitIndex == -1)
                activeUnitIndex = 0;
        }

        var enemies = _gridData.GetUnitsByTeam(_currentAgentTeam == 1 ? 2 : 1);

        for (int i = 0; i < 3; i++)
            enemySlots.Add(i < enemies.Count ? enemies[i].character : null);

        // this code should not run (should already be handled by turn manager and turn queue)
        if (allySlots[activeUnitIndex] == null)
        {
            Debug.Log("<color=red>WARNING:</color> null slot is selected, changing to another unit!");
            activeUnitIndex = allySlots.FindIndex(u => u != null);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CharacterObject active = null;

        if (activeUnitIndex >= 0 && activeUnitIndex < allySlots.Count)
            active = allySlots[activeUnitIndex];

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
        sensor.AddObservation(turnManager.currentTurn / _maxTurn);

        sensor.AddObservation(GetAliveAllies() / 3f); // num allies alive
        sensor.AddObservation(GetAliveEnemies() / 3f); // num enemies alive
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
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
        CharacterObject current = turnManager.turnQueue.GetCurrent();

        if (current == null || current.Team != _currentAgentTeam)
        {
            Debug.LogWarning("Ignoring action (not agent turn)");
            return;
        }

        if (turnManager.TurnAlreadyEnded)
        {
            Debug.LogWarning("Ignoring action (turn already ended)");
            return;
        }

        AddReward(-0.01f);
        int actionType = actions.DiscreteActions[0];
        int tileIndex = actions.DiscreteActions[1]; 

        // Konversi dari tile index kembali ke koord grid
        int x = (tileIndex % 10) - 5; // -5 karena range x dan y -5 hingga 4
        int y = (tileIndex / 10) - 5;
        Vector3Int targetPos = new Vector3Int(x, y, 0);

        if (activeUnitIndex < 0 || activeUnitIndex >= allySlots.Count)
        {
            Debug.LogError("Invalid active unit index!");
            ForceComplete();
            return;
        }

        CharacterObject character = allySlots[activeUnitIndex];
        Vector3Int currentPos = character.Position;

        switch (actionType)
        {
            case 0:
                if (!previewSystem.ComputeReachableTiles(currentPos, character.RemainingMoveRange).Contains(targetPos))
                {
                    AddReward(-0.1f);
                    RequestDecision();
                    return;
                }
                AddReward(0.05f);
                hasMoved = true;
                combatExecutor.ExecuteMove(character, currentPos, targetPos, _gridData);
                StartCoroutine(WaitForMoveThenDecideOrEnd(character));
                break;

            case 1:
                if (!previewSystem.ComputeAttackableTiles(currentPos, character.AtkRange).Contains(targetPos))
                {
                    AddReward(-0.1f);
                    RequestDecision();
                    return;
                }
                AddReward(0.05f);
                hasAttacked = true;
                combatExecutor.ExecuteAttack(character, currentPos, targetPos, _gridData);
                DecideOrEnd(character);
                break;

            case 2:
                EndTurn(character);
                break;
            default:
                Debug.Log("DEFAULT TRIGGERED");
                break;
        }
        
        CumulativeReward = GetCumulativeReward();
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (activeUnitIndex < 0 || activeUnitIndex >= allySlots.Count)
            return;

        CharacterObject character = allySlots[activeUnitIndex];
        if (character == null || character.HP <= 0)
        { 
            print("null or dead character is selected");
            return;
        }

        Vector3Int currentPos = character.Position;

        HashSet<Vector3Int> reachable = previewSystem.ComputeReachableTiles(currentPos, character.RemainingMoveRange);
        HashSet<Vector3Int> attackable = previewSystem.ComputeAttackableTiles(currentPos, character.AtkRange);

        // MASK ACTION TYPE
        bool canMove = reachable.Count > 0 && !hasMoved;
        bool canAttack = attackable.Count > 0 && !hasAttacked;

        if (!canMove)
            actionMask.SetActionEnabled(0, 0, false); // disable MOVE

        if (!canAttack)
            actionMask.SetActionEnabled(0, 1, false); // disable ATTACK

        // MASK X/Y
        // Build allowed positions
        HashSet<int> validTiles = new();

        if (canMove)
        {
            foreach (Vector3Int pos in reachable)
                validTiles.Add((pos.y + 5) * 10 + pos.x + 5);
        }
    
        if (canAttack)
        {
            foreach (Vector3Int pos in attackable)
                validTiles.Add((pos.y + 5) * 10 + pos.x + 5);
        }
    
        // ALWAYS include current position as fallback
        validTiles.Add((currentPos.y + 5) * 10 + currentPos.x + 5);
    
        // Apply mask
        for (int i = 0; i < 100; i++)
        {
            if (!validTiles.Contains(i))
                actionMask.SetActionEnabled(1, i, false);
        }
    }

    // Public API
    public void SetManualAction(int actionType, Vector3Int targetPos)
    {
        chosenActionType = actionType;
        chosenTileIndex = (targetPos.y + 5) * 10 + targetPos.x + 5;
        hasAction = true;

        RequestDecision();
    }

    public void SetAgentTeam(int team) => _currentAgentTeam = team;
    public void SetActiveUnitIndex(int index) => activeUnitIndex = index;
    public void SetAllySlots(List<CharacterObject> slots) => allySlots = slots;
    public void SetGridData(GridData data) => _gridData = data;
    public bool GetIsManualMode() => isManualMode;
    public void ForceComplete() => isTurnComplete = true;
    public void EndTurn() {}

    // Private helpers

    private IEnumerator WaitForMoveThenDecideOrEnd(CharacterObject character)
    {
        while (combatExecutor.IsMoving)
            yield return null;

        DecideOrEnd(character);
    }

    private IEnumerator FlashGround(Color targetColor, float duration)
    {
        float elapsedTime = 0f;

        groundRenderer.material.color = targetColor;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            groundRenderer.material.color = Color.Lerp(targetColor, defaultGroundColor, elapsedTime / duration);
            yield return null;
        }
    }
    private void EndTurn(CharacterObject character)
    {
        if (turnManager.TurnAlreadyEnded)
        {
            Debug.LogWarning("HandleEndTurn called twice!");
            return;
        }

        if (turnManager.turnQueue.GetCurrent() != character)
        {
            Debug.LogError($"EndTurn called for non-current unit: {character.Name}");
            return;
        }
        turnManager.TurnAlreadyEnded = true;
        character.ResetMovement();
        character.EnableAttack();
        hasMoved = false;
        hasAttacked = false;
        Debug.Log("Agent turn finished");
        if (isManualMode)
            OnTurnEnded?.Invoke();
        isTurnComplete = true;
    }

    private void DecideOrEnd(CharacterObject character)
    {
        if (turnManager.TurnAlreadyEnded)
            return;
        if (character == null || character.HP <= 0)
        {
            ForceComplete();
            return;
        }
        bool canMove = !hasMoved && character.RemainingMoveRange > 0;
        bool canAttack = !hasAttacked;

        if (!isManualMode)
        {
            if (!canMove && !canAttack)
                EndTurn(character);
            else
                RequestDecision();
        }
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

    private bool IsValidUnit(int index)
    {
        return index >= 0 && index < allySlots.Count &&
                allySlots[index] != null && allySlots[index].HP > 0;
    }


}
