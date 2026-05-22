using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
using System;

public class CombatAgent2 : Agent, ITurnActor, IObservableAgent
{
    [Header("References")]
    [SerializeField] private MovementPreview previewSystem;
    [SerializeField] private CombatExecutor combatExecutor;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Renderer groundRenderer;

    [Header("Settings")]
    [SerializeField] private bool isManualMode = false;
    [SerializeField] private bool isBTRecordingMode = false;
    [SerializeField] private bool isRecording = false;

    [Header("Shaped Reward Weights")]
    [SerializeField] private float inRangeBonus = 0.002f;

    [Header("Stat Normalization Ceilings")]
    [SerializeField] private float maxPossibleHP     = 215f;
    [SerializeField] private float maxPossibleDamage = 55f;
    [SerializeField] private float maxPossibleDef    = 20f;
    [SerializeField] private float maxPossibleRange  = 3f;

    private const int NumUnitTypes = 6;
    private const int ObsPerUnit = 12;

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
    private const int TileActionCount = MapSize * MapSize;
    private const int NoOpAction    = 0;
    private const int MoveOffset   = 1;
    private const int AttackOffset = TileActionCount + 1;
    private const int EndTurnAction = (TileActionCount * 2) + 1;
    private float _maxTurn;
    private bool hasAction = false;
    private bool hasMoved = false;
    private bool hasAttacked = false;
    private bool isTurnComplete = false;
    private bool btActionFullyProcessed = true;
    public bool IsBTActionFullyProcessed() => btActionFullyProcessed;
    private List<DamageListener> damageListeners = new();
    private float episodeDifficultyScore = 0f;
    private StatsRecorder statsRecorder;

    public Action OnTurnEnded;
    private Action onBTActionComplete;
    public bool IsPlayer => false;
    public bool IsTurnComplete() => isTurnComplete;

    // ML Agent lifecycle

    public override void Initialize()
    {
        CurrEp = 0;
        CumulativeReward = 0f;
        statsRecorder = Academy.Instance.StatsRecorder;
        if (groundRenderer != null)
        {
            groundMaterial = groundRenderer.material;
            defaultGroundColor = groundRenderer.material.color;
        }
    }

    public override void OnEpisodeBegin()
    {
        if (!enabled) return;

        hasMoved = false;
        hasAttacked = false;
        isTurnComplete = false;
        btActionFullyProcessed = true;
        chosenActionType = 2;
        chosenTileIndex = GridPosToTileIndex(Vector3Int.zero);
        hasAction = false;
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

        episodeDifficultyScore = ComputeDifficultyScore();
        statsRecorder.Add("Environment/EpisodeDifficulty", episodeDifficultyScore);
        statsRecorder.Add("Environment/HardEpisodeRate", episodeDifficultyScore > 0.7f ? 1f : 0f);

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
        if (_gridData == null)
        {
            // Pad with zeros so the observation vector size stays consistent
            int totalObs = (MapSize * MapSize) + (3 * ObsPerUnit) + (3 * ObsPerUnit) + 3 + 5;
            for (int i = 0; i < totalObs; i++)
                sensor.AddObservation(0f);
            return;
        }

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
            ObserveAlly(sensor, allySlots[i], currPos);
    
        //  ENEMY UNIT DATA
        for (int i = 0; i < 3; i++)
        {
            ObserveEnemy(sensor, enemySlots[i], currPos);
        }

        // WHICH UNIT IS ACTING
        for (int i = 0; i < 3; i++)
            sensor.AddObservation(i == activeUnitIndex ? 1f : 0f);

        // TURN INFO
        sensor.AddObservation(turnManager.currentTurn / _maxTurn);
        sensor.AddObservation(hasMoved ? 1f : 0f);
        sensor.AddObservation(hasAttacked ? 1f : 0f);
        sensor.AddObservation(CountAlive(allySlots) / 3f); // num allies alive
        sensor.AddObservation(CountAlive(enemySlots) / 3f); // num enemies alive
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;

        if (!hasAction)
        {
            discrete[0] = EndTurnAction;
            return;
        }

        discrete[0] = EncodeAction(chosenActionType, chosenTileIndex);
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

        // PenalizePerTurn();
        int encodedAction = actions.DiscreteActions[0];

        if (encodedAction == 0 && isBTRecordingMode && !IsBTActionFullyProcessed())
        {
            return;
        }

        DecodeAction(encodedAction,out int actionType,out int tileIndex);

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
            
            case 3:
                if (isBTRecordingMode)
                {
                    btActionFullyProcessed = true;
                    NotifyBTActionComplete();
                }
                else if (!isManualMode)
                {
                    RequestDecision();
                }
                break;
            default:
                Debug.Log("DEFAULT TRIGGERED");
                break;
        }

