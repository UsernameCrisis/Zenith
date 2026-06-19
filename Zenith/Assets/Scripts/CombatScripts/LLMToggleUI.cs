using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LLMToggleUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle llmToggle;

    [Header("Optional label that shows the current state")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Header("Toggle Colors")]
    [SerializeField] private Color toggleOnColor  = new Color(0.2f, 0.8f, 0.2f); // green
    [SerializeField] private Color toggleOffColor = Color.white;

    void Start()
    {
        if (llmToggle == null)
        {
            Debug.LogError("[LLMToggleUI] llmToggle is not assigned in the Inspector!");
            return;
        }

        bool currentValue = GameManager.Instance != null && GameManager.Instance.useLLMForNPCs;
        llmToggle.isOn = currentValue;

        // RefreshVisuals(currentValue);

        llmToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnDestroy()
    {
        if (llmToggle != null)
            llmToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LLMToggleUI] GameManager.Instance is null — toggle value not saved.");
            return;
        }

        GameManager.Instance.useLLMForNPCs = isOn;
        // RefreshVisuals(isOn);

        Debug.Log($"[LLMToggleUI] LLM NPCs set to: {isOn}");
    }

    private void RefreshVisuals(bool isOn)
    {
        // Color the toggle label text to give clear feedback.
        TextMeshProUGUI label = llmToggle.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.color = isOn ? toggleOnColor : toggleOffColor;

        if (statusLabel != null)
            statusLabel.text = isOn ? "NPC allies will use the LLM brain"
                                    : "NPC allies will use the Behavior Tree";
    }
}