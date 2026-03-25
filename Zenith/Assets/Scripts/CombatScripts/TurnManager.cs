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

    private TurnQueue turnQueue;
    private List<CharacterObject> allySlots = new();
    public List<string> defeatedEnemyNames = new();

    void Awake()
    {
        Instance = this;
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
        gridData = mapPopulator.GetComponent<PopulateMap>().objectsData;
        var units = gridData.GetAllUnits();

        List<CharacterObject> characters = new();

        foreach (var u in units)
        {
            CharacterObject c = u.character;
            characters.Add(c);

            if (c.Team == 1)
                allySlots.Add(c);
    
            c.OnDied += HandleCharacterDeath;
        }

        turnQueue = new TurnQueue(characters, 10); // Sementara simulate 10 turn ahead
        print(turnQueue.allUnits.Count);
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
                gridSelect.BeginTurn(pos, gridData);
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
        ai.BeginTurn(pos, gridData);
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
        print(" halo" +gridData.GetAllEnemies().Count);
        if (currentTurn >= maxTurn)
        {
            if (controlMode == CombatControlMode.MLAgent)
            {
                combatAgent.AddReward(-0.2f);
                combatAgent.EndEpisode();
            }
            return;
        }
        turnQueue.PopNext();
        currentTurn++;
        StartTurn();
    }

    private bool AreEnemiesRemaining()
    {
        return gridData.GetAllEnemies().Count > 0;
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

    void CheckBattleEnd()
    {
        int aliveAllies = gridData.GetTeamNPC().Count;
        int aliveEnemies = gridData.GetAllEnemies().Count;

        if (aliveEnemies == 0)
        {
            State = CombatState.Victory;

            if (controlMode == CombatControlMode.MLAgent)
            {
                combatAgent.AddReward(1f);
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
                combatAgent.AddReward(-1f);
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
