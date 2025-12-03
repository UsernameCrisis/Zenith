using System;
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
    public GameObject ArmorSlot1;
    public GameObject ArmorSlot2;
    public GameObject ArmorSlot3;
    public GameObject ArmorSlot4;
    public GameObject WeaponSlot;

    public void SetGearActive()
    {
        ChestItemsUI.SetActive(false);
        GearPanel.SetActive(true);
    }
    
    public void SetInteractableInventoryActive()
    {
        GearPanel.SetActive(false);
        ChestItemsUI.SetActive(true);
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
}
