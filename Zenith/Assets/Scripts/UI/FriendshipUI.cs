using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FriendshipUI : MonoBehaviour
{
    [System.Serializable]
    public struct FriendshipTierVisuals
    {
        public string tierName;
        public int maxPointsForTier;
        public Sprite logoSprite;
        public Color tierColor;
    }

    [Header("UI Components")]
    [SerializeField] private GameObject friendshipPanel;
    [SerializeField] private Slider friendshipSlider;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI numericalText;

    [Header("Dynamic Graphic Elements")]
    [SerializeField] private Image friendshipLogo;
    [SerializeField] private Image sliderFillImage;
    [SerializeField] private Image sliderBackgroundImage;

    [Header("Friendship Tier Visual Configurations")]
    [Tooltip("Configure your tiers from lowest points to highest points.")]
    [SerializeField] private FriendshipTierVisuals[] friendshipTiers;

    private void Start()
    {
        friendshipPanel.SetActive(false);
    }

    public void DisplayFriendship(string npcID)
    {
        if (GameManager.Instance == null) return;

        friendshipPanel.SetActive(true);

        NPCSaveData data = GameManager.Instance.GetNPCData(npcID);
        int currentPoints = data.friendship;

        friendshipSlider.maxValue = 2000f;
        friendshipSlider.value = Mathf.Clamp(currentPoints, 0, 2000);

        if (numericalText != null)
        {
            numericalText.text = $"{currentPoints} / 2000";
        }

        UpdateTierVisuals(currentPoints);
    }

    public void HideFriendship()
    {
        friendshipPanel.SetActive(false);
    }

    private void UpdateTierVisuals(int score)
    {
        string statusString = GetAttitudeString(score);
        if (statusText != null) statusText.text = statusString;

        FriendshipTierVisuals activeVisuals = new FriendshipTierVisuals();
        bool visualsFound = false;

        for (int i = 0; i < friendshipTiers.Length; i++)
        {
            if (score <= friendshipTiers[i].maxPointsForTier)
            {
                activeVisuals = friendshipTiers[i];
                visualsFound = true;
                break;
            }
        }

        if (!visualsFound && friendshipTiers.Length > 0)
        {
            activeVisuals = friendshipTiers[friendshipTiers.Length - 1];
        }

        if (friendshipTiers.Length > 0)
        {
            if (friendshipLogo != null && activeVisuals.logoSprite != null)
            {
                friendshipLogo.sprite = activeVisuals.logoSprite;
            }

            if (sliderFillImage != null)
            {
                sliderFillImage.color = new Color(activeVisuals.tierColor.r, activeVisuals.tierColor.g, activeVisuals.tierColor.b, 1f);
            }

            if (sliderBackgroundImage != null)
            {
                sliderBackgroundImage.color = new Color(activeVisuals.tierColor.r, activeVisuals.tierColor.g, activeVisuals.tierColor.b, 1f);
            }

            if (statusText != null)
            {
                statusText.text = statusString;
                statusText.color = new Color(activeVisuals.tierColor.r, activeVisuals.tierColor.g, activeVisuals.tierColor.b, 1f);
            }
        }
    }

    private string GetAttitudeString(int score)
    {
        if (score <= -51) return "Dislike";
        if (score <= -1) return "Annoyed";
        if (score <= 200) return "Neutral";
        if (score <= 500) return "Polite";
        if (score <= 1000) return "Acquaintance";
        if (score <= 1500) return "Friendly";
        return "Very Friendly";
    }
}