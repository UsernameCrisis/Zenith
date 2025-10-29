using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class Player : Character
{
    private static Player Instance;
    public PlayerMovement PlayerMovement { get; private set; }
    public PlayerData PlayerData { get; private set; }
    public PlayerUIManager PlayerUIManager { get; private set; }
    public PlayerEventManager PlayerEventManager { get; private set; }
    public PlayerAnimationManager PlayerAnimationManager { get; private set; }
    public Canvas canvas{ get; private set; }
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
        PlayerMovement = gameObject.GetComponent<PlayerMovement>();
        PlayerData = gameObject.GetComponent<PlayerData>();
        PlayerUIManager = gameObject.GetComponent<PlayerUIManager>();
        PlayerEventManager = gameObject.GetComponent<PlayerEventManager>();
        PlayerAnimationManager = gameObject.gameObject.GetComponent<PlayerAnimationManager>();
        canvas = gameObject.GetComponentInChildren<Canvas>();
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
        PlayerMovement.canMove(canMove);
    }

    protected override void die()
    {
        throw new NotImplementedException();
    }

    public void TakeDamage(int damage) {this.PlayerData.TakeDamage(damage);}

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
