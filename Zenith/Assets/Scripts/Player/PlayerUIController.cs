using System;
using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    public PlayerHealthUI _playerHealthUI{ get; set; }
    private PlayerData _playerData;
    public void Initialize(PlayerHealthUI healthUI)
    {
        _playerHealthUI = healthUI;
    }
}
