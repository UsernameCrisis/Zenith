using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Rendering;
using System.Threading.Tasks;

[Serializable]
public class Player : MonoBehaviour
{
    public static Player Instance;
    [Header("Player Components")]
    [SerializeField] public PlayerMovement _playerMovement { get; private set; }
    [SerializeField] public PlayerUIController _playerUIController{ get; private set; }
    [SerializeField] public PlayerData _playerData{ get; private set; }
    [SerializeField] public PlayerEventController _playerEventController { get; private set; }
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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async void Initialize()
    {
        _playerMovement = gameObject.GetComponent<PlayerMovement>();
        _playerUIController = gameObject.GetComponent<PlayerUIController>();
        _playerData = gameObject.GetComponent<PlayerData>();
        _playerEventController = gameObject.GetComponent<PlayerEventController>();

        _playerMovement.Initialize();
        _playerUIController.Initialize(_playerHealthUI);
        _playerData.Initialize(_baseHP, _baseSPD, _deathVolume, _inventory);
        _playerEventController.Initialize(_invincibilityDuration);

        await Task.CompletedTask;
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
