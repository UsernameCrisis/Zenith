using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Rendering;

public class Player : MonoBehaviour
{
    [Header("Player Components")]
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField]  private PlayerUIController _playerUIController;
    [SerializeField] private PlayerData _playerData;
    [SerializeField] private PlayerEventController _playerEventController;
    [Header("Base Stats")]
    [SerializeField] private int baseHP;
    [SerializeField] private int baseSPD;
    [SerializeField] private float invincibilityDuration = 0.4f;
    [SerializeField] private Volume deathVolume;
    [SerializeField] private Inventory inventory;
    [Header("Lists")]
    [SerializeReference] private static List<Character> companions = new List<Character>(2);
    [SerializeReference] private List<Move> moves = new List<Move>();


    public void Initialize()
    {
        _playerMovement = Instantiate(_playerMovement);
        _playerUIController = Instantiate(_playerUIController);
        _playerData = Instantiate(_playerData);
        _playerEventController = Instantiate(_playerEventController);

        _playerData.Initialize(baseHP, baseSPD, deathVolume, inventory, invincibilityDuration);
        _playerMovement.Initialize();
        _playerEventController.Initialize(_playerMovement);

        Debug.Log(companions == null);
    }

    public List<Character> GetCompanions()
    {
        return companions;
    }
    public List<Move> getMoves()
    {
        return moves;
    }
}
