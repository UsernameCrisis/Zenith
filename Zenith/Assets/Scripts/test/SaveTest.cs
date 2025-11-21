using System.Collections.Generic;
using UnityEngine;

public class SaveTest : MonoBehaviour
{
    public Inventory inventory;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            List<List<Item>> items = inventory.GetInventoryIn2DList();
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = 0; j < items[i].Count; j++)
                {
                    if (items[i][j] == null) continue;
                    Debug.Log(items[i][j].name);
                }
            }
        }
    }
}
