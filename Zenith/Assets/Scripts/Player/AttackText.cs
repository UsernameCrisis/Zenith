using TMPro;
using UnityEngine;

public class AttackText : MonoBehaviour
{
    void Update()
    {
        string t = "Atk: " + FindAnyObjectByType<PlayerOverworldAttributes>().Atk.ToString();
        GetComponent<TMP_Text>().text = t;
    }
}
