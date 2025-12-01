using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public Transform _parentAfterDrag;
    public Item item;
    private bool _isHovering = false;
    public ItemDescription _itemDescription;
    public int value;
    public int quantity = 1;

    void Start()
    {
        if (value != 0) return;
        value = UnityEngine.Random.Range(item.value_min, item.value_max);
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
        _itemDescription = FindAnyObjectByType<OverworldUI>().ItemDescriptionObject.GetComponent<ItemDescription>();
        _isHovering = true;
        _itemDescription.SetData(item.name, value, quantity);
        StartCoroutine(ShowDescription());
    }

    public void OnPointerExit(PointerEventData eventData) 
    {
        _isHovering = false;
        FindAnyObjectByType<OverworldUI>().ItemDescriptionObject.SetActive(false);
    }

    private IEnumerator ShowDescription()
    {
        yield return new WaitForSeconds(0.3f);
        if (_isHovering)
            FindAnyObjectByType<OverworldUI>().ItemDescriptionObject.SetActive(true);
    }
}
