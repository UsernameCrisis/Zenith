using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class MainCanvasManager : MonoBehaviour
{
    public static MainCanvasManager Instance { get; private set; }
    public PlayerHealthUI PlayerHealthUI { get; private set; }
    public GoldUI PlayerGoldUI {get; private set; }

   public InventoryRight InventoryRight { get; set; }
   public Inventory RootInventory {get; set; }


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

        RootInventory = GetComponentInChildren<Inventory>(); 
        InventoryRight = GetComponentInChildren<InventoryRight>();

        PlayerGoldUI = GetComponentInChildren<GoldUI>();
        PlayerGoldUI.UpdateAmount();

        RootInventory.ToggleInventory();
    }

    private void Update() {
        if (InputSystem.actions.FindAction("Inventory").WasPressedThisFrame())
        {
            RootInventory.ToggleInventory();
        }
    }
}
