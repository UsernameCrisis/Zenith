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
        Debug.Log(FindAnyObjectByType<OverworldUI>().PotionBuffPanel == null);
        if (draggableItem.item.HasTimer) FindAnyObjectByType<OverworldUI>().PotionBuffPanel.AddItem(draggableItem);
    }
}
