using System.Collections.Generic;
using UnityEngine;

public class InventoryRight : MonoBehaviour
{
    [SerializeField] private GameObject _rowPrefab;
    [SerializeField] private List<GameObject> rows = new List<GameObject>();

    public void CheckIfNeededNewRow()
    {
        //new row
        if (!rows[rows.Count - 1].GetComponent<InventoryRow>().RowIsEmpty())
        {
            Debug.Log("AddRow");
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
}
