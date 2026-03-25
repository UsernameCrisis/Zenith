using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootEntry
{
    public BaseItem item;

    [Header("Weight per Gold Spent")]
    public float weight0to1k = 0;
    public float weight1kto5k = 0;
    public float weight5kto10k = 0;
    public float weightOver10k = 0;

    public float GetWeight(int goldSpent)
    {
        if (goldSpent < 1000) return weight0to1k;
        if (goldSpent < 5000) return weight1kto5k;
        if (goldSpent < 10000) return weight5kto10k;
        return weightOver10k;
    }
}

[CreateAssetMenu(fileName = "New Loot Table", menuName = "Inventory/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> tableEntries;

    public BaseItem PickItem(int goldSpent)
    {
        float totalWeight = 0;
        foreach (var entry in tableEntries)
            totalWeight += entry.GetWeight(goldSpent);

        if (totalWeight <= 0) return null;

        float roll = Random.Range(0, totalWeight);
        float cursor = 0;

        foreach (var entry in tableEntries)
        {
            cursor += entry.GetWeight(goldSpent);
            if (roll <= cursor) return entry.item;
        }

        return null;
    }
}