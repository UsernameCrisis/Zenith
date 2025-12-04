using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SellMenu : ItemActionMenuChild, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!hover) return;
        if (!FindAnyObjectByType<OverworldUI>().inventory.isSelling) return;

        draggableItem.Sell();
    }
}
