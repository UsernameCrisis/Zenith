using System.Collections.Generic;
using UnityEngine;

public class InteractableMerchant : InteractableObject, Interactable
{
    public ItemTable MerchantItemTable;
    private bool _isInitialized = false;
    private List<DraggableItem> items= new List<DraggableItem>();
    private List<ItemData> itemdata= new List<ItemData>();
    public DraggableItem DraggableItemPrefab;
    public void OnInteract()
    {
        if (!_isInitialized) InitializeItems();

        FindAnyObjectByType<OverworldUI>().shopUIController.LoadItems(itemdata);

        GameObject Inventory = FindAnyObjectByType<OverworldUI>().inventory.gameObject;
        Inventory.SetActive(true);
        Inventory.GetComponent<Inventory>().isSelling = true;
        Inventory.GetComponentInChildren<InventoryLeft>().SetShopActive();
    }

    private void InitializeItems()
    {
        for (int i = 0; i < MerchantItemTable.PossibleItems.Count; i++)
        {
            if (Random.Range(0, 100) <= MerchantItemTable.ChancePercentage[i])
            {
                DraggableItem draggableItem = Instantiate(DraggableItemPrefab);
                draggableItem.item = MerchantItemTable.PossibleItems[i];
                draggableItem.InitializeItemValues();
                draggableItem.SetSprite();
                draggableItem.slotType = DraggableItem.SlotType.Shop;
                draggableItem.quantity = Random.Range(MerchantItemTable.MinRange[i], MerchantItemTable.MaxRange[i]);
                items.Add(draggableItem);

                itemdata.Add(draggableItem.GetData());
                Destroy(draggableItem);
            }
        }
        _isInitialized = true;
    }

    public override void ItemTaken(ItemData itemData)
    {
        for (int i = 0; i < itemdata.Count; i++)
        {
            itemdata.RemoveAt(i);
            return;
        }
    }
}
