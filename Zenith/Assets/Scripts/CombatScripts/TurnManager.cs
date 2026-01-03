using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TurnManager : MonoBehaviour
{
    
    public static TurnManager Instance;
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

        List<CharacterObject> characters = new(); // For getting the object only

        foreach (var u in units)
        {
            characters.Add(u.character);
        }

        turnQueue = new TurnQueue(characters, 10); // Sementara simulate 10 turn ahead
        print(turnQueue.allUnits.Count);
    }
    
    public void StartTurn()
    {
        CharacterObject current = turnQueue.GetCurrent();
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

    public void EndTurn()
    {
        turnQueue.PopNext();
        currentTurn++;
        StartTurn();
    }
}
