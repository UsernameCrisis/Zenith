using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipUI : MonoBehaviour
{
    public TMP_Text itemName;
    public TMP_Text itemRarity;
    public TMP_Text itemDescription;
    public TMP_Text itemStats;

    [Header("Settings")]
    public Vector2 padding = new Vector2(20, 10);
    public float fadeSpeed = 12f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isShowing = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (isShowing)
        {
            FollowMouse();
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
        }
        else
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    private void FollowMouse()
    {
        Vector2 mousePos = Input.mousePosition;

        float pivotY = (mousePos.y > Screen.height / 2) ? 1 : 0;
        rectTransform.pivot = new Vector2(1, pivotY);

        float xOffset = -padding.x;
        float yOffset = (pivotY == 1 ? -padding.y : padding.y);

        transform.position = mousePos + new Vector2(xOffset, yOffset);
    }

    public void UpdateTooltip(BaseItem item)
    {
        itemName.text = item.itemName;
        itemRarity.text = item.rarity.ToString();
        itemRarity.color = GetRarityColor(item.rarity);
        itemDescription.text = item.description;
        itemStats.text = $"Weight: {item.weight} | Value: {item.sellPrice}g";

        isShowing = true;
    }

    public void HideTooltip()
    {
        isShowing = false;
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.8f, 0.8f, 0.8f),
            ItemRarity.Uncommon => Color.green,
            ItemRarity.Rare => Color.cyan,
            ItemRarity.Epic => Color.magenta,
            ItemRarity.Legendary => new Color(1f, 0.5f, 0f),
            _ => Color.white
        };
    }
}