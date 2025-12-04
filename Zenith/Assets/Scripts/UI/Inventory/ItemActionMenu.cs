using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemActionMenu : MonoBehaviour, IPointerExitHandler
{
    public ItemActionMenuChild use;
    public ItemActionMenuChild sell;
    public void OnPointerExit(PointerEventData eventData)
    {
       gameObject.SetActive(false);
    }

    public void SetItem(DraggableItem item)
    {
        sell.SetItem(item);
    }

    public void Read(bool consumable, bool canSell)
    {
        use.text.color = consumable ? Color.green : Color.red;

        sell.text.color = canSell ? Color.green : Color.red;
    }
}
