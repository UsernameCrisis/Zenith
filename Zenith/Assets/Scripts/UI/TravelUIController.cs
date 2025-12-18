using UnityEngine;
using UnityEngine.UI;

public class TravelUIController : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement playerMovement;
    public GameObject inventoryUI;
    public Button closeButton;

    private void Awake()
    {
        gameObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }

    private void OnEnable()
    {
        if (playerMovement != null)
            playerMovement.canMove(false);

        if (inventoryUI != null && inventoryUI.activeSelf)
            inventoryUI.SetActive(false);
    }

    private void OnDisable()
    {
        if (playerMovement != null)
            playerMovement.canMove(true);
    }

    private void CloseUI()
    {
        gameObject.SetActive(false);
    }
}
