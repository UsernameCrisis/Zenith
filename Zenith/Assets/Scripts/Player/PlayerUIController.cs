using System;
using UnityEngine;

public class PlayerUIController : MonoBehaviour
{
    private PlayerHealthUI _playerHealthUI;
    private PlayerData _playerData;
    public void Initialize(PlayerHealthUI healthUI, PlayerData playerData)
    {
        _playerHealthUI = healthUI;
        _playerData = playerData;
        _playerHealthUI.Initialize(_playerData);
    }
}
