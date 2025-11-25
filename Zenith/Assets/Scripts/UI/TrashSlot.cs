using UnityEngine;
using UnityEngine.EventSystems;

public class TrashSlot : MonoBehaviour, IDropHandler
{
    public virtual void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;

        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();
        draggableItem._parentAfterDrag = transform.GetChild(0);

        if (transform.GetChild(0).childCount > 0)
        {
            Destroy(transform.GetChild(0).GetChild(0).gameObject);
        }
    }
//     public void OnDrop(PointerEventData eventData)
//     {
//         Destroy(eventData.pointerDrag);
//     }
}
