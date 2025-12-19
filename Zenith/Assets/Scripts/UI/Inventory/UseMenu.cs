using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UseMenu : ItemActionMenuChild, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!hover) return;

        draggableItem.item.ConsumableScript.OnConsume();
        draggableItem.Consume();
    }
}
