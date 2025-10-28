using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class Player : Character
{
    private static Player Instance;
    public PlayerMovement playerMovement { get; private set; }
    public PlayerData playerData { get; private set; }
    public PlayerUIManager playerUIManager { get; private set; }
    public PlayerEventManager playerEventManager { get; private set; }
    public PlayerAnimationManager playerAnimationManager{ get; private set; }
    private List<Character> companions;
    private List<Move> moves = new List<Move>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        playerMovement = gameObject.GetComponent<PlayerMovement>();
        playerData = gameObject.GetComponent<PlayerData>();
        playerUIManager = gameObject.GetComponent<PlayerUIManager>();
        playerEventManager = gameObject.GetComponent<PlayerEventManager>();
        playerAnimationManager = gameObject.gameObject.GetComponent<PlayerAnimationManager>();
    }

    public List<Character> GetCompanions()
    {
        return companions;
    }


    // Update is called once per frame
    void Update()
    {

    }

    public void canMove(bool canMove)
    {
        playerMovement.canMove(canMove);
    }

    protected override void die()
    {
        throw new NotImplementedException();
    }

    //temp
    public List<Move> getMoves()
    {
        return moves;
    }

    public void save(ref PlayerSaveData data)
    {
        data.Position = transform.position;
    }
    public void load(PlayerSaveData data)
    {
        transform.position = data.Position;
    }
}


[System.Serializable]
public struct PlayerSaveData
{
    public Vector3 Position;
}
