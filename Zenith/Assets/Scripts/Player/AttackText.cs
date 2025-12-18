using TMPro;
using UnityEngine;

public class AttackText : MonoBehaviour
{
    public void UpdateNumber()
    {
        string t = "Atk: " + GameManager.Instance.playerAtk.ToString();
        GetComponent<TMP_Text>().text = t;
    }
}
