using UnityEngine;

[CreateAssetMenu(fileName = "New Equipment", menuName = "Inventory/Items/Equipment")]
public class EquipmentItem : BaseItem
{
    public enum EquipSlot { Weapon, Head, Chest, Legs }

    [Header("Equipment Stats")]
    public EquipSlot slot;
    public int attackBonus;
    public int defenseBonus;
    public int speedBonus;
    public int maxHpBonus;

    public override void Use()
    {
        InventoryManager.Instance.EquipItem(this);
    }
}