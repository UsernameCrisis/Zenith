using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public DraggableItem draggableItemPrefab;
    public void OnDrop(PointerEventData eventData)
    {
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
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
