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
    public CombatState State { get; private set; } = CombatState.Playing;
    private GridData gridData;
    private PopulateMap mapPopulator;
    [HideInInspector] public int currentTurn = 1;
    [SerializeField] private int maxTurn = 50;
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private TurnOrderUI turnOrderUI;
    [SerializeField] private CombatControlMode controlMode = CombatControlMode.Player;
    [SerializeField] private CombatAgent combatAgent;
    [SerializeField] private int currentAgentTeam = 1;
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private bool showTurnOrderUI = true;

    public TurnQueue turnQueue;
    private bool isTurnRunning = false;
    private List<CharacterObject> allySlots = new List<CharacterObject>(3);
    public List<string> defeatedEnemyNames = new();
    public bool TurnAlreadyEnded { get;  set; } = false;
    private ITurnActor actor;
    private Coroutine turnLoopCoroutine;

    void Start()
    {
        
    }
    private void InitializeTurnQueue()
    {
        var units = gridData.GetAllUnits();

        List<CharacterObject> characters = new();
        allySlots = new List<CharacterObject>(3);

        foreach (var u in units)
        {
            CharacterObject c = u.character;
            characters.Add(c);

            if (c.Team == currentAgentTeam)
                allySlots.Add(c);
    
            c.OnDied += HandleCharacterDeath;
        }

        while (allySlots.Count < 3)
            allySlots.Add(null);

        turnQueue = new TurnQueue(characters, 10); // Sementara simulate 10 turn ahead
        print("Total units: " + turnQueue.allUnits.Count);
    }
    
    public void StartTurn()
    {   
        if (isTurnRunning)
        {
            Debug.LogError("StartTurn called while turn is already running!");
            return;
        }
        
        StartCoroutine(RunTurn());
    }
    public void StartTurnLoop()
    {
        print("starting");
        if (turnLoopCoroutine != null)
            StopCoroutine(turnLoopCoroutine);

        turnLoopCoroutine = StartCoroutine(TurnLoop());
    }
    private IEnumerator TurnLoop()
    {
        isTurnRunning = false;
        while (State == CombatState.Playing)
        {
            yield return RunTurn();
        }
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
        if (showTurnOrderUI)
            turnOrderUI.Refresh(turnQueue.GetVisibleTurns());
        
        while (turnQueue.GetCurrent() != null && gridData.GetPositionOf(turnQueue.GetCurrent()) == null)
        {
            Debug.LogWarning($"Skipping dead/missing unit: {turnQueue.GetCurrent()?.Name}");
            turnQueue.PopNext();
        }

        CharacterObject current = turnQueue.GetCurrent();
        if (current == null)
        {
            Debug.LogWarning("No current unit, skipping turn");
            turnQueue.PopNext();
            isTurnRunning = false;
            yield break;
        }

        AdvanceATB(current);

        if (controlMode == CombatControlMode.Player && current.IsPlayer)
        {
            gridSelect.BeginTurn(gridData);
            actor = gridSelect;
        }
        else if (controlMode == CombatControlMode.MLAgent && current.Team == currentAgentTeam)
        {
            int index = GetAllySlotIndex(current);

            if (index >= 0)
            {
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
                // print("agent is playing");
            }
        }
        else
        {
            TileData tile = gridData.GetTileAt(current.Position);
            actor = tile.PlacedGameObject.GetComponent<ITurnActor>();
            if (actor != null)
                actor.BeginTurn(gridData, current);
            // print("ai is playing");
        }

        if (actor == null)
        {
            print("actor is null");
            turnQueue.PopNext();
            isTurnRunning = false;
            yield break;
        }

        float timeout = 20f;
        float timer = 0f;
        
        yield return null;
        // print("before wait until");
        yield return new WaitUntil(() =>
        {
            timer += Time.deltaTime;
            return actor == null || actor.IsTurnComplete() || timer > timeout;
        });
        if (timer > timeout)
        {
            Debug.LogError("Turn timeout! Forcing completion.");
        }
        // print("turn completed");
        yield return EndTurnRoutine();
        isTurnRunning = false;
    }

    private IEnumerator EndTurnRoutine()
    {
        TurnAlreadyEnded = true;
        if (controlMode == CombatControlMode.MLAgent)
        {
            if (currentTurn > maxTurn)
            {
                combatAgent.AddReward(-10f);
                isTurnRunning = false;
                StartCoroutine(EndEpisodeNextFrame());
                yield break;
            }
            combatAgent.AddReward(-0.01f);
            currentTurn++;
        }
        turnQueue.PopNext();
        
        yield return null;
    }

    public void ResetEnv()
    {
        Cleanup();
        GetComponent<CombatExecutor>().ResetState();
        if (turnLoopCoroutine != null)
        {
            StopCoroutine(turnLoopCoroutine);
            turnLoopCoroutine = null;
        }
        isTurnRunning = false;
        State = CombatState.Playing;
        currentTurn = 1;
        allySlots.Clear();
        defeatedEnemyNames.Clear();
        mapPopulator = GetComponentInChildren<PopulateMap>();
        mapPopulator.Generate();

        gridData = mapPopulator.objectsData;
        combatAgent.setGridData(gridData);
        combatAgent.SetAgentTeam(currentAgentTeam);
        

        InitializeTurnQueue();
        if (showTurnOrderUI)
            turnOrderUI.Refresh(turnQueue.GetVisibleTurns());
        combatAgent.SetAllySlots(allySlots);
        StartTurnLoop();
    }

    void Cleanup()
    {
        if (gridData == null) return;
        var units = gridData.GetAllUnits();

        foreach (var unit in units)
        {
            CharacterObject characterObject = unit.character;
            if (characterObject != null)
            {
                characterObject.OnDied -= HandleCharacterDeath;
                print("cleanup unsubscribe");
            }
        }
    }

    void AdvanceATB(CharacterObject active)
    {
        if (active.Speed <= 0f)
            return;

        var units = gridData.GetAllUnits();
        float time = (100f - active.CurrentATB) / active.Speed;

        foreach (var u in units)
        {
            u.character.AddATB(u.character.Speed * time);
        }

        active.SubATB(100f);
    }

    private void HandleCharacterDeath(CharacterObject character)
    {
        if (character.Team == currentAgentTeam)
        {
            if (controlMode == CombatControlMode.MLAgent)
                combatAgent.AddReward(-0.5f);
            int index = allySlots.IndexOf(character);
            if (index != -1)
            {
                allySlots[index] = null;
            }
        }
        else
            if (controlMode == CombatControlMode.MLAgent)
                combatAgent.AddReward(0.5f);

        bool wasCurrent = turnQueue.GetCurrent() == character;

        ITurnActor deadActor = null;

        if (wasCurrent)
        {
            Vector3Int? pos = gridData.GetPositionOf(character);

            if (pos != null)
            {
                TileData tile = gridData.GetTileAt(pos.Value);
                if (tile?.PlacedGameObject != null)
                {
                    print("deadactor");
                    deadActor = tile.PlacedGameObject.GetComponent<ITurnActor>();
                }
            }
        }

        turnQueue.Remove(character);
        Debug.Log($"Removing from queue: {character.Name}");
        if (showTurnOrderUI)
            turnOrderUI.Refresh(turnQueue.GetVisibleTurns());
        mapPopulator.HandleCharacterDeath(character);
        bool episodeEnded = CheckBattleEnd();

        if (wasCurrent && !episodeEnded)
        {
            Debug.Log("Current unit died, forcing turn completion");
            if (deadActor != null)
            {
                if (deadActor is CombatAgent agent)
                    agent.ForceComplete();
                else if (deadActor is EnemyAIControllerBase enemy)
                    enemy.ForceComplete();
                // PlayerSystem later
            }
        }
        
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

    public int GetMaxTurn() => maxTurn;
    public bool GetUseAnimation() => useAnimation;

    bool CheckBattleEnd()
    {
        int aliveAllies = gridData.GetUnitsByTeam(currentAgentTeam).Count;
        int aliveEnemies = gridData.GetUnitsByTeam(currentAgentTeam == 1 ? 2 : 1).Count;

        if (aliveEnemies == 0)
        {
            State = CombatState.Victory;

            if (controlMode == CombatControlMode.MLAgent)
            {
                print("MENANG");
                gridSelect.ExitCharacter();
                combatAgent.AddReward(10f);
                StartCoroutine(EndEpisodeNextFrame());
                return true;
            }
            else
            {
                // SceneManager.UnloadSceneAsync("Combat_test1");
                // ShowScene(SceneManager.GetActiveScene());
                // Destroy(GameManager.Instance.CurrentEnemy);
                // GameManager.Instance.CurrentEnemy = null;
                
                for (int i = 0; i < 3;i++)
                {
                    GameManager.Instance.defeatedEnemyNames.Add(defeatedEnemyNames[i]);
                }

                string enemyList = string.Join(", ", GameManager.Instance.defeatedEnemyNames);
                Debug.Log($" Enemies defeated: {enemyList}");

                // PERLU TAMBAH END SCREEN 
                
                GameManager.Instance.EndCombat();
                return true;
            }
        }

        if (aliveAllies == 0)
        {
            State = CombatState.Defeat;

            if (controlMode == CombatControlMode.MLAgent)
            {
                combatAgent.AddReward(-10f);
                StartCoroutine(EndEpisodeNextFrame());
                return true;
            }
            return true;
        }
        return false;
    }

    private IEnumerator EndEpisodeNextFrame()
    {
        yield return null; // wait one frame for all current coroutines to finish
        combatAgent.EndEpisode();
    }

    // void ShowScene(Scene scene)
    // {
    //     foreach (GameObject root in scene.GetRootGameObjects())
    //     {
    //         root.SetActive(true);
    //     }
    // }
}
