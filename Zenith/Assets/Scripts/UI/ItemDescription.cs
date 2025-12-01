using TMPro;
using UnityEngine;

public class ItemDescription : MonoBehaviour
{
    public GameObject Text;
    public GameObject GoldValueText;
    public GameObject QuantityText;

    public void SetData(string name, int value, int quantity)
    {
        Text.GetComponent<TMP_Text>().text = name;
        GoldValueText.GetComponent<TMP_Text>().text = value.ToString();
        QuantityText.GetComponent<TMP_Text>().text = quantity.ToString();
    }
}
