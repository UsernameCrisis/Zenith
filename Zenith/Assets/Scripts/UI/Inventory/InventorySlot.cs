using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject _hoverIndicator;
    public TMP_Text QuantityText;
    public enum Item_Type
    {
        Any,
        Helmet,
        Chestplate,
        Legs,
        Boots,
        Weapon,
    }
    public Item_Type AcceptedType = Item_Type.Any;
    public DraggableItem draggableItemPrefab;
    private void Start() {
        UpdateQuantity();
    }
    public virtual void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;

        //type check
        if (AcceptedType != Item_Type.Any)
        {
            if (eventData.pointerDrag.GetComponent<DraggableItem>().item.type.ToString() != AcceptedType.ToString())
            {
                return;
            }
        }

        //combine same item
        if (ContainsItem())
        {
            if (GetItem().item == dropped.GetComponent<DraggableItem>().item)
            {
                GetItem().quantity += dropped.GetComponent<DraggableItem>().quantity;
                UpdateQuantity();
                Destroy(dropped);
            }
        }
        //move item
        else
        {
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            draggableItem._parentAfterDrag = transform.GetChild(0);
        }
        UpdateQuantity();
        // FindAnyObjectByType<PlayerOverworldAttributes>().UpdateStats();
    }

    // public void SetItem(DraggableItem item)
    // {
    //     DraggableItem newDraggableItem = Instantiate(draggableItemPrefab);
    //     newDraggableItem.item = item.item;
    //     newDraggableItem.transform.SetParent(transform.GetChild(0), false);
    //     UpdateQuantity();
    // }
    public void SetItem(DraggableItem item)
    {
        item.transform.SetParent(transform.GetChild(0), false);
        UpdateQuantity();
    }
    public bool ContainsItem() {return transform.GetChild(0).childCount > 0;}
    public DraggableItem GetSlotItem()
    {
        if (!ContainsItem()) return null;

        return Instantiate(GetComponentInChildren<DraggableItem>());
    }

    public void UnloadItem(DraggableItem item)
    {
        if (!ContainsItem())
        {
            SetItem(item);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Color c = _hoverIndicator.GetComponent<Image>().color;
        c.a = 1f;
        _hoverIndicator.GetComponent<Image>().color = c;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Color c = _hoverIndicator.GetComponent<Image>().color;
        c.a = 0f;
        _hoverIndicator.GetComponent<Image>().color = c;
    }

    public bool HasThisItem(DraggableItem item)
    {
        if (!ContainsItem()) return false;
        return GetItem().item == item.item && item.item.stackable && (GetItem().quantity < item.item.stackable_limit);
    }

    private DraggableItem GetItem()
    {
        try {return transform.GetChild(0).GetChild(0).GetComponent<DraggableItem>();} catch (Exception e) {return null;}
    }
    public void AddExistingItem(DraggableItem item)
    {
        GetItem().quantity += item.quantity;
        UpdateQuantity();
    }
    public void UpdateQuantity()
    {
        if (!ContainsItem()) {QuantityText.text = ""; return;}
        if (GetItem().quantity == 0 || GetItem().quantity == 1) {QuantityText.text = ""; return;}
        QuantityText.text = FormatQuantity(GetItem().quantity);
    }
    private string FormatQuantity(int n)
    {
        string t = "";
        if (n < 10) t = "    ";
        else if (n < 100) t = "   ";
        t += n.ToString();
        return t;
    }

    public void RemoveItem()
    {
        Destroy(transform.GetChild(0).GetChild(0).gameObject);
    }
}
