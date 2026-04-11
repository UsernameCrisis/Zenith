using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    
    public static TurnManager Instance;
    public CombatState State { get; private set; } = CombatState.Playing;
    private GridData gridData;
    private int maxTurn = 50;
    [HideInInspector] public int currentTurn = 1;
    [SerializeField] private PopulateMap mapPopulator;
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private TurnOrderUI turnOrderUI;
    [SerializeField] private CombatControlMode controlMode = CombatControlMode.Player;
    [SerializeField] private CombatAgent combatAgent;
    [SerializeField] private int currentAgentTeam = 1;
    [SerializeField] private bool useAnimation = true;

    private TurnQueue turnQueue;
    private List<CharacterObject> allySlots = new();
    public List<string> defeatedEnemyNames = new();

    void Awake()
    {
        Instance = this;
        gridData = mapPopulator.GetComponent<PopulateMap>().objectsData;
        combatAgent.setGridData(gridData);
        combatAgent.SetAgentTeam(currentAgentTeam);
    }

    private IEnumerator Start()
    {
        yield return null;
        InitializeTurnQueue();
        turnOrderUI.Refresh(turnQueue.GetVisibleTurns());
        StartTurn();
    }
    private void InitializeTurnQueue()
    {
        currentTurn = 1;
        
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
                gridSelect.BeginTurn(gridData, current);
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
                combatAgent.AddReward(-0.05f);
                combatAgent.EndEpisode();
            }
            return;
        }
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
