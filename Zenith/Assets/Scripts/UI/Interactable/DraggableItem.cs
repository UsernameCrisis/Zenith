using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public Transform _parentAfterDrag;
    public Item item;

    void Start()
    {
        Debug.Log(this.GetComponent<Image>().raycastTarget);
    }

    public void Initialize()
    {
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _parentAfterDrag = transform.parent;
        transform.SetParent(FindAnyObjectByType<Canvas>().transform, true);
        transform.SetAsLastSibling();
        this.GetComponent<Image>().raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = InputSystem.actions.FindAction("MousePosition").ReadValue<Vector2>();
        // transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(_parentAfterDrag);
        this.GetComponent<Image>().raycastTarget = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        FindAnyObjectByType<OverworldUI>().ItemDescriptionObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FindAnyObjectByType<OverworldUI>().ItemDescriptionObject.SetActive(false);
    }
}
