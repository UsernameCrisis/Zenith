using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    public Sprite sprite;
    public Sprite UILogo;
    public string name;
    public bool stackable;
    public int stackable_limit;
    public int value_min;
    public int value_max;
    public float base_stat_value; 
    public string description;
    public ItemBaseScript ConsumableScript;
    public bool HasTimer = false;
    public enum Item_Type
    {
        Consumable,
        Weapon,
        Helmet,
        Chestplate,
        Legs,
        Boots,
        Junk
    }
    public Item_Type type;
    public DraggableItem.Rarity rarity;
}
