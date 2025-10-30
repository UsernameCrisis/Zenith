using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{

    public void UpdateAmount()
    {
        TMP_Text text = GetComponentInChildren<TMP_Text>();

        text.text = GameManager.Instance.Player.PlayerData.Gold.ToString();
    }
}
