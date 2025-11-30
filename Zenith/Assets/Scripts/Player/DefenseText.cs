using TMPro;
using UnityEngine;

public class DefenseText : MonoBehaviour
{
    void Update()
    {
        string t = "Def: " + FindAnyObjectByType<PlayerOverworldAttributes>().Def.ToString();
        GetComponent<TMP_Text>().text = t;
    }
}
