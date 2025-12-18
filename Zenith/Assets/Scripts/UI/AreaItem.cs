using UnityEngine;
using UnityEngine.UI;

public class AreaItem : MonoBehaviour
{
    [Header("Area Settings")]
    public string sceneName;
    public Button button;

    [Header("Confirmation UI")]
    public ConfirmationUI confirmationUI;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClickArea);
    }

    private void OnClickArea()
    {
        if (confirmationUI != null)
        {
            confirmationUI.OpenConfirmation(sceneName);
        }
    }
}
