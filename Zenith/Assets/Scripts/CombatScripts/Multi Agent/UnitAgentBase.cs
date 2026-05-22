using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
using System;

public class UnitAgentBase : Agent, ITurnActor, IObservableAgent
{
    protected MovementPreview previewSystem;
    protected CombatExecutor combatExecutor;
    protected TurnManager turnManager;

    [Header("Settings")]
    [SerializeField] private bool isBTRecordingMode = false;

    [Header("Stat Normalization Ceilings")]
    [SerializeField] private float maxPossibleHP = 215f;
    [SerializeField] private float maxPossibleDamage = 55f;
    [SerializeField] private float maxPossibleDef = 20f;
    [SerializeField] private float maxPossibleRange = 3f;

    protected const int MapMin = -5;
    protected const int MapMax =  5;
    protected const int MapSize = 10;
    protected const int MapOffset =  5;
    protected const int TileActionCount = MapSize * MapSize;
    protected const int NoOpAction = 0;
    protected const int MoveOffset = 1;
    protected const int AttackOffset = TileActionCount + 1;
    protected const int EndTurnAction = (TileActionCount * 2) + 1;
    protected const int NumUnitTypes = 6;
    protected const int ObsPerUnit = 12;
    protected CharacterObject myCharacter;
    protected List<CharacterObject> teammateSlots = new(2);
    protected List<CharacterObject> enemySlots = new(3);

    private int _currentAgentTeam = 2;
    private GridData _gridData;
    private float _maxTurn;
    private bool hasMoved = false;
    private bool hasAttacked = false;
    private bool isTurnComplete = false;
    private bool isDeadThisEpisode = false;
    private bool btActionFullyProcessed = true;
    private Action onBTActionComplete;
    private bool hasAction       = false;
    private int  chosenActionType = 2;
    private int  chosenTileIndex  = 55;
    private List<DamageListener> damageListeners = new();

    [HideInInspector] public float CumulativeReward = 0f;
    public bool IsPlayer => false;
    public bool IsTurnComplete() => isTurnComplete;
    public bool IsBTActionFullyProcessed() => btActionFullyProcessed;
    public bool IsDeadThisEpisode => isDeadThisEpisode;

    // REWARD WEIGHT PROPERTIES
    protected virtual float InRangeBonus => 0.002f;
    protected virtual float UnitKillReward => 0.25f;
    protected virtual float UnitDeathPenalty => 0.25f;
    protected virtual float VictoryReward => 1f;
    protected virtual float DefeatPenalty => 1f; // applied as negative
    protected virtual float DamageDealtBase => 0.01f;
    protected virtual float DamageDealtPowerScale => 0.02f;
    protected virtual float DamageTakenBase => 0.01f;
    protected virtual float DamageTakenPowerScale => 0.02f;

    // ML AGENT LIFECYCLE
    public override void Initialize()
    {
        previewSystem = GetComponentInParent<MovementPreview>();
        combatExecutor = GetComponentInParent<CombatExecutor>();
        turnManager = GetComponentInParent<TurnManager>();

        if (previewSystem == null) Debug.LogError($"[{name}] UnitAgentBase: MovementPreview not found in parent!");
        if (combatExecutor == null) Debug.LogError($"[{name}] UnitAgentBase: CombatExecutor not found in parent!");
        if (turnManager == null) Debug.LogError($"[{name}] UnitAgentBase: TurnManager not found in parent!");
    }

