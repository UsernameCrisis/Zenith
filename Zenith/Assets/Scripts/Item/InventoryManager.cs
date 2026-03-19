using System.Collections.Generic;
using UnityEngine;

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

    [Header("Inventory Settings")]
    public List<ItemStack> mainInventory = new();
    public float maxWeight = 50.0f;
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

    public bool AddItem(BaseItem item, int amount)
    {
        // 1. Check weight first
        if (currentWeight + (item.weight * amount) > maxWeight)
        {
            Debug.Log("Too heavy to carry!");
            return false;
        }

        // 2. Handle Stacking
        if (item.isStackable)
        {
            ItemStack existingStack = mainInventory.Find(slot => slot.itemData == item);
            if (existingStack != null)
            {
                existingStack.quantity += amount;
                UpdateWeight();
                return true;
            }
        }

        // 3. Add as new stack (if not stackable or not found)
        mainInventory.Add(new ItemStack(item, amount));
        UpdateWeight();
        return true;
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
                int randomIndex = Random.Range(0, mainInventory.Count);
                mainInventory.RemoveAt(randomIndex);
            }
        }
        UpdateWeight();
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
}