using UnityEngine;
using System.Collections.Generic;

public class InventoryDisplayUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject rowPrefab;

    public GameObject tooltipPanel;

    public void RefreshUI()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemStack stack in InventoryManager.Instance.mainInventory)
        {
            GameObject newRow = Instantiate(rowPrefab, contentParent);
            newRow.GetComponent<InventoryRowUI>().Setup(stack, this);
        }
    }

    void OnEnable() => RefreshUI();

    public void ShowTooltip(BaseItem item) { /* Tooltip logic here later */ }
    public void HideTooltip() { /* Tooltip logic here later */ }
}