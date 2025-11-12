using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public enum Item_Type
    {
        Any,
        Helmet,
        Chestplate,
        Legs,
        Boots,
        Weapon
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

        if (transform.childCount == 0)
        {
            DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
            draggableItem._parentAfterDrag = transform;
        }
    }

    public void SetItem(Item item)
    {
        DraggableItem newDraggableItem = Instantiate(draggableItemPrefab);
        newDraggableItem.item = item;
        newDraggableItem.Initialize();
        newDraggableItem.transform.SetParent(transform, false);
    }
    
    public bool ContainsItem() {return transform.childCount > 0;}
}
