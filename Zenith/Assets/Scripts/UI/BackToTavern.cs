using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackToTavern : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public CanvasGroup warningUI;
    public Button buttonActivator;
    [SerializeField] private ConfirmationUI confirmationUI;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private TMP_Text warningText;

    [Header("Settings")]
    public float checkRadius = 20f;

    private Collider[] hitResults = new Collider[20];

    private void Start()
    {
        if (buttonActivator != null)
            buttonActivator.onClick.AddListener(CheckSurroundings);

        if (warningUI != null)
        {
            warningUI.alpha = 0f;
            warningUI.gameObject.SetActive(false);
        }
    }

    private void CheckSurroundings()
    {
        if (player == null) return;

        int count = Physics.OverlapSphereNonAlloc(player.transform.position, checkRadius, hitResults, enemyLayer, QueryTriggerInteraction.Collide);

        bool enemyFound = false;
        for (int i = 0; i < count; i++)
        {
            if (hitResults[i].CompareTag("Enemy"))
            {
                enemyFound = true;
                break;
            }
        }

        PlayerOverworldAttributes attributes = player.GetComponent<PlayerOverworldAttributes>();
        if (attributes != null && attributes.currentHP <= 0)
        {
            ShowWarning("You are dead! Unable To Teleport");
        }
        else if (enemyFound)
        {
            ShowWarning("Enemies Nearby! Unable To Teleport.");
        }
        else if (confirmationUI != null)
        {
            confirmationUI.OpenConfirmation("Tavern");
        }
    }


    private void ShowWarning(string message)
    {
        if (warningUI == null) return;

        if (warningText != null)
            warningText.text = message;

        warningUI.gameObject.SetActive(true);
        StartCoroutine(FadeOutWarning());
    }

    private IEnumerator FadeOutWarning()
    {
        float holdDuration = 0.5f;
        float fadeDuration = 0.5f;
        float elapsed = 0f;

        warningUI.alpha = 1f;

        yield return new WaitForSecondsRealtime(holdDuration);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            warningUI.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        warningUI.alpha = 0f;
        warningUI.gameObject.SetActive(false);
    }
}
