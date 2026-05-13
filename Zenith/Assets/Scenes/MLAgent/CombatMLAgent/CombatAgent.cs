using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
using System;

public class CombatAgent : Agent, ITurnActor
{
    [Header("References")]
    [SerializeField] private MovementPreview previewSystem;
    [SerializeField] private CombatExecutor combatExecutor;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Renderer groundRenderer;

    [Header("Settings")]
    [SerializeField] private bool isManualMode = false;

    [Header("Shaped Reward Weights")]
    [SerializeField] private float damageDealtRewardScale = 0.3f;
    [SerializeField] private float damageTakenPenaltyScale = 0.15f;
    [SerializeField] private float inRangeBonus = 0.05f;

    [HideInInspector] public int CurrEp = 0;
    [HideInInspector] public float CumulativeReward = 0f;

    private Color defaultGroundColor;
    private Coroutine flashGroundCoroutine;
    private Material groundMaterial;
    private List<CharacterObject> allySlots = new();
    private List<CharacterObject> enemySlots = new();
    private int chosenActionType = 2; // default to end turn
    private int chosenTileIndex = 55; // center tile
    private int _currentAgentTeam = 2;
    private GridData _gridData;
    private int activeUnitIndex;
    private const int MapMin = -5;
    private const int MapMax = 5;
    private const int MapSize  = 10;
    private const int MapOffset =  5;
    private float _maxTurn;
    private bool hasAction = false;
    private bool hasMoved = false;
    private bool hasAttacked = false;
    private bool isTurnComplete = false;
    private List<DamageListener> damageListeners = new();

    public Action OnTurnEnded;
    public bool IsPlayer => false;
    public bool IsTurnComplete() => isTurnComplete;

    // ML Agent lifecycle