    public override void OnEpisodeBegin()
    {
        hasMoved = false;
        hasAttacked  = false;
        isTurnComplete = false;
        isDeadThisEpisode = false;
        btActionFullyProcessed = true;
        hasAction = false;
        chosenActionType = 2;
        chosenTileIndex = GridPosToTileIndex(Vector3Int.zero);

        CumulativeReward = 0f;
        UnsubscribeAllDamageCallbacks();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Total obs count must be constant regardless of runtime state.
        // Grid: MapSize*MapSize = 100
        // Self: ObsPerUnit = 12
        // Teammates: 2 * ObsPerUnit = 24
        // Enemies: 3 * ObsPerUnit = 36
        // Has-moved, has-attacked, turn progress, allies alive, enemies alive: 5
        // Total: 177

        if (_gridData == null || myCharacter == null || isDeadThisEpisode)
        {
            int totalObs = (MapSize * MapSize) + ObsPerUnit + (2 * ObsPerUnit) + (3 * ObsPerUnit) + 5;
            for (int i = 0; i < totalObs; i++)
                sensor.AddObservation(0f);
            return;
        }

        Vector3Int selfPos = myCharacter.Position;

        for (int x = MapMin; x < MapMax; x++)
        {
            for (int y = MapMin; y < MapMax; y++)
            {
                Vector3Int pos  = new Vector3Int(x, y, 0);
                TileData   tile = _gridData.GetTileAt(pos);
                float occupation;

                if (tile == null)
                    occupation = 0f;
                else if (tile.PlacedObject is CharacterObject ch)
                    occupation = (ch.Team == _currentAgentTeam) ? 2f : 3f;
                else
                    occupation = 1f;

                sensor.AddObservation(occupation / 3f);
            }
        }

        ObserveAlly(sensor, myCharacter, selfPos);

        for (int i = 0; i < 2; i++)
            ObserveAlly(sensor, i < teammateSlots.Count ? teammateSlots[i] : null, selfPos);

        for (int i = 0; i < 3; i++)
            ObserveEnemy(sensor, i < enemySlots.Count ? enemySlots[i] : null, selfPos);

        sensor.AddObservation(turnManager.currentTurn / _maxTurn);
        sensor.AddObservation(hasMoved    ? 1f : 0f);
        sensor.AddObservation(hasAttacked ? 1f : 0f);
        sensor.AddObservation(CountAlive(teammateSlots, myCharacter) / 2f);
        sensor.AddObservation(CountAlive(enemySlots, null)           / 3f);
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
        hasAction   = false;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (isDeadThisEpisode)
            return;

        if (myCharacter == null)
        {
            Debug.LogWarning($"[{name}] OnActionReceived: myCharacter is null!");
            ForceComplete();
            return;
        }

        CharacterObject current = turnManager.turnQueue.GetCurrent();

        if (current == null || current != myCharacter)
        {
            Debug.LogWarning($"[{name}] Ignoring action — not this unit's turn.");
            return;
        }

        if (turnManager.TurnAlreadyEnded)
        {
            Debug.LogWarning($"[{name}] Ignoring action — turn already ended.");
            return;
        }

        int encodedAction = actions.DiscreteActions[0];

        if (encodedAction == NoOpAction && isBTRecordingMode && !IsBTActionFullyProcessed())
            return;

        DecodeAction(encodedAction, out int actionType, out int tileIndex);
        Vector3Int targetPos = TileIndexToGridPos(tileIndex);
        Vector3Int currentPos = myCharacter.Position;

        switch (actionType)
        {
            case 0: HandleMoveAction(myCharacter, currentPos, targetPos); break;
            case 1: HandleAttackAction(myCharacter, currentPos, targetPos); break;
            case 2: EndTurn(myCharacter); break;
            case 3:
                if (isBTRecordingMode)
                {
                    btActionFullyProcessed = true;
                    NotifyBTActionComplete();
                }
                else
                {
                    RequestDecision();
                }
                break;
            default:
                Debug.LogWarning($"[{name}] Unknown action type {actionType}");
                break;
        }

        CumulativeReward = GetCumulativeReward();
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (isDeadThisEpisode || myCharacter == null || myCharacter.HP <= 0)
        {
            for (int i = 1; i <= EndTurnAction; i++)
                actionMask.SetActionEnabled(0, i, false);
            return;
        }

        Vector3Int currentPos = myCharacter.Position;
        HashSet<Vector3Int> reachable = previewSystem.ComputeReachableTiles(currentPos, myCharacter.RemainingMoveRange);
        HashSet<Vector3Int> attackable = previewSystem.ComputeAttackableTiles(currentPos, myCharacter.AtkRange);

        for (int i = 0; i <= EndTurnAction; i++)
            actionMask.SetActionEnabled(0, i, false);

        actionMask.SetActionEnabled(0, NoOpAction, false);

        if (!hasMoved)
        {
            foreach (Vector3Int pos in reachable)
                actionMask.SetActionEnabled(0, MoveOffset + GridPosToTileIndex(pos), true);
        }

        if (!hasAttacked)
        {
            foreach (Vector3Int pos in attackable)
                actionMask.SetActionEnabled(0, AttackOffset + GridPosToTileIndex(pos), true);
        }

        actionMask.SetActionEnabled(0, EndTurnAction, true);
    }

