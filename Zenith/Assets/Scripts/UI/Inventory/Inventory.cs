using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    public bool expandable = false;
    public GameObject ItemDescription;
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
            // if (transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.active) transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.SetActive(false);
        }
    }

    public List<List<DraggableItem>> GetInventoryIn2DList()
    {
        List<List<DraggableItem>> list = GetComponentInChildren<InventoryRight>().GetItems();
        return list;
    }

    public void LoadInventoryFrom2DList(List<List<DraggableItem>> list)
    {
        GetComponentInChildren<InventoryRight>().UnloadItems(list);
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
