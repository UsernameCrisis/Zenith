using TMPro;
using UnityEngine;

public class DefenseText : MonoBehaviour
{
    public void UpdateNumber()
    {
        string t = "Def: " + GameManager.Instance.playerDef.ToString();
        GetComponent<TMP_Text>().text = t;
    }
}
