using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainCanvasManager : MonoBehaviour
{
    public static MainCanvasManager Instance { get; private set; }
    public PlayerHealthUI PlayerHealthUI { get; private set; }

   public Inventory Inventory { get; set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        PlayerHealthUI = GetComponentInChildren<PlayerHealthUI>();
        PlayerHealthUI.UpdateUI();
        PlayerHealthUI.hpText.enabled = false;

        Inventory = GetComponentInChildren<Inventory>();
        Inventory.ToggleInventory();
    }

    private void Update() {
        if (InputSystem.actions.FindAction("Inventory").WasPressedThisFrame())
        {
            Inventory.ToggleInventory();
        }
    }
}
