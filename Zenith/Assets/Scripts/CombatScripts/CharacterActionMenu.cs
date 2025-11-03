using UnityEngine;
using UnityEngine.UI;
using System;

public class CharacterActionMenu : MonoBehaviour
{
    public Button attackButton;
    public Button moveButton;
    public Button skillButton;
    public Button itemButton;
    public Button endButton;

    public event Action<string> OnActionSelected;

    void Awake()
    {
        attackButton.onClick.AddListener(() => OnActionSelected?.Invoke("Attack"));
        moveButton.onClick.AddListener(() => OnActionSelected?.Invoke("Move"));
        skillButton.onClick.AddListener(() => OnActionSelected?.Invoke("Skill"));
        itemButton.onClick.AddListener(() => OnActionSelected?.Invoke("Item"));
        endButton.onClick.AddListener(() => OnActionSelected?.Invoke("EndTurn"));
        gameObject.SetActive(false);
    }

    public void Show(Vector3 worldPos)
    {
        transform.position = Camera.main.WorldToScreenPoint(worldPos);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
