using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemActionMenuChild : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image background;
    protected bool hover = false;
    protected DraggableItem draggableItem;
    public TMP_Text text;
    public void OnPointerEnter(PointerEventData eventData)
    {
        hover = true;
        background.color = Color.lightGray;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hover = false;
        background.color = Color.gray;
    }

    public void SetItem(DraggableItem item)
    {
        draggableItem = item;
    }
}
