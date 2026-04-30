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
    MLAgent
}

public class TurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private TurnOrderUI turnOrderUI;
    [SerializeField] private CombatAgent combatAgent;

    [Header("Settings")]
    [SerializeField] private CombatControlMode controlMode = CombatControlMode.Player;
    [SerializeField] private int currentAgentTeam = 1;
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private bool showTurnOrderUI = true;

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
        isTurnRunning = false;
        State = CombatState.Playing;
        currentTurn = 1;

        if (turnLoopCoroutine != null)
        {
            StopCoroutine(turnLoopCoroutine);
            turnLoopCoroutine = null;
        }
        
        allySlots.Clear();
        resultHandler.ResetEnv();
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
            Debug.LogError("Turn timeout! Forcing completion.");
        yield return EndTurnRoutine();
        isTurnRunning = false;
    }

    private IEnumerator EndTurnRoutine()
    {
        TurnAlreadyEnded = true;
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
        if (controlMode == CombatControlMode.Player && current.IsPlayer)
        {
            gridSelect.BeginTurn(gridData);
            actor = gridSelect;
            return;
        }

        if (controlMode == CombatControlMode.MLAgent && current.Team == currentAgentTeam)
        {
            int index = GetAllySlotIndex(current);
            if (index < 0) return;

            combatAgent.SetActiveUnitIndex(index);

            if (combatAgent.GetIsManualMode())
            {
                gridSelect.BeginTurn(gridData, current);
                actor = gridSelect;
            }
            else
            {
                combatAgent.BeginTurn(gridData, current);
                actor = combatAgent;
            }
            return;
        }

        // Bot-controlled enemy unit.
        TileData tile = gridData.GetTileAt(current.Position);
        if (tile?.PlacedGameObject == null) return;

        actor = tile.PlacedGameObject.GetComponent<ITurnActor>();
        actor?.BeginTurn(gridData, current);
    }

    private void HandleCharacterDied(CharacterObject character)
    {
        bool wasCurrent = turnQueue.GetCurrent() == character;

        ITurnActor deadActor = wasCurrent ? GetActorFor(character) : null;

        turnQueue.Remove(character);
        RefreshTurnOrderUI();

        GetComponentInChildren<PopulateMap>().HandleCharacterDeath(character);

        int slotIndex = allySlots.IndexOf(character);
        if (slotIndex != -1)
            allySlots[slotIndex] = null;

        bool battleEnded = CheckBattleEnd();

        if (wasCurrent && !battleEnded && deadActor != null)
        {
            Debug.LogWarning("Current unit died, forcing turn completion.");
            ForceActorComplete(deadActor);
        }
        
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
        if (deadActor is CombatAgent agent)
            agent.ForceComplete();
        else if (deadActor is EnemyAIControllerBase enemy)
            enemy.ForceComplete();
        // PlayerSystem handling can be added here when needed.
    }

    // Turn queue setup

    private void InitializeTurnQueue(GridData data)
    {
        gridData = data;
        resultHandler.GridData = data;

        var units = gridData.GetAllUnits();
        List<CharacterObject> characters = new();
        allySlots = new List<CharacterObject>(3);

        foreach (var u in units)
        {
            CharacterObject c = u.character;
            characters.Add(c);

            if (c.Team == currentAgentTeam)
                allySlots.Add(c);
    
            resultHandler.SubscribeCharacterDeath(u.character);
        }

        while (allySlots.Count < 3)
            allySlots.Add(null);

        combatAgent.SetAllySlots(allySlots);
        turnQueue = new TurnQueue(characters, 10); // Sementara simulate 10 turn ahead
        print("Total units: " + turnQueue.allUnits.Count);
        RefreshTurnOrderUI();
    }

    private void HandleEnvReset(GridData newGridData)
    {
        InitializeTurnQueue(newGridData);
        StartTurnLoop();
    }

    void AdvanceATB(CharacterObject active)
    {
        if (active.Speed <= 0f) return;

        var units = gridData.GetAllUnits();
        float time = (100f - active.CurrentATB) / active.Speed;

        foreach (var u in units)
        {
            u.character.AddATB(u.character.Speed * time);
        }

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
