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

    private TurnQueue turnQueue;
    private List<CharacterObject> allySlots = new();
    public List<string> defeatedEnemyNames = new();

    void Start()
    {
        ResetEnv();
    }
    private void InitializeTurnQueue()
    {
        var units = gridData.GetAllUnits();

        List<CharacterObject> characters = new();

        foreach (var u in units)
        {
            CharacterObject c = u.character;
            characters.Add(c);

            if (c.Team == currentAgentTeam)
                allySlots.Add(c);
    
            c.OnDied += HandleCharacterDeath;
        }

        turnQueue = new TurnQueue(characters, 10); // Sementara simulate 10 turn ahead
        print("Total units: " + turnQueue.allUnits.Count);
    }
    
    public void StartTurn()
    {
        CharacterObject current = turnQueue.GetCurrent();
        if (current == null || gridData.GetPositionOf(current) == null)
        {
            EndTurn();
            return;
        }
        AdvanceATB(current);
        turnOrderUI.Refresh(turnQueue.GetVisibleTurns());
        Vector3Int? posNullable = gridData.GetPositionOf(current);

        if (posNullable == null)
        {
            Debug.LogError("ERROR: Character not found on grid! " + current.Name);
            return;
        }

        Vector3Int pos = posNullable.Value;
        // PLAYER MODE
        if (controlMode == CombatControlMode.Player)
        {
            if (current.IsPlayer)
            {
                gridSelect.BeginTurn(gridData);
                return;
            }
        }

        // AGENT MODE
        if (controlMode == CombatControlMode.MLAgent && current.Team == 1)
        {
            int index = GetAllySlotIndex(current);

            if (index >= 0)
            {
                combatAgent.SetActiveUnitIndex(index);
                if (combatAgent.GetIsManualMode())
                    gridSelect.BeginTurn(gridData, current);
                else
                    combatAgent.RequestDecision();
                return;
            }
        }

        TileData tile = gridData.GetTileAt(pos);
        EnemyAIControllerBase ai = tile.PlacedGameObject.GetComponent<EnemyAIControllerBase>();
        
        if (ai == null)
        {
            EndTurn();
            return;
        }
        print("enemy is playing");
        ai.BeginTurn(gridData);
    }

    public void ResetEnv()
    {
        Cleanup();

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
        turnOrderUI.Refresh(turnQueue.GetVisibleTurns());
        StartTurn();
    }

    void Cleanup()
    {
        if (gridData == null) return;
        var units = gridData.GetAllUnits();

        foreach (var unit in units)
        {
            CharacterObject characterObject = unit.character;
            if (characterObject != null)
                characterObject.OnDied -= HandleCharacterDeath;
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
            combatAgent.AddReward(-0.5f);
        else
            combatAgent.AddReward(0.5f);

        turnQueue.Remove(character);
        turnOrderUI.Refresh(turnQueue.GetVisibleTurns());

        mapPopulator.HandleCharacterDeath(character);

        CheckBattleEnd();
        
        if (turnQueue.GetCurrent() == character)
        {
            EndTurn();
        }
    }

    public void EndTurn()
    {
        if (currentTurn >= maxTurn)
        {
            if (controlMode == CombatControlMode.MLAgent)
            {
                combatAgent.AddReward(-10f);
                combatAgent.EndEpisode();
            }
            return;
        }
        combatAgent.AddReward(-0.05f);
        turnQueue.PopNext();
        currentTurn++;
        StartTurn();
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

    void CheckBattleEnd()
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
                combatAgent.EndEpisode();
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
            }
        }

        if (aliveAllies == 0)
        {
            State = CombatState.Defeat;

            if (controlMode == CombatControlMode.MLAgent)
            {
                combatAgent.AddReward(-10f);
                combatAgent.EndEpisode();
            }
        }
    }

    // void ShowScene(Scene scene)
    // {
    //     foreach (GameObject root in scene.GetRootGameObjects())
    //     {
    //         root.SetActive(true);
    //     }
    // }
}
