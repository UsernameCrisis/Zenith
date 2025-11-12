using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Analytics;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI goldtext;
    [SerializeField] private PlayerOverworldAttributes playerOverworldAttributes;

    [Header("Visual Feedback")]
    [SerializeField] private RectTransform hpBarTransform;
    [SerializeField] private Image fillImage;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Color normalColor = Color.red;
    [HideInInspector] public Inventory inventory;
    public GameObject interactUI;
    private GameObject currentInteractableObject;


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
        goldtext.enabled = false;
        UpdateUI(playerOverworldAttributes.currentHP, playerOverworldAttributes.maxHP);

        inventory = GetComponentInChildren<Inventory>();
        inventory.ToggleInventory();
    }

    void Update()
    {
        if (InputSystem.actions.FindAction("Inventory").WasPressedThisFrame())
        {
            inventory.ToggleInventory();
            inventory.GetComponentInChildren<InventoryLeft>().SetGearActive();
            
        }

        if (InteractUIIsActive())
        {
            if (InputSystem.actions.FindAction("Interact").WasPressedThisFrame())
            {
                if (inventory.gameObject.active)
                {
                    transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.SetActive(false);
                    inventory.GetComponentInChildren<InventoryLeft>().SetGearActive();
                    inventory.SetActive(false);
                    return;
                }
                if (currentInteractableObject != null) currentInteractableObject.GetComponent<Interactable>().OnInteract();
            }
        } 

        // if (inventory.gameObject.active)
        // {
        //     if (InputSystem.actions.FindAction("ExitSelect").WasPressedThisFrame());
        //     {
        //         Debug.Log("hit 2");
        //         inventory.ToggleInventory();
        //     }
        // }   
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
        goldtext.enabled = true;
    }

    public void HideHPText()
    {
        Debug.Log("Not Showing");
        hpText.enabled = false;
        goldtext.enabled = false;
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

    public void ToggleInteractUI(GameObject Interactible)
    {
        //kalo ada yang bisa di interact muncul imagenya
        //terus dilock ke interactible lain sampe ontriggerexit ditrigger ini function lagi

        if (currentInteractableObject == null)
        {
            currentInteractableObject = Interactible;
            interactUI.SetActive(!interactUI.active);
            return;
        }
        if (currentInteractableObject == Interactible)
        {
            interactUI.SetActive(!interactUI.active);
            currentInteractableObject = null;
        }
    }
    public bool InteractUIIsActive() { return interactUI.active; }
    public void EmptyCurrentInteractable() { currentInteractableObject = null; }
}