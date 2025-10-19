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
    [SerializeField] private int _baseHP;
    [SerializeField] private int _baseSPD;
    [SerializeField] private float _invincibilityDuration = 0.4f;
    [Header("Component Dependencies")]
    [SerializeField] private Volume _deathVolume;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private PlayerHealthUI _playerHealthUI;
    [Header("Lists")]
    [SerializeReference] private static List<Character> companions = new List<Character>(2);
    [SerializeReference] private List<Move> moves = new List<Move>();


    public void Initialize()
    {
        _playerMovement = Instantiate(_playerMovement);
        _playerUIController = Instantiate(_playerUIController);
        _playerData = Instantiate(_playerData);
        _playerEventController = Instantiate(_playerEventController);

        _playerMovement.Initialize();
        _playerUIController.Initialize(_playerHealthUI, _playerData);
        _playerData.Initialize(_baseHP, _baseSPD, _deathVolume, _inventory);
        _playerEventController.Initialize(_playerMovement, _invincibilityDuration);

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
