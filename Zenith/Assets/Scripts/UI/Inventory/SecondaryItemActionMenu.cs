using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.EventSystems;

public class SecondaryItemActionMenu : MonoBehaviour, IPointerExitHandler, IPointerDownHandler
{
    public ItemActionMenuChild button;
    private bool canClick = false;
    private DraggableItem.SlotType slotType;
    private GameObject ParentGameObject;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!canClick) return;

        switch (slotType)
        {
            case DraggableItem.SlotType.Chest:
                ParentGameObject.GetComponent<DraggableItem>().Take();
                break;
            case DraggableItem.SlotType.Shop:
                ParentGameObject.GetComponent<DraggableItem>().Buy();
                break;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
       gameObject.SetActive(false);
    }
    public void SetGameObject(GameObject gameObject)
    {
        ParentGameObject = gameObject;
    }
    public void SetType(DraggableItem.SlotType type, int value)
    {
        slotType = type;
        switch (slotType)
        {
            case DraggableItem.SlotType.Chest:
                button.text.text = "Take";
                button.text.color = FindAnyObjectByType<PlayerOverworldAttributes>().inventory.CanInsertToInventory(ParentGameObject.GetComponent<DraggableItem>()) ? Color.green : Color.red;
                canClick = FindAnyObjectByType<PlayerOverworldAttributes>().inventory.CanInsertToInventory(ParentGameObject.GetComponent<DraggableItem>()) ? true : false;
                break;
            case DraggableItem.SlotType.Shop:
                button.text.text = "Buy";
                button.text.color = (FindAnyObjectByType<PlayerOverworldAttributes>().gold >= value) ? Color.green : Color.red;
                canClick = (FindAnyObjectByType<PlayerOverworldAttributes>().gold >= value) ? true : false;
                break;
        }
    }
}
