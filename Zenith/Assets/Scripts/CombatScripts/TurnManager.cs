using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TurnManager : MonoBehaviour
{
    
    public static TurnManager Instance;
    private GridData gridData;
    [SerializeField] private PopulateMap mapPopulator;
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private EnemyAIController_1 enemyAI;
    
    private TurnQueue turnQueue;

    void Awake()
    {
        Instance = this;
        
    }

    private IEnumerator Start()
    {
        yield return null;
        InitializeTurnQueue();
        StartTurn();
    }
    private void InitializeTurnQueue()
    {
        gridData = mapPopulator.GetComponent<PopulateMap>().objectsData;
        var units = gridData.GetAllUnits();

        List<CharacterObject> characters = new(); // For getting the object only

        foreach (var u in units)
        {
            characters.Add(u.character);
        }

        turnQueue = new TurnQueue(characters); // Sementara belum pakai speed system
        print(turnQueue.units.Count);
    }
    
    public void StartTurn()
    {
        CharacterObject current = turnQueue.GetCurrent();

        Vector3Int? posNullable = gridData.GetPositionOf(current);

        if (posNullable == null)
        {
            Debug.LogError("ERROR: Character not found on grid! " + current.Name);
            return;
        }

        Vector3Int pos = posNullable.Value;

        if (current.IsPlayer)
        {
            print("player is playing");
            gridSelect.BeginTurn(pos, gridData);
        }
        else
        {
            print("enemy is playing");
            enemyAI.BeginTurn(pos, gridData);
        }
    }

    public void EndTurn()
    {
        turnQueue.Next();
        StartTurn();
    }
}
