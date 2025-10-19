using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField]  private PlayerUIController _playerUIController;
    [SerializeField] private PlayerData _playerData;
    [SerializeField] private PlayerEventController _playerEventController;
    [SerializeReference] private static List<Character> companions = new List<Character>(2);
    [SerializeReference] private List<Move> moves = new List<Move>();

    public void Initialize()
    {
        _playerMovement = Instantiate(_playerMovement);
        _playerUIController = Instantiate(_playerUIController);
        _playerData = Instantiate(_playerData);
        _playerEventController = Instantiate(_playerEventController);

        Debug.Log(companions == null);
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
        // playerMovement.canMove(canMove);
    }

    //temp
    public List<Move> getMoves()
    {
        return moves;
    }
}
