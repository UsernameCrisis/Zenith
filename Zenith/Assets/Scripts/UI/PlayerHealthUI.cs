using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Analytics;

public class OverworldUI : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private GameObject Gold;
    [SerializeField] private PlayerOverworldAttributes playerOverworldAttributes;

    [Header("Visual Feedback")]
    [SerializeField] private RectTransform hpBarTransform;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Color normalColor = Color.red;
    [Header("OtherUI")]
    public Inventory inventory;
    public GameObject interactUI;
    public GameObject ItemDescriptionObject;
    private Vector2 latestMousePos;
    public ItemActionMenu itemActionMenu;


    void Awake()
    {
        if (playerOverworldAttributes != null)
        {
            playerOverworldAttributes.HealthChanged += UpdateUI;
        }
    }

    void Start()
    {
        hpText.enabled = false;
        Gold.SetActive(false);
        UpdateUI(playerOverworldAttributes.currentHP, playerOverworldAttributes.maxHP);
        inventory.ToggleInventory();
        interactUI.SetActive(false);
    }

    void Update()
    {
        //inventory
        if (InputSystem.actions.FindAction("Inventory").WasPressedThisFrame())
        {
            inventory.ToggleInventory();
            inventory.GetComponentInChildren<InventoryLeft>().SetGearActive();
        } 
        //itemdescriptionobject
        if (latestMousePos != InputSystem.actions.FindAction("MousePosition").ReadValue<Vector2>()) {
            latestMousePos = InputSystem.actions.FindAction("MousePosition").ReadValue<Vector2>();
            ItemDescriptionObject.transform.position = latestMousePos 
            - ((latestMousePos.y >= 540)? new Vector2(0, 180) : new Vector2(0, -180))
            + ((latestMousePos.x <= 960)? new Vector2(120, 0) : new Vector2(-120, 0));
        }
    }

    void UpdateUI(int current, int max)
    {
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
        Gold.SetActive(true);
    }

    public void HideHPText()
    {
        Debug.Log("Not Showing");
        hpText.enabled = false;
        Gold.SetActive(false);
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