using UnityEngine;

public class RandomInteractableItem : InteractableObject
{
    public LootTable lootTable;
    private BaseItem rolledItem;

    void Start()
    {
        int gold = GameManager.Instance != null ? GameManager.Instance.gold_spent : 0;
        rolledItem = lootTable.PickItem(gold);

        if (rolledItem == null)
        {
            Debug.LogWarning("LootTable rolled null! Destroying empty drop.");
            Destroy(gameObject);
        }
    }

    public override void OnInteract()
    {
        if (rolledItem == null) return;

        bool success = InventoryManager.Instance.AddItem(rolledItem, 1);

        if (success)
        {
            Debug.Log($"Picked up Randomized Item: {rolledItem.itemName}");
            base.OnInteract();
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("<color=orange>Too heavy to pick up the mystery item!</color>");
        }
    }
}