    public override void Initialize()
    {
        CurrEp = 0;
        CumulativeReward = 0f;
        if (groundRenderer != null)
        {
            groundMaterial = groundRenderer.material;
            defaultGroundColor = groundRenderer.material.color;
        }
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

        UnsubscribeAllDamageCallbacks();
        turnManager.ResetEnv();
        _maxTurn = turnManager.GetMaxTurn();
        // allySlots.Clear();
        enemySlots.Clear();
        var enemies = _gridData.GetUnitsByTeam(_currentAgentTeam == 1 ? 2 : 1);

        for (int i = 0; i < 3; i++)
            enemySlots.Add(i < enemies.Count ? enemies[i].character : null);
        SubscribeAllDamageCallbacks();
        // This code should not run (should already be handled by turn manager and turn queue)
        if (!IsValidActiveUnit())
        {
            Debug.Log("<color=red>WARNING:</color> null slot is selected, changing to another unit!");
            activeUnitIndex = allySlots.FindIndex(u => u != null);
            if (activeUnitIndex == -1) activeUnitIndex = 0;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        CharacterObject active = IsValidActiveUnit() ? allySlots[activeUnitIndex] : null;
        Vector3Int currPos = active != null ? active.Position : Vector3Int.zero;

        // GRID
        for (int x = MapMin; x < MapMax; x++)
        {
            for (int y = MapMin; y < MapMax; y++)
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
            ObserveUnit(sensor, allySlots[i], currPos);
    
        //  ENEMY UNIT DATA
        for (int i = 0; i < 3; i++)
        {
            ObserveUnit(sensor, enemySlots[i], currPos);
        }

        // WHICH UNIT IS ACTING
        for (int i = 0; i < 3; i++)
            sensor.AddObservation(i == activeUnitIndex ? 1f : 0f);

        // TURN INFO
        sensor.AddObservation(turnManager.currentTurn / _maxTurn);
        sensor.AddObservation(hasMoved    ? 1f : 0f);
        sensor.AddObservation(hasAttacked ? 1f : 0f);
        sensor.AddObservation(CountAlive(allySlots) / 3f); // num allies alive
        sensor.AddObservation(CountAlive(enemySlots) / 3f); // num enemies alive
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;

        if (!hasAction)
        {
            discrete[0] = 2; // EndTurn
            discrete[1] = GridPosToTileIndex(Vector3Int.zero);
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

        PenalizePerTurn();
        int actionType = actions.DiscreteActions[0];
        int tileIndex = actions.DiscreteActions[1]; 

        Vector3Int targetPos = TileIndexToGridPos(tileIndex);

        if (!IsValidActiveUnit())
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
                HandleMoveAction(character, currentPos, targetPos);
                break;

            case 1:
                HandleAttackAction(character, currentPos, targetPos);
                break;

            case 2:
                EndTurn(character);
                break;
            default:
                Debug.Log("DEFAULT TRIGGERED");
                break;
        }

        SurvivalBonusReward();
        
        CumulativeReward = GetCumulativeReward();
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!IsValidActiveUnit()) return;

        CharacterObject character = allySlots[activeUnitIndex];
        if (character == null || character.HP <= 0)
        { 
            Debug.LogWarning("null or dead character is selected");
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
                validTiles.Add(GridPosToTileIndex(pos));
        }
    
        if (canAttack)
        {
            foreach (Vector3Int pos in attackable)
                validTiles.Add(GridPosToTileIndex(pos));
        }
    
        // ALWAYS include current position as fallback
        validTiles.Add(GridPosToTileIndex(currentPos));
    
        // Apply mask
        int totalTiles = MapSize * MapSize;
        for (int i = 0; i < totalTiles; i++)
        {
            if (!validTiles.Contains(i))
                actionMask.SetActionEnabled(1, i, false);
        }
    }

    // Turn management

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

    public void ForceComplete() => isTurnComplete = true;
    public void EndTurn() {}

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

    // Private helpers

    private void HandleMoveAction(CharacterObject character, Vector3Int currentPos, Vector3Int targetPos)
    {
        if (!previewSystem.ComputeReachableTiles(currentPos, character.RemainingMoveRange).Contains(targetPos))
        {
            PenalizeInvalidAction();
            RequestDecision();
            return;
        }
        // RewardValidAction();
        hasMoved = true;
        combatExecutor.ExecuteMove(character, currentPos, targetPos, _gridData);
        StartCoroutine(WaitForMoveThenDecideOrEnd(character));
    }

    private void HandleAttackAction(CharacterObject character, Vector3Int currentPos, Vector3Int targetPos)
    {
        if (!previewSystem.ComputeAttackableTiles(currentPos, character.AtkRange).Contains(targetPos))
        {
            PenalizeInvalidAction();
            RequestDecision();
            return;
        }
        // RewardValidAction();
        hasAttacked = true;
        combatExecutor.ExecuteAttack(character, currentPos, targetPos, _gridData);
        DecideOrEnd(character);
    }

    private IEnumerator WaitForMoveThenDecideOrEnd(CharacterObject character)
    {
        while (combatExecutor.IsMoving)
            yield return null;
        
        if (turnManager.TurnAlreadyEnded) yield break;

        DecideOrEnd(character);
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

        GivePositioningReward(character);
        turnManager.TurnAlreadyEnded = true;
        character.ResetMovement();
        character.EnableAttack();
        hasMoved = false;
        hasAttacked = false;
        if (isManualMode)
            OnTurnEnded?.Invoke();
        isTurnComplete = true;
    }

    private void DecideOrEnd(CharacterObject character)
    {
        if (turnManager.TurnAlreadyEnded) return;
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

    // Reward helpers

    private void GivePositioningReward(CharacterObject character)
    {
        Vector3Int pos = character.Position;

        foreach (var enemy in enemySlots)
        {
            if (enemy == null || enemy.HP <= 0) continue;

            int dist = Mathf.Abs(pos.x - enemy.Position.x) +
                    Mathf.Abs(pos.y - enemy.Position.y);

            if (dist <= character.AtkRange)
            {
                AddReward(inRangeBonus);
                return;
            }
        }
    }

    private void SurvivalBonusReward()
    {
        int totalAliveAllies = CountAlive(allySlots);
        AddReward(0.005f * totalAliveAllies);
    }
    private void PenalizePerTurn() => AddReward(-0.005f);
    private void PenalizeInvalidAction() => AddReward(-0.1f);
    private void RewardValidAction() => AddReward(0.02f);

    // Observation helpers

    private void ObserveUnit(VectorSensor sensor, CharacterObject unit, Vector3Int relativeTo)
    {
        if (unit == null)
        {
            // Padding if fewer units
            for (int i = 0; i < 9; i++)
                sensor.AddObservation(0f);
            return;
        }

        sensor.AddObservation((float)unit.HP / unit.MaxHp);
        sensor.AddObservation((float)unit.Damage / 20f);
        sensor.AddObservation((float)unit.Defense / 20f);
        sensor.AddObservation((float)unit.AtkRange / 5f);
        sensor.AddObservation((float)unit.RemainingMoveRange / unit.MaxMoveRange);
        sensor.AddObservation(unit.CurrentATB / 100f);
        sensor.AddObservation(unit.Speed / 20f);
        // Position is relative to the active unit so the agent learns spatial reasoning
        sensor.AddObservation((unit.Position.x - relativeTo.x) / 10f);
        sensor.AddObservation((unit.Position.y - relativeTo.y) / 10f);
    }

    private int CountAlive(List<CharacterObject> slots)
    {
        int alive = 0;
        foreach (var unit in slots)
            if (unit != null && unit.HP > 0) alive++;
        return alive;
    }

    // Coordinate encoding

    private static int GridPosToTileIndex(Vector3Int pos) =>
        (pos.y + MapOffset) * MapSize + (pos.x + MapOffset);

    private static Vector3Int TileIndexToGridPos(int index) =>
        new Vector3Int(
            (index % MapSize) - MapOffset,
            (index / MapSize) - MapOffset,
            0);

    // Validity check

    private bool IsValidActiveUnit() =>
        activeUnitIndex >= 0 &&
        activeUnitIndex < allySlots.Count &&
        allySlots[activeUnitIndex] != null;

    // Visual feedback

    private IEnumerator FlashGround(Color targetColor, float duration)
    {
        float elapsedTime = 0f;
        groundMaterial.color = targetColor;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            groundMaterial.color = Color.Lerp(targetColor, defaultGroundColor, elapsedTime / duration);
            yield return null;
        }
        groundMaterial.color = defaultGroundColor;
    }

    private void SubscribeAllDamageCallbacks()
    {
        foreach (var enemy in enemySlots)
        {
            if (enemy == null) continue;
            var listener = new DamageListener(this, enemy, isEnemy: true);
            damageListeners.Add(listener);
            enemy.OnTakenDamage += listener.OnDamage;
        }

        foreach (var ally in allySlots)
        {
            if (ally == null) continue;
            var listener = new DamageListener(this, ally, isEnemy: false);
            damageListeners.Add(listener);
            ally.OnTakenDamage += listener.OnDamage;
        }
    }

    private void UnsubscribeAllDamageCallbacks()
    {
        foreach (var listener in damageListeners)
        {
            if (listener.character != null)
                listener.character.OnTakenDamage -= listener.OnDamage;
        }
        damageListeners.Clear();
    }

    private void OnEnemyTookDamage(int finalDamage, CharacterObject enemy)
    {
        if (enemy.MaxHp <= 0) return;
        float normalised = (float)finalDamage / enemy.MaxHp;
        AddReward(normalised * damageDealtRewardScale);
    }

    private void OnAllyTookDamage(int finalDamage, CharacterObject ally)
    {
        if (ally.MaxHp <= 0) return;
        float normalised = (float)finalDamage / ally.MaxHp;
        AddReward(-(normalised * damageTakenPenaltyScale));
    }

    private class DamageListener
    {
        private readonly CombatAgent agent;
        public readonly CharacterObject character;
        private readonly bool isEnemy;

        public DamageListener(CombatAgent agent, CharacterObject character, bool isEnemy)
        {
            this.agent     = agent;
            this.character = character;
            this.isEnemy   = isEnemy;
        }
        public void OnDamage(int finalDamage)
        {
            if (isEnemy)
                agent.OnEnemyTookDamage(finalDamage, character);
            else
                agent.OnAllyTookDamage(finalDamage, character);
        }
    }
}
