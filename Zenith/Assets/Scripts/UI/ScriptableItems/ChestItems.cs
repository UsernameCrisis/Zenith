using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestItems", menuName = "Scriptable Objects/ChestItems")]
public class ChestItems : ScriptableObject
{
    public List<Item> PossibleItems;
    public List<float> ChancePercentage;
    public List<int> MinRange;
    public List<int> MaxRange;
    private List<List<Item>> items;

    public void Awake()
    {
        items = new List<List<Item>>();
        for (int i = 0; i < PossibleItems.Count; i++)
        {
            if (Random.Range(0, 100) <= ChancePercentage[i])
            {
                AddItem(PossibleItems[i]);
            }
        }
    }
    private void AddItem(Item item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            for (int j = 0; i < items[j].Count; j++)
            {
                if (items[i][j] == null) items[i][j] = item;
            }
        }
    }

    public List<List<Item>> GetItems()
    {
        return items;
    }
}