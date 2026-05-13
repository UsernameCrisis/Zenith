using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CombatState
{
    Playing,
    Victory,
    Defeat
}

public enum CombatControlMode
{
    Player,
    MLAgent,
    BehaviorTree,
    PlayerVsAgent,
    EnemyPlayer,
    BTRecording,
    Demonstration
}

public class TurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private TurnOrderUI turnOrderUI;
    [SerializeField] private CombatAgent2 combatAgent;

    [Header("Settings")]
    [SerializeField] private CombatControlMode controlMode = CombatControlMode.Player;
    [SerializeField] private int currentAgentTeam = 2;
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private bool showTurnOrderUI = true;
    [SerializeField] private int demoControlledTeam = 0;

    public CombatState State { get; private set; } = CombatState.Playing;
    public TurnQueue turnQueue;
    public int currentTurn = 1;
    public bool TurnAlreadyEnded { get; set; } = false;
    public List<string> DefeatedEnemyNames => resultHandler.DefeatedEnemyNames;

    private GridData gridData;
    private BattleResultHandler resultHandler;
    private List<CharacterObject> allySlots = new List<CharacterObject>(3);
    private ITurnActor actor;
    private Coroutine turnLoopCoroutine;
    private bool isTurnRunning = false;

    void Start()
    {
        resultHandler = GetComponent<BattleResultHandler>();
        resultHandler.OnCharacterDied += HandleCharacterDied;
        resultHandler.OnEnvReset += HandleEnvReset;
        ResetEnv();
    }

    void OnDestroy()
    {
        if (resultHandler == null) return;
        resultHandler.OnCharacterDied -= HandleCharacterDied;
        resultHandler.OnEnvReset -= HandleEnvReset;
    }

    // Public API
    
    public bool GetUseAnimation() => useAnimation;

    public void StartTurnLoop()
    {
        if (turnLoopCoroutine != null)
            StopCoroutine(turnLoopCoroutine);

        turnLoopCoroutine = StartCoroutine(TurnLoop());
    }

    public void ResetEnv()
    {
        StopAllCoroutines();
        turnLoopCoroutine = null;
        isTurnRunning = false;
        State = CombatState.Playing;
        currentTurn = 1;
        allySlots.Clear();
        resultHandler.ResetEnv();
    }

    public void StopTurnLoop()
    {
        isTurnRunning = false;
        if (turnLoopCoroutine != null)
        {
            StopCoroutine(turnLoopCoroutine);
            turnLoopCoroutine = null;
        }
    }

    public int GetMaxTurn() => resultHandler.MaxTurn;

    // Turn loop

    private IEnumerator TurnLoop()
    {
        isTurnRunning = false;
        while (State == CombatState.Playing)
            yield return RunTurn();
    }
    private IEnumerator RunTurn()
    {
        if (isTurnRunning)
        {
            Debug.LogError("Turn already running!");
            yield break;
        }

        isTurnRunning = true;
        TurnAlreadyEnded = false;
        actor = null;

        RefreshTurnOrderUI();
        SkipDeadUnits();

        CharacterObject current = turnQueue.GetCurrent();
        if (current == null)
        {
            Debug.LogWarning("No current unit, skipping turn");
            turnQueue.PopNext();
            isTurnRunning = false;
            yield break;
        }

        AdvanceATB(current);
        AssignActor(current);

        if (actor == null)
        {
            print("actor is null");
            turnQueue.PopNext();
            isTurnRunning = false;
            yield break;
        }
        yield return null;
        float timeout = 20f;
        float timer = 0f;

        yield return new WaitUntil(() =>
        {
            timer += Time.deltaTime;
            return actor == null || actor.IsTurnComplete() || timer > timeout;
        });
        if (timer > timeout)
            Debug.LogWarning("Turn timeout! Forcing completion.");
        yield return EndTurnRoutine();
        isTurnRunning = false;
    }

    private IEnumerator EndTurnRoutine()
    {
        TurnAlreadyEnded = true;

        if (controlMode == CombatControlMode.BTRecording)
        {
            var allUnits = gridData.GetAllUnits();
            foreach (var unit in allUnits)
            {
                var bt = gridData.GetTileAt(unit.pos)?.PlacedGameObject?
                    .GetComponent<EnemyAIControllerBase>();
                bt?.SetObservingAgent(null);
            }
        }

        bool episodeEnded = resultHandler.HandleTurnEnd(currentTurn);

        if (episodeEnded)
        {
            isTurnRunning = false;
            yield break;
        }

        currentTurn++;
        turnQueue.PopNext();
        yield return null;
    }

    private void AssignActor(CharacterObject current)
    {
        if (controlMode == CombatControlMode.Player)
        {
            if (current.IsPlayer)
            {
                gridSelect.BeginTurn(gridData, current);
                actor = gridSelect;
                return;
            }
            AssignBehaviorTreeActor(current);
            return;
        }

        if (controlMode == CombatControlMode.MLAgent)
        {
            if (current.Team == currentAgentTeam)
            {
                AssignAgentActor(current);
                return;
            }
            AssignBehaviorTreeActor(current);
            return;
        }

        if (controlMode == CombatControlMode.PlayerVsAgent)
        {
            if (current.Team != currentAgentTeam)
            {
                if (current.IsPlayer)
                {
                    gridSelect.BeginTurn(gridData, current);
                    actor = gridSelect;
                }
                else
                {
                    AssignBehaviorTreeActor(current);
                }
                return;
            }
            AssignAgentActor(current);
            return;
        }

        if (controlMode == CombatControlMode.EnemyPlayer)
        {
            if (current.Team == 2)
            {
                gridSelect.BeginTurn(gridData, current);
                actor = gridSelect;
            }
            else
            {
                AssignBehaviorTreeActor(current);
            }
            return;
        }

        if (controlMode == CombatControlMode.Demonstration)
        {
            if (current.Team == demoControlledTeam)
            {
                bool wholeTeamIsHuman = demoControlledTeam != 1;
                if (wholeTeamIsHuman || current.IsPlayer)
                {
                    gridSelect.BeginTurn(gridData, current);
                    actor = gridSelect;
                    return;
                }
            }
            AssignBehaviorTreeActor(current);
            return;
        }

        if (controlMode == CombatControlMode.BTRecording)
        {
            if (current.Team == currentAgentTeam)
            {
                AssignBTRecordingActor(current);
            }
            else
            {
                AssignBehaviorTreeActor(current);
            }
            return;
        }
        AssignBehaviorTreeActor(current);
    }

    private void AssignBTRecordingActor(CharacterObject current)
    {
        int index = GetAllySlotIndex(current);
        if (index < 0)
        {
            Debug.LogWarning($"AssignBTRecordingActor: {current.Name} not in allySlots!");
            return;
        }
        combatAgent.SetActiveUnitIndex(index);
        combatAgent.BeginTurn(gridData, current); // start listening for request decision

        TileData tile = gridData.GetTileAt(current.Position);
        if (tile?.PlacedGameObject == null)
        {
            Debug.LogWarning($"AssignBTRecordingActor: no GameObject for {current.Name}");
            return;
        }

        EnemyAIControllerBase btActor = tile.PlacedGameObject
            .GetComponent<EnemyAIControllerBase>();

        if (btActor == null)
        {
            Debug.LogWarning($"No EnemyAIControllerBase found for {current.Name}!");
            return;
        }

        btActor.SetObservingAgent(combatAgent);
        btActor.BeginTurn(gridData, current);
        actor = combatAgent;
    }

    private void AssignAgentActor(CharacterObject current)
    {
        int index = GetAllySlotIndex(current);
        if (index < 0)
        {
            Debug.LogWarning($"AssignAgentActor: {current.Name} not found in allySlots!");
            return;
        }

        combatAgent.SetActiveUnitIndex(index);
        combatAgent.BeginTurn(gridData, current);

        if (combatAgent.GetIsManualMode())
        {
            gridSelect.BeginTurn(gridData, current);
            actor = gridSelect;
        }
        else
        {
            // combatAgent.BeginTurn(gridData, current);
            actor = combatAgent;
        }
    }

    private void AssignBehaviorTreeActor(CharacterObject current)
    {
        TileData tile = gridData.GetTileAt(current.Position);
        if (tile?.PlacedGameObject == null)
        {
            Debug.LogWarning($"AssignBehaviorTreeActor: no GameObject found for {current.Name}");
            return;
        }

        actor = tile.PlacedGameObject.GetComponent<ITurnActor>();
        actor?.BeginTurn(gridData, current);
    }

    private void HandleCharacterDied(CharacterObject character)
    {
        bool wasCurrent = turnQueue.GetCurrent() == character;

        ITurnActor deadActor = wasCurrent ? GetActorFor(character) : null;

        turnQueue.Remove(character);
        RefreshTurnOrderUI();

        int slotIndex = allySlots.IndexOf(character);
        if (slotIndex != -1)
            allySlots[slotIndex] = null;

        GetComponentInChildren<PopulateMap>().HandleCharacterDeath(character, onRemoved:() => 
        {
            bool battleEnded = CheckBattleEnd();

            if (wasCurrent && !battleEnded && deadActor != null)
            {
                Debug.LogWarning("Current unit died, forcing turn completion.");
                ForceActorComplete(deadActor);
            }
        });
    }

    private ITurnActor GetActorFor(CharacterObject character)
    {
        Vector3Int? pos = gridData.GetPositionOf(character);
        if (pos == null) return null;

        TileData tile = gridData.GetTileAt(pos.Value);
        return tile?.PlacedGameObject?.GetComponent<ITurnActor>();
    }

    private void ForceActorComplete(ITurnActor deadActor)
    {
        if (deadActor is CombatAgent2 agent)
            agent.ForceComplete();
        else if (deadActor is EnemyAIControllerBase enemy)
            enemy.ForceComplete();
        else if (deadActor is PlayerSystem player)
            player.ForceComplete();
    }

    // Turn queue setup

    private void InitializeTurnQueue(GridData data)
    {
        gridData = data;
        resultHandler.GridData = data;

        int humanControlledTeam = controlMode switch
        {
            CombatControlMode.EnemyPlayer => 2,
            CombatControlMode.Demonstration => demoControlledTeam,
            CombatControlMode.MLAgent => currentAgentTeam,
            CombatControlMode.BTRecording => currentAgentTeam,
            _ => 1
        };
        gridSelect.SetControlledTeam(humanControlledTeam);

        var units = gridData.GetAllUnits();
        List<CharacterObject> characters = new();
        allySlots = new List<CharacterObject>(3);

        foreach (var u in units)
        {
            CharacterObject c = u.character;
            characters.Add(c);

            bool agentControlsThisUnit = (controlMode == CombatControlMode.MLAgent || 
                controlMode == CombatControlMode.PlayerVsAgent || 
                controlMode == CombatControlMode.BTRecording) && c.Team == currentAgentTeam;

            if (agentControlsThisUnit)
                allySlots.Add(c);
    
            resultHandler.SubscribeCharacterDeath(u.character);
        }

        while (allySlots.Count < 3)
            allySlots.Add(null);

        combatAgent.SetAllySlots(allySlots);
        turnQueue = new TurnQueue(characters, 10); // Sementara simulate 10 turn ahead
        // print("Total units: " + turnQueue.allUnits.Count);
        RefreshTurnOrderUI();
    }

    private void HandleEnvReset(GridData newGridData)
    {
        InitializeTurnQueue(newGridData);
        StartCoroutine(StartTurnLoopNextFrame());
    }

    private IEnumerator StartTurnLoopNextFrame()
    {
        yield return null;
        StartTurnLoop();
    }
    void AdvanceATB(CharacterObject active)
    {
        if (active.Speed <= 0f) return;

        var units = gridData.GetAllUnits();
        float time = (100f - active.CurrentATB) / active.Speed;

        foreach (var u in units)
            u.character.AddATB(u.character.Speed * time);

        active.SubATB(100f);
    }

    bool CheckBattleEnd()
    {
        bool ended = resultHandler.CheckBattleEnd();
        if (ended) State = ended ? (IsVictory() ? CombatState.Victory : CombatState.Defeat) : State;
        return ended;
    }

    private bool IsVictory() => GridData().GetUnitsByTeam(currentAgentTeam == 1 ? 2 : 1).Count == 0;
    private GridData GridData() => gridData;

    private void SkipDeadUnits()
    {
        while (turnQueue.GetCurrent() != null &&
                gridData.GetPositionOf(turnQueue.GetCurrent()) == null)
        {
            Debug.LogWarning($"Skipping dead/missing unit: {turnQueue.GetCurrent()?.Name}");
            turnQueue.PopNext();
        }
    }

    private void RefreshTurnOrderUI()
    {
        if (showTurnOrderUI)
            turnOrderUI.Refresh(turnQueue.GetVisibleTurns());
    }

    int GetAllySlotIndex(CharacterObject unit)
    {
        for (int i = 0; i < allySlots.Count; i++)
        {
            if (allySlots[i] == unit)
                return i;
        }

        return -1;
    }

    // void ShowScene(Scene scene)
    // {
    //     foreach (GameObject root in scene.GetRootGameObjects())
    //     {
    //         root.SetActive(true);
    //     }
    // }
}