    // ITURNACTOR IMPLEMENTATIONS
    public void BeginTurn(GridData gridData, CharacterObject character)
    {
        if (character != myCharacter)
        {
            Debug.LogError($"[{name}] BeginTurn called for wrong character: {character?.Name}");
            return;
        }

        _gridData = gridData;
        _maxTurn = turnManager.GetMaxTurn();

        isTurnComplete = false;
        hasMoved = false;
        hasAttacked = false;
        hasAction = false;
        chosenActionType = 2;
        chosenTileIndex  = GridPosToTileIndex(Vector3Int.zero);

        if (!isBTRecordingMode) RequestDecision();
    }
    public void EndTurn() { /* Required by ITurnActor, intentionally empty. */ }

    public void ForceComplete() => isTurnComplete = true;

    // PUBLIC API
    /// <summary>
    /// Called by TurnManager in MultiAgent mode
    /// </summary>
    /// <param name="character"></param>
    /// <param name="agentTeam"></param>
    /// <param name="teammates"></param>
    /// <param name="enemies"></param>
    /// <param name="gridData"></param>
    public void NotifyUnitAssignment(CharacterObject character, int agentTeam,
                                    List<CharacterObject> teammates,
                                    List<CharacterObject> enemies,
                                    GridData gridData)
    {
        myCharacter = character;
        _currentAgentTeam = agentTeam;
        _gridData = gridData;
        _maxTurn = turnManager != null ? turnManager.GetMaxTurn() : 200f;

        teammateSlots.Clear();
        teammateSlots.AddRange(teammates);

        enemySlots.Clear();
        enemySlots.AddRange(enemies);

        UnsubscribeAllDamageCallbacks();
        SubscribeAllDamageCallbacks();
    }

    public void NotifyUnitDied()
    {
        isDeadThisEpisode = true;
        isTurnComplete    = true; // so TurnManager doesn't wait on us
        UnsubscribeAllDamageCallbacks();
    }

    // BT RECORDING INTERFACE
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

    // REWARD EVENTS (CALLED BY BATTLERESULTHANDLER)
    public void OnVictory()
    {
        AddReward(VictoryReward);
    }

    public void OnDefeat()
    {
        int deadEnemies = CountDead(enemySlots);
        float partialCredit = deadEnemies * (1f / 3f);
        AddReward(-DefeatPenalty + partialCredit);
    }

    public void OnGlobalTurnEnd() => AddReward(-0.002f);

    public void OnUnitKilled(CharacterObject unit)
    {
        if (unit.Team == _currentAgentTeam)
            AddReward(-UnitDeathPenalty);
        else
            AddReward(+UnitKillReward);
    }

    public void SetGridData(GridData data) => _gridData = data;
    public void SetAgentTeam(int team) => _currentAgentTeam = team;
    public bool GetIsBTRecordingMode() => isBTRecordingMode;

