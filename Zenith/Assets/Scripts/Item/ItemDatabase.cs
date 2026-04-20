using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<BaseItem> allItems;

    public BaseItem GetItemByID(string id)
    {
        return allItems.Find(i => i.itemID == id);
    }
}