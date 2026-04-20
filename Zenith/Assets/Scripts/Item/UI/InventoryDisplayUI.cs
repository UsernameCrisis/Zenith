using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryDisplayUI : MonoBehaviour
{
    public Transform contentParent;
    public GameObject rowPrefab;

    public GameObject tooltipPanel;
    public TMP_Text weightText;

    public void RefreshUI()
    {
        if (this == null || InventoryManager.Instance == null || contentParent == null)
            return;

        foreach (Transform child in contentParent)
        {
            if (child != null) Destroy(child.gameObject);
        }

        foreach (ItemStack stack in InventoryManager.Instance.mainInventory)
        {
            if (stack == null || stack.itemData == null) continue;

            GameObject newRow = Instantiate(rowPrefab, contentParent);
            newRow.GetComponent<InventorySlot>().Setup(stack, this);
        }

        if (weightText != null)
        {
            float current = InventoryManager.Instance.currentWeight;
            float max = InventoryManager.Instance.maxWeight;
            weightText.text = $"{current:F1} / {max:F1} kg";
        }
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
            InventoryManager.Instance.OnInventoryChanged += RefreshUI;
        }
        RefreshUI();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
        }
    }

    public void ShowTooltip(BaseItem item) { /* Tooltip logic here later */ }
    public void HideTooltip() { /* Tooltip logic here later */ }
}