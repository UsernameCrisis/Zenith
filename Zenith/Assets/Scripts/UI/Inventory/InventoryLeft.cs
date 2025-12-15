using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryLeft : MonoBehaviour
{
    public enum Panels
    {
        Chest,
        Gear
    }

    public Panels CurrentPanel = Panels.Gear;
    public GameObject GearPanel;
    public GameObject ChestItemsUI;
    public GameObject ShopPanel;
    public GameObject ArmorSlot1;
    public GameObject ArmorSlot2;
    public GameObject ArmorSlot3;
    public GameObject ArmorSlot4;
    public GameObject WeaponSlot;

    public void SetGearActive()
    {
        ChestItemsUI.SetActive(false);
        ShopPanel.SetActive(false);
        GearPanel.SetActive(true);
    }
    
    public void SetInteractableInventoryActive()
    {
        GearPanel.SetActive(false);
        ShopPanel.SetActive(false);
        ChestItemsUI.SetActive(true);
    }
    public void SetShopActive()
    {
        GearPanel.SetActive(false);
        ChestItemsUI.SetActive(false);
        ShopPanel.SetActive(true);
    }
    public void SetNoneActive()
    {
        GearPanel.SetActive(false);
        ChestItemsUI.SetActive(false);
        ShopPanel.SetActive(false);
    }

    public int GetArmor()
    {
        int armor = 0;
        try {armor += (ArmorSlot1.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value == 0 ? 
        (int) ArmorSlot1.transform.GetChild(0).GetComponentInChildren<DraggableItem>().item.base_stat_value :
        (int) ArmorSlot1.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value);} catch (Exception ex) {} 
        try {armor += (ArmorSlot2.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value == 0 ? 
        (int) ArmorSlot2.transform.GetChild(0).GetComponentInChildren<DraggableItem>().item.base_stat_value :
        (int) ArmorSlot2.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value);} catch (Exception ex) {} 
        try {armor += (ArmorSlot3.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value == 0 ? 
        (int) ArmorSlot3.transform.GetChild(0).GetComponentInChildren<DraggableItem>().item.base_stat_value :
        (int) ArmorSlot3.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value);} catch (Exception ex) {} 
        try {armor += (ArmorSlot4.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value == 0 ? 
        (int) ArmorSlot4.transform.GetChild(0).GetComponentInChildren<DraggableItem>().item.base_stat_value :
        (int) ArmorSlot4.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value);} catch (Exception ex) {} 
        return armor;
    }
    public int GetAtk()
    {
        try {return (WeaponSlot.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value == 0 ? 
        (int) WeaponSlot.transform.GetChild(0).GetComponentInChildren<DraggableItem>().item.base_stat_value : 
        (int) WeaponSlot.transform.GetChild(0).GetComponentInChildren<DraggableItem>().stat_value);} catch (Exception ex) {return 1;} 
    }

    public List<DraggableItem> GetItems()
    {
        List<DraggableItem> items = new List<DraggableItem>();
        try {items.Add(Instantiate(ArmorSlot1.transform.GetChild(0).GetComponentInChildren<DraggableItem>()));} catch (Exception ex) {items.Add(null);}
        try {items.Add(Instantiate(ArmorSlot2.transform.GetChild(0).GetComponentInChildren<DraggableItem>()));} catch (Exception ex) {items.Add(null);}
        try {items.Add(Instantiate(ArmorSlot3.transform.GetChild(0).GetComponentInChildren<DraggableItem>()));} catch (Exception ex) {items.Add(null);}
        try {items.Add(Instantiate(ArmorSlot4.transform.GetChild(0).GetComponentInChildren<DraggableItem>()));} catch (Exception ex) {items.Add(null);}
        try {items.Add(Instantiate(WeaponSlot.transform.GetChild(0).GetComponentInChildren<DraggableItem>()));} catch (Exception ex) {items.Add(null);}
        return items;
    }

    public void UnloadEquipment(List<DraggableItem> list)
    {
        try {ArmorSlot1.GetComponent<InventorySlot>().SetItem(list[0]);} catch (Exception ex) {}
        try {ArmorSlot2.GetComponent<InventorySlot>().SetItem(list[1]);} catch (Exception ex) {}
        try {ArmorSlot3.GetComponent<InventorySlot>().SetItem(list[2]);} catch (Exception ex) {}
        try {ArmorSlot4.GetComponent<InventorySlot>().SetItem(list[3]);} catch (Exception ex) {}
        try {WeaponSlot.GetComponent<InventorySlot>().SetItem(list[4]);} catch (Exception ex) {}
    }
}
