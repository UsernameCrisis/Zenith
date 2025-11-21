using TMPro;
using UnityEngine;

public class ItemDescription : MonoBehaviour
{
    public GameObject Text;
    public GameObject GoldValueText;

    public void SetData(string name, int value)
    {
        Text.GetComponent<TMP_Text>().text = name;
        GoldValueText.GetComponent<TMP_Text>().text = value.ToString();
    }
}
