using System;
using System.Collections;
using System.Collections.Generic;
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
    public int stat_value;
    public enum Rarity
    {
        Commmon,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    public int tier = 1;
    public float range_distance = 0.5f;
    public Rarity rarity;
    private float[] rarity_multiplier = {1.0f, 2.0f, 3.0f, 4.0f, 5.0f};
    private int rarity_num;

    void Start()
    {
        try {rarity = item.rarity;} catch (Exception e) {rarity = Rarity.Commmon;}
        switch (rarity)
        {
            case Rarity.Commmon:
                rarity_num = 1;
                break;
            case Rarity.Uncommon:
                rarity_num = 2;
                break;
            case Rarity.Rare:
                rarity_num = 3;
                break;
            case Rarity.Epic:
                rarity_num = 4;
                break;
            case Rarity.Legendary:
                rarity_num = 5;
                break;
        }
        if (value != 0) return;
        value = UnityEngine.Random.Range(item.value_min, item.value_max);
    }

    public void InitializeItemValues()
    {
        if (item.type == Item.Item_Type.Consumable) return;

        float rarity_multiplier_floor = rarity_multiplier[rarity_num] - ((range_distance * UnityEngine.Random.Range(0.8f, 1.2f)) / 2);
        float rarity_multiplier_ceiling = rarity_multiplier[rarity_num] + ((range_distance * UnityEngine.Random.Range(0.8f, 1.2f)) / 2);
        float tier_multiplier_floor = 1.0f + (0.5f * tier * (1/3));
        float tier_multiplier_ceiling = 1.0f + (0.5f * tier * (1/3)) + range_distance;

        stat_value = (int)Mathf.Round(item.base_stat_value * UnityEngine.Random.Range(tier_multiplier_floor, tier_multiplier_ceiling) * UnityEngine.Random.Range(rarity_multiplier_floor, rarity_multiplier_ceiling));
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
        _itemDescription.SetData(item.name, value, quantity, item.description, rarity, this);
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
