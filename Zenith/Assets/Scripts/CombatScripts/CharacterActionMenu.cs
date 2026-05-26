using UnityEngine;
using UnityEngine.UI;
using System;

public class CharacterActionMenu : MonoBehaviour
{
    public Button attackButton;
    public Button moveButton;
    public Button endButton;

    public event Action<string> OnActionSelected;

    void Awake()
    {
        attackButton.onClick.AddListener(() => OnActionSelected?.Invoke("Attack"));
        moveButton.onClick.AddListener(() => OnActionSelected?.Invoke("Move"));
        endButton.onClick.AddListener(() => OnActionSelected?.Invoke("EndTurn"));
        gameObject.SetActive(false);
    }

    public void Show(Vector3 worldPos)
    {
        gameObject.SetActive(true);
        transform.position = worldPos;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