        // SurvivalBonusReward();
        
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

        for (int i = 0; i <= EndTurnAction; i++)
            actionMask.SetActionEnabled(0, i, false); // disables all action

        actionMask.SetActionEnabled(0, NoOpAction, false);

        // Enable move actions
        if (!hasMoved)
        {
            foreach (Vector3Int pos in reachable)
            {
                int tile = GridPosToTileIndex(pos);
                actionMask.SetActionEnabled(0, MoveOffset + tile, true);
            }
        }

        // Enable attack actions
        if (!hasAttacked)
        {
            foreach (Vector3Int pos in attackable)
            {
                int tile = GridPosToTileIndex(pos);
                actionMask.SetActionEnabled(0, AttackOffset + tile, true);
            }
        }

        // Always allow end turn
        actionMask.SetActionEnabled(0, EndTurnAction, true);
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

        chosenActionType = 2;
        chosenTileIndex = GridPosToTileIndex(Vector3Int.zero); // center tile, safe fallback
        hasAction = false;

        if (!isRecording && !isManualMode && !isBTRecordingMode) RequestDecision();
    }

    public void ResetState()
    {
        StopAllCoroutines();
        if (groundRenderer != null && groundMaterial.color != defaultGroundColor)
        {
            flashGroundCoroutine = StartCoroutine(
                FlashGround(groundMaterial.color, 2f));
        }
        hasMoved = false;
        hasAttacked = false;
        chosenActionType = 2;
        chosenTileIndex = 0;
        hasAction = false;
        isTurnComplete = false;
        btActionFullyProcessed = true;
    }

    public void ForceComplete() => isTurnComplete = true;
    public void EndTurn() {}

    // Public API
    public void SetManualAction(int actionType, Vector3Int targetPos)
    {
        chosenActionType = actionType;
        chosenTileIndex = actionType == 2
            ? GridPosToTileIndex(Vector3Int.zero)
            : (targetPos.y + 5) * 10 + targetPos.x + 5;
        hasAction = true;
        btActionFullyProcessed = false;

        RequestDecision();
    }
    public void RegisterBTActionCallback(Action callback)
    {
        onBTActionComplete = callback;
    }

    public void SetAgentTeam(int team) => _currentAgentTeam = team;
    public void SetActiveUnitIndex(int index) => activeUnitIndex = index;
    public void SetAllySlots(List<CharacterObject> slots) => allySlots = slots;
    public void SetGridData(GridData data) => _gridData = data;
    public void SetHasMoved(bool value) => hasMoved = value;
    public void SetHasAttacked(bool value) => hasAttacked = value;
    public bool GetIsManualMode() => isManualMode;

    // Private helpers

    private void HandleMoveAction(CharacterObject character, Vector3Int currentPos, Vector3Int targetPos)
    {
        if (!previewSystem.ComputeReachableTiles(currentPos, character.RemainingMoveRange).Contains(targetPos))
        {
            PenalizeInvalidAction();
            if (isBTRecordingMode)
            {
                btActionFullyProcessed = true;
                NotifyBTActionComplete(); 
            }
            else if (!isManualMode)
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
            if (isBTRecordingMode)
            {
                btActionFullyProcessed = true;
                NotifyBTActionComplete();
            }
            else if (!isManualMode)
                RequestDecision();
            return;
        }
        // RewardValidAction();
        hasAttacked = true;
        combatExecutor.ExecuteAttack(character, currentPos, targetPos, _gridData);

        if (isBTRecordingMode)
        {
            StartCoroutine(WaitForAttackThenNotifyBT(character));
            return;
        }
        DecideOrEnd(character);
    }

    private IEnumerator WaitForMoveThenDecideOrEnd(CharacterObject character)
    {
        while (combatExecutor.IsMoving)
            yield return null;
        
        if (turnManager.TurnAlreadyEnded) yield break;

        if (isBTRecordingMode)
        {
            btActionFullyProcessed = true;
            NotifyBTActionComplete();
            yield break;
        }

        DecideOrEnd(character);
    }

    private IEnumerator WaitForAttackThenNotifyBT(CharacterObject character)
    {
        while (combatExecutor.IsAttacking)
            yield return null;

        if (turnManager.TurnAlreadyEnded) yield break;

        btActionFullyProcessed = true;
        NotifyBTActionComplete();
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

        if (isBTRecordingMode) return;

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

    private int EncodeAction(int actionType, int tileIndex)
    {
        return actionType switch
        {
            0 => MoveOffset + tileIndex,
            1 => AttackOffset + tileIndex,
            2 => EndTurnAction,
            _ => EndTurnAction
        };
    }

    private void DecodeAction(int action, out int actionType, out int tileIndex)
    {
        if (action == NoOpAction)
        {
            actionType = 3;
            tileIndex = 0;
        }
        else if (action < AttackOffset)
        {
            actionType = 0;
            tileIndex = action - MoveOffset;
        }
        else if (action < EndTurnAction)
        {
            actionType = 1;
            tileIndex = action - AttackOffset;
        }
        else
        {
            actionType = 2;
            tileIndex = GridPosToTileIndex(Vector3Int.zero);
        }
    }

    private void NotifyBTActionComplete()
    {
        var callback = onBTActionComplete;
        onBTActionComplete = null;
        callback?.Invoke();
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

    private void PenalizeInvalidAction() => AddReward(-0.01f);
    public void OnVictory() => AddReward(1f);
    public void OnDefeat()
    {
        float defeatPenalty = -1f;
        int deadEnemies = 3 - CountAlive(enemySlots);
        float partialCredit = deadEnemies * (1f/3f);
        AddReward(defeatPenalty + partialCredit);
    }
    public void OnGlobalTurnEnd() => AddReward(-0.002f);
    public void OnUnitKilled(CharacterObject unit)
    {
        if (unit.Team == _currentAgentTeam)
            AddReward(-0.25f);
        else
            AddReward(+0.25f);
    }

    // Observation helpers

    private void ObserveAlly(VectorSensor sensor, CharacterObject unit, Vector3Int relativeTo)
    {
        if (unit == null || unit.HP <= 0)
        {
            // Padding if fewer units
            for (int i = 0; i < ObsPerUnit; i++)
                sensor.AddObservation(0f);
            return;
        }

        int typeIndex = Mathf.Clamp(unit.ID, 0, NumUnitTypes - 1);
        for (int i = 0; i < NumUnitTypes; i++)
            sensor.AddObservation(i == typeIndex ? 1f : 0f);

        sensor.AddObservation((float)unit.HP / unit.MaxHp);
        sensor.AddObservation(unit.Damage / maxPossibleDamage);
        sensor.AddObservation(unit.Defense / maxPossibleDef);
        sensor.AddObservation(unit.AtkRange / maxPossibleRange);
        sensor.AddObservation((unit.Position.x - relativeTo.x) / 10f);
        sensor.AddObservation((unit.Position.y - relativeTo.y) / 10f);
    }

    private void ObserveEnemy(VectorSensor sensor, CharacterObject unit, Vector3Int relativeTo)
    {
        if (unit == null || unit.HP <= 0)
        {
            // Padding if fewer units
            for (int i = 0; i < ObsPerUnit; i++)
                sensor.AddObservation(0f);
            return;
        }

        int typeIndex = Mathf.Clamp(unit.ID, 0, NumUnitTypes - 1);
        for (int i = 0; i < NumUnitTypes; i++)
            sensor.AddObservation(i == typeIndex ? 1f : 0f);

        sensor.AddObservation((float)unit.HP / unit.MaxHp);
        sensor.AddObservation(unit.MaxHp / maxPossibleHP);
        sensor.AddObservation(unit.Damage / maxPossibleDamage);
        sensor.AddObservation(unit.Defense / maxPossibleDef);
        sensor.AddObservation((unit.Position.x - relativeTo.x) / 10f);
        sensor.AddObservation((unit.Position.y - relativeTo.y) / 10f);
    }

    private float ComputeDifficultyScore()
    {
        float totalPower = 0f;
        int count = 0;

        foreach (var enemy in enemySlots)
        {
            if (enemy == null) continue;

            float power = ComputeUnitPower(enemy);

            totalPower += power;
            count++;
        }

        return count > 0 ? totalPower / count : 0f;
    }

    private float ComputeUnitPower(CharacterObject unit)
    {
        return (unit.MaxHp  / maxPossibleHP)      * 0.4f
             + (unit.Damage / maxPossibleDamage)  * 0.4f
             + (unit.Defense / maxPossibleDef)    * 0.2f;
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
        // float normalised = (float)finalDamage / enemy.MaxHp;
        // AddReward(normalised * damageDealtRewardScale);
        float power = ComputeUnitPower(enemy);
        AddReward(0.01f + (0.02f * power));
    }

    private void OnAllyTookDamage(int finalDamage, CharacterObject ally)
    {
        if (ally.MaxHp <= 0) return;
        // float normalised = (float)finalDamage / ally.MaxHp;
        // AddReward(-(normalised * damageTakenPenaltyScale));
        float power = ComputeUnitPower(ally);
        AddReward(-0.01f + (-0.02f * power));
    }

    private class DamageListener
    {
        private readonly CombatAgent2 agent;
        public readonly CharacterObject character;
        private readonly bool isEnemy;

        public DamageListener(CombatAgent2 agent, CharacterObject character, bool isEnemy)
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
