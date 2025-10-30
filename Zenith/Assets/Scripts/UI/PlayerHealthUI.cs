using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    public TextMeshProUGUI hpText;

    [Header("Visual Feedback")]
    [SerializeField] private RectTransform hpBarTransform;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Color normalColor = Color.red;


    public void UpdateUI()
    {
        int current = GameManager.Instance.Player.PlayerData.getHP();
        int max = GameManager.Instance.Player.PlayerData.getMaxHP();
        int previous = (int)hpBar.value;

        hpBar.maxValue = max;
        hpBar.value = current;

        string currentFormatted = FormatHP(current);
        string maxFormatted = FormatHP(max);
        hpText.text = $"{currentFormatted}/{maxFormatted}";

        if (current < previous)
            StartCoroutine(ShakeHPBar());
        StartCoroutine(FlashFill());
    }
    public void ShowHPText()
    {
        Debug.Log("Showing");
        hpText.enabled = true;
    }

    public void HideHPText()
    {
        Debug.Log("Not Showing");
        hpText.enabled = false;
    }


    string FormatHP(int value)
    {
        if (value >= 1_000_000)
            return (value / 1_000_000f).ToString("0.#") + "M";
        else if (value >= 1_000)
            return (value / 1_000f).ToString("0.#") + "k";
        else
            return value.ToString();
    }

    IEnumerator ShakeHPBar(float duration = 0.2f, float magnitude = 10f)
    {
        Vector3 originalPos = hpBarTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            hpBarTransform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        hpBarTransform.localPosition = originalPos;
    }

    IEnumerator FlashFill(float duration = 0.2f)
    {
        fillImage.color = flashColor;
        yield return new WaitForSeconds(duration);
        fillImage.color = normalColor;
    }
}