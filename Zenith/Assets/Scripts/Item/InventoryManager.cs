using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[System.Serializable]
public class ItemStack
{
    public BaseItem itemData;
    public int quantity;

    public ItemStack(BaseItem item, int amount)
    {
        itemData = item;
        quantity = amount;
    }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public event Action OnInventoryChanged;

    [Header("Inventory Settings")]
    public List<ItemStack> mainInventory = new();
    public float maxWeight = 200.0f;
    public float currentWeight;

    public Dictionary<EquipmentItem.EquipSlot, EquipmentItem> equippedItems = new();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SortInventory()
    {
        mainInventory = mainInventory
            .OrderBy(stack => GetTypePriority(stack.itemData.type)) // 1. Type
            .ThenByDescending(stack => (int)stack.itemData.rarity)  // 2. Rarity (Legendary first)
            .ThenBy(stack => stack.itemData.itemName)              // 3. Alphabetical
            .ToList();

        PrintInventoryDebug();
    }

    private int GetTypePriority(ItemType type)
    {
        return type switch
        {
            ItemType.Equipment => 0,
            ItemType.Consumable => 1,
            ItemType.Junk => 2,
            _ => 3
        };
    }

    public bool AddItem(BaseItem item, int amount)
    {
        // Check weight first
        if (currentWeight + (item.weight * amount) > maxWeight)
        {
            Debug.Log("<color=red>Too heavy to carry!</color>");
            return false;
        }

        bool foundStack = false;
        // Handle Stacking
        if (item.isStackable)
        {
            ItemStack existingStack = mainInventory.Find(slot => slot.itemData == item);
            if (existingStack != null)
            {
                existingStack.quantity += amount;
                foundStack = true;
            }
        }

        // Add as new stack if we didn't find one
        if (!foundStack)
        {
            mainInventory.Add(new ItemStack(item, amount));
        }
        UpdateWeight();
        SortInventory();

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem(BaseItem item, int amount = 1)
    {
        ItemStack stack = mainInventory.Find(s => s.itemData == item);
        if (stack != null)
        {
            stack.quantity -= amount;
            if (stack.quantity <= 0)
            {
                mainInventory.Remove(stack);
            }
        }
        UpdateWeight();
        OnInventoryChanged?.Invoke();
    }

    public void UseItem(ItemStack stack)
    {
        if (stack.itemData == null) return;

        stack.itemData.Use();

        if (stack.itemData.type == ItemType.Consumable)
        {
            RemoveItem(stack.itemData, 1);
        }

        OnInventoryChanged?.Invoke();
    }

    public void UpdateWeight()
    {
        currentWeight = 0;
        foreach (ItemStack stack in mainInventory)
        {
            currentWeight += stack.itemData.weight * stack.quantity;
        }
    }

    // Your Death Penalty: Loses ~50% of the total stacks (randomized)
    public void HandleDeathPenalty()
    {
        int itemsToLose = mainInventory.Count / 2;
        for (int i = 0; i < itemsToLose; i++)
        {
            if (mainInventory.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, mainInventory.Count);
                mainInventory.RemoveAt(randomIndex);
            }
        }
        UpdateWeight();
        OnInventoryChanged?.Invoke();
        Debug.Log("Player died. Lost 50% of inventory items.");
    }

    public void EquipItem(EquipmentItem newItem)
    {
        if (equippedItems.ContainsKey(newItem.slot))
        {
            equippedItems[newItem.slot] = newItem;
        }
        else
        {
            equippedItems.Add(newItem.slot, newItem);
        }

        RefreshTotalStats();
    }

    public void RefreshTotalStats()
    {
        int totalAtk = 10;
        int totalDef = 0;
        int totalSpeed = 5;
        int totalMaxHP = 100;

        foreach (var item in equippedItems.Values)
        {
            totalAtk += item.attackBonus;
            totalDef += item.defenseBonus;
            totalSpeed += item.speedBonus;
            totalMaxHP += item.maxHpBonus;
        }

        GameManager.Instance.playerAtk = totalAtk;
        GameManager.Instance.playerDef = totalDef;
        GameManager.Instance.playerSpeed = totalSpeed;
        GameManager.Instance.playerMaxHP = totalMaxHP;

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerOverworldAttributes>();
        if (player != null)
        {
            player.maxHP = totalMaxHP;
            player.currentHP = Mathf.Min(player.currentHP, player.maxHP);
        }
    }

    private void PrintInventoryDebug()
    {
        string debugLog = "--- CURRENT INVENTORY ---\n";
        debugLog += $"Total Weight: {currentWeight}/{maxWeight}\n";
        debugLog += "TYPE | RARITY | NAME | QTY\n";
        debugLog += "---------------------------\n";

        foreach (var stack in mainInventory)
        {
            debugLog += $"[{stack.itemData.type}] ";
            debugLog += $"({stack.itemData.rarity}) ";
            debugLog += $"{stack.itemData.itemName} ";
            debugLog += $"x{stack.quantity}\n";
        }

        Debug.Log(debugLog);
    }
}