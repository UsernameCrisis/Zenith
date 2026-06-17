using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class StatSliderRow : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_InputField inputField;

    public event Action<int> OnValueChanged;

    private int currentValue;
    private bool isSyncing = false;

    public int Value => currentValue;

    public void Initialize(string labelText, int minValue, int maxValue, int defaultValue)
    {
        if (label != null)
            label.text = labelText;

        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = true;

        SetValueInternal(defaultValue, notify: false);

        slider.onValueChanged.AddListener(OnSliderChanged);
        inputField.onEndEdit.AddListener(OnInputFieldChanged);
    }

    public void ResetToDefault(int defaultValue)
    {
        SetValueInternal(defaultValue, notify: true);
    }

    private void OnSliderChanged(float newValue)
    {
        if (isSyncing) return;
        SetValueInternal(Mathf.RoundToInt(newValue), notify: true);
    }

    private void OnInputFieldChanged(string text)
    {
        if (isSyncing) return;

        if (!int.TryParse(text, out int parsed))
        {
            RefreshInputFieldText();
            return;
        }

        int clamped = Mathf.Clamp(parsed, (int)slider.minValue, (int)slider.maxValue);
        SetValueInternal(clamped, notify: true);
    }

    private void SetValueInternal(int value, bool notify)
    {
        isSyncing = true;

        currentValue = value;
        slider.value = value;
        RefreshInputFieldText();

        isSyncing = false;

        if (notify)
            OnValueChanged?.Invoke(currentValue);
    }

    private void RefreshInputFieldText()
    {
        inputField.text = currentValue.ToString();
    }
}
