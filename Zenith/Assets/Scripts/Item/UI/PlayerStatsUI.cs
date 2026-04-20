using UnityEngine;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    public TMP_Text atkText, defText, spdText, hpText;

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateStatsDisplay;
            InventoryManager.Instance.OnInventoryChanged += UpdateStatsDisplay;
        }
        UpdateStatsDisplay();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= UpdateStatsDisplay;
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= UpdateStatsDisplay;
    }

    public void UpdateStatsDisplay()
    {
        if (this == null) return;

        var gm = GameManager.Instance;

        if (gm == null) return;

        if (atkText == null || defText == null || spdText == null || hpText == null)
        {
            return;
        }

        atkText.text = $"<color=#FF0000>ATK: {gm.playerAtk}</color>";
        defText.text = $"<color=#0000FF>DEF: {gm.playerDef}</color>";
        spdText.text = $"<color=#00CCFF>SPD: {gm.playerSpeed}</color>";
        hpText.text = $"<color=#00FF00>MaxHP: {gm.playerMaxHP}</color>";
    }
}