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
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemStack stack in InventoryManager.Instance.mainInventory)
        {
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
        RefreshUI();
        InventoryManager.Instance.OnInventoryChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RefreshUI;
    }

    public void ShowTooltip(BaseItem item) { /* Tooltip logic here later */ }
    public void HideTooltip() { /* Tooltip logic here later */ }
}