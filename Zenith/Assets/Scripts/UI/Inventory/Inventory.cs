using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    public bool expandable = false;
    public GameObject ItemDescription;
    public DraggableItem DraggableItemPrefab;
    public bool isSelling = false;
    public void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
    public void ToggleInventory()
    {
        gameObject.SetActive(!gameObject.active);
    }
    public void AddItem(DraggableItem item)
    {
        GetComponentInChildren<InventoryRight>().AddItem(item);
    }

    void Update()
    {
        if (InputSystem.actions.FindAction("ExitSelect").WasPressedThisFrame())
        {
            if (gameObject.active) gameObject.SetActive(false);
            if (ItemDescription.active) ItemDescription.SetActive(false);
            isSelling = false;
            // if (transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.active) transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.SetActive(false);
        }
    }

    public List<List<DraggableItem>> SaveInventoryIn2DList()
    {
        return GetComponentInChildren<InventoryRight>().GetItems();
    }

    public List<DraggableItem> GetEquipmentAsList()
    {
        return GetComponentInChildren<InventoryLeft>().GetItems();
    }

    public void LoadInventory(List<List<ItemData>> inventory, List<ItemData> equipment)
    {
        List<List<DraggableItem>> _inventory = new List<List<DraggableItem>>();
        List<DraggableItem> _equipment = new List<DraggableItem>();

        for (int i = 0; i < inventory.Count; i++)
        {
            _inventory.Add(new List<DraggableItem>());
            for (int j = 0; j < inventory[i].Count; j++)
            {
                if (inventory[i][j] == null)
                {
                    _inventory[i].Add(null);
                    continue;
                }
                DraggableItem newDraggableItem = Instantiate(DraggableItemPrefab);
                newDraggableItem.SetData(inventory[i][j]);
                _inventory[i].Add(newDraggableItem);
            }
        }

        for (int i = 0; i < equipment.Count; i++)
        {
            if (equipment[i] == null)
            {
                _equipment.Add(null);
                continue;
            }
            DraggableItem newDraggableItem = Instantiate(DraggableItemPrefab);
            newDraggableItem.SetData(equipment[i]);
            _equipment.Add(newDraggableItem);
        }


        GetComponentInChildren<InventoryRight>().UnloadItems(_inventory);
        GetComponentInChildren<InventoryLeft>().UnloadEquipment(_equipment);
    }
    public int GetArmor()
    {
        return GetComponentInChildren<InventoryLeft>().GetArmor();
    }
    public int GetATK()
    {
        return GetComponentInChildren<InventoryLeft>().GetAtk();
    }

    public bool CanInsertToInventory(DraggableItem item)
    {
        return GetComponentInChildren<InventoryRight>().CanInsertItem(item);
    }
}
