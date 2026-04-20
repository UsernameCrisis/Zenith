using UnityEngine;

public enum ItemType { Junk, Consumable, Equipment }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

public abstract class BaseItem : ScriptableObject
{
    [Header("Identity")]
    public string itemID;

    [Header("Visuals")]
    public Sprite icon;
    public string itemName;
    [TextArea] public string description;

    [Header("Classification")]
    public ItemType type;
    public ItemRarity rarity;

    [Header("Stats & Economy")]
    public bool isStackable;
    public int maxStackSize = 99;
    public int sellPrice;
    public float weight;

    public abstract void Use();

    private void OnValidate()
    {
        itemID = name;
    }
}