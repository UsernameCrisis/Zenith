using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject _hoverIndicator;
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
    public virtual void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;

        if (AcceptedType != Item_Type.Any)
        {
            if (eventData.pointerDrag.GetComponent<DraggableItem>().item.type.ToString() != AcceptedType.ToString())
            {
                return;
            }
        }

        if (transform.childCount == 1)
        {
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            draggableItem._parentAfterDrag = transform.GetChild(0);
        }
        // FindAnyObjectByType<PlayerOverworldAttributes>().UpdateStats();
    }

    public void SetItem(Item item)
    {
        DraggableItem newDraggableItem = Instantiate(draggableItemPrefab);
        newDraggableItem.item = item;
        newDraggableItem.Initialize();
        newDraggableItem.transform.SetParent(transform.GetChild(0), false);
    }
    
    public bool ContainsItem() {return transform.childCount > 1;}
    public Item GetSlotItem()
    {
        if (!ContainsItem()) return null;

        return GetComponentInChildren<DraggableItem>().item;
    }

    public void UnloadItem(Item item)
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
}
