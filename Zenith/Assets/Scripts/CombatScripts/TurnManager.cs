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

public class TurnManager : MonoBehaviour
{
    
    public static TurnManager Instance;
    public CombatState State { get; private set; } = CombatState.Playing;
    private GridData gridData;
    private int maxTurn = 50;
    private int currentTurn = 1;
    [SerializeField] private PopulateMap mapPopulator;
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private TurnOrderUI turnOrderUI;

    
    private TurnQueue turnQueue;

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

        if (current.IsPlayer)
        {
            gridSelect.BeginTurn(pos, gridData);
            return;
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
        
        if (!AreEnemiesRemaining())
        {
            SceneManager.LoadScene("Ruins");
            return;
        }
        
        if (turnQueue.GetCurrent() == character)
        {
            EndTurn();
        }
    }

    public void EndTurn()
    {
        turnQueue.PopNext();
        currentTurn++;
        StartTurn();
    }

    private bool AreEnemiesRemaining()
    {
        return gridData.GetAllEnemies().Count > 0;
    }
}
