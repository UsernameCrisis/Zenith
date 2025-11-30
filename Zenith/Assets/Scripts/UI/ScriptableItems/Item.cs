using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    public Sprite sprite;
    public string name;
    public bool stackable;
    public int stackable_limit;
    public int value_min;
    public int value_max;
    public int stat_value; 
    private int value;
    public enum Item_Type
    {
        Consumable,
        Weapon,
        Helmet,
        Chestplate,
        Legs,
        Boots
    }
    public Item_Type type;
    public void Initialize()
    {
        value = Random.Range(value_min, value_max);
    }
    public int GetValue() {return value;}
}
