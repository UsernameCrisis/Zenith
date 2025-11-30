using System.Collections.Generic;
using UnityEngine;

public class InventoryRight : MonoBehaviour
{
    [SerializeField] private GameObject _rowPrefab;
    [SerializeField] private List<GameObject> rows = new List<GameObject>();

    public void CheckIfNeededNewRow()
    {
        if (!transform.parent.GetComponent<Inventory>().expandable) return;
        //new row
        if (!rows[rows.Count - 1].GetComponent<InventoryRow>().RowIsEmpty())
        {
            GameObject newRow = Instantiate(_rowPrefab);
            rows.Add(newRow);
            newRow.transform.SetParent(gameObject.transform);
            newRow.transform.localScale = rows[0].transform.localScale;
            return;
        }
        //delete row
        for (int i = rows.Count - 1; i > 1; i--)
        {
            if (rows[i].GetComponent<InventoryRow>().RowIsEmpty() && rows[i - 1].GetComponent<InventoryRow>().RowIsEmpty())
            {
                GameObject toBeRemovedRow = rows[i];
                Destroy(toBeRemovedRow);
                rows.Remove(toBeRemovedRow);
            }
        }
    }

    public void AddItem(Item item)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].GetComponent<InventoryRow>().RowHasEmptySlot())
            {
                rows[i].GetComponent<InventoryRow>().AddItemToNextEmptySlot(item);
                // CheckIfNeededNewRow();
                return;
            }
        }
    }

    public List<List<Item>> GetItems()
    {
        List<List<Item>> list = new List<List<Item>>();
        for (int i = 0; i < rows.Count; i++)
        {
            list.Add(rows[i].GetComponent<InventoryRow>().GetAsList());
        }
        return list;
    }

    public void UnloadItems(List<List<Item>> list)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            rows[i].GetComponent<InventoryRow>().UnloadList(list[i]);
        }
    }
}
