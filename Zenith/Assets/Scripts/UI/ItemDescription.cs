using System.Diagnostics;
using TMPro;
using UnityEngine;

public class ItemDescription : MonoBehaviour
{
    public GameObject Text;
    public GameObject GoldValueText;
    public GameObject QuantityText;
    public GameObject ItemDescriptionText;

    public void SetData(string name, int value, int quantity, string description, DraggableItem.Rarity rarity, DraggableItem item)
    {
        Text.GetComponent<TMP_Text>().text = name;
        GoldValueText.GetComponent<TMP_Text>().text = value.ToString();
        QuantityText.GetComponent<TMP_Text>().text = quantity.ToString();

        string full_desc = "";

        switch (item.item.type)
        {
            case Item.Item_Type.Consumable:
                break;
            case Item.Item_Type.Weapon:
                full_desc += "Attack: ";
                full_desc += item.stat_value.ToString();
                break;
            case Item.Item_Type.Helmet:
            case Item.Item_Type.Chestplate:
            case Item.Item_Type.Legs:
            case Item.Item_Type.Boots:
                full_desc += "Defense: ";
                full_desc += item.stat_value.ToString();
                break;
        }
        full_desc += ("\n" + description);

        ItemDescriptionText.GetComponent<TMP_Text>().text = full_desc;



        Color c = new Color();
        switch (rarity)
        {
            case DraggableItem.Rarity.Commmon:
                c = Color.white;
                break;
            case DraggableItem.Rarity.Uncommon:
                c = Color.limeGreen;
                break;
            case DraggableItem.Rarity.Rare:
                c = Color.blue;
                break;
            case DraggableItem.Rarity.Epic:
                c = Color.purple;
                break;
            case DraggableItem.Rarity.Legendary:
                c = Color.gold;
                break;
        }
        Text.GetComponent<TMP_Text>().color = c;
        GoldValueText.GetComponent<TMP_Text>().color = c;
        QuantityText.GetComponent<TMP_Text>().color = c;
        ItemDescriptionText.GetComponent<TMP_Text>().color = c;
    }
}