    // PRIVATE ACTION HANDLING
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
            else
                RequestDecision();
            return;
        }

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
            else
                RequestDecision();
            return;
        }

        hasAttacked = true;
        combatExecutor.ExecuteAttack(character, currentPos, targetPos, _gridData);

        if (isBTRecordingMode)
        {
            StartCoroutine(WaitForAttackThenNotifyBT());
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

    private IEnumerator WaitForAttackThenNotifyBT()
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
            Debug.LogWarning($"[{name}] HandleEndTurn called twice!");
            return;
        }

        if (turnManager.turnQueue.GetCurrent() != character)
        {
            Debug.LogError($"[{name}] EndTurn called for non-current unit: {character.Name}");
            return;
        }

        GivePositioningReward(character);
        turnManager.TurnAlreadyEnded = true;
        character.ResetMovement();
        character.EnableAttack();
        hasMoved = false;
        hasAttacked = false;
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

        if (!canMove && !canAttack)
            EndTurn(character);
        else
            RequestDecision();
    }

    // PRIVATE ACTION ENCODING
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

    // PRIVATE REWARDS
    private void GivePositioningReward(CharacterObject character)
    {
        Vector3Int pos = character.Position;
        foreach (var enemy in enemySlots)
        {
            if (enemy == null || enemy.HP <= 0) continue;
            int dist = Mathf.Abs(pos.x - enemy.Position.x) + Mathf.Abs(pos.y - enemy.Position.y);
            if (dist <= character.AtkRange)
            {
                AddReward(InRangeBonus);
                return;
            }
        }
    }

    private void PenalizeInvalidAction() => AddReward(-0.01f);

    private void OnEnemyTookDamage(int finalDamage, CharacterObject enemy)
    {
        if (enemy.MaxHp <= 0) return;
        float power = ComputeUnitPower(enemy);
        AddReward(DamageDealtBase + (DamageDealtPowerScale * power));
    }

    private void OnAllyTookDamage(int finalDamage, CharacterObject ally)
    {
        if (ally.MaxHp <= 0) return;
        float power = ComputeUnitPower(ally);
        AddReward(-DamageTakenBase + (-DamageTakenPowerScale * power));
    }

    // PRIVATE OBSERVATIONS
    private void ObserveAlly(VectorSensor sensor, CharacterObject unit, Vector3Int relativeTo)
    {
        if (unit == null || unit.HP <= 0)
        {
            for (int i = 0; i < ObsPerUnit; i++) sensor.AddObservation(0f);
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
            for (int i = 0; i < ObsPerUnit; i++) sensor.AddObservation(0f);
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

    // PRIVATE HELPERS
    private float ComputeUnitPower(CharacterObject unit)
    {
        return (unit.MaxHp / maxPossibleHP) * 0.4f
             + (unit.Damage / maxPossibleDamage) * 0.4f
             + (unit.Defense / maxPossibleDef) * 0.2f;
    }

    private int CountAlive(List<CharacterObject> slots, CharacterObject exclude)
    {
        int alive = 0;
        foreach (var unit in slots)
        {
            if (unit == null || unit == exclude) continue;
            if (unit.HP > 0) alive++;
        }
        return alive;
    }

    private int CountDead(List<CharacterObject> slots)
    {
        int dead = 0;
        foreach (var unit in slots)
            if (unit != null && unit.HP <= 0) dead++;
        return dead;
    }

    protected static int GridPosToTileIndex(Vector3Int pos) =>
        (pos.y + MapOffset) * MapSize + (pos.x + MapOffset);

    protected static Vector3Int TileIndexToGridPos(int index) =>
        new Vector3Int((index % MapSize) - MapOffset, (index / MapSize) - MapOffset, 0);

    // DAMAGE LISTENER BOOKKEEPING
    private void SubscribeAllDamageCallbacks()
    {
        foreach (var enemy in enemySlots)
        {
            if (enemy == null) continue;
            var listener = new DamageListener(this, enemy, isEnemy: true);
            damageListeners.Add(listener);
            enemy.OnTakenDamage += listener.OnDamage;
        }

        if (myCharacter != null)
        {
            var selfListener = new DamageListener(this, myCharacter, isEnemy: false);
            damageListeners.Add(selfListener);
            myCharacter.OnTakenDamage += selfListener.OnDamage;
        }

        foreach (var ally in teammateSlots)
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

    private class DamageListener
    {
        private readonly UnitAgentBase agent;
        public  readonly CharacterObject character;
        private readonly bool isEnemy;

        public DamageListener(UnitAgentBase agent, CharacterObject character, bool isEnemy)
        {
            this.agent = agent;
            this.character = character;
            this.isEnemy = isEnemy;
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
