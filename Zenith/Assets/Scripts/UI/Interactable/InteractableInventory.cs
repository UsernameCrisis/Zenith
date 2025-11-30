// using Unity.VisualScripting;
// using UnityEngine;

// public class InteractableInventory : InteractableObject, Interactable
// {
//     public Inventory inventory;
//     public ChestInventory chestInventory;
//     public GameObject ChestUI;
//     public ChestItems items;
//     public GameObject Object
//     {
//         get { return gameObject; }
//     }
//     void Awake()
//     {
//         items.Awake();
//     }

//     public override void OnInteract()
//     {
//         inventory.SetActive(true);
//         inventory.GetComponentInChildren<InventoryLeft>().SetInteractableInventoryActive();
//         Debug.Log(items.GetItems().Count);
//         chestInventory.UnloadItems(items.GetItems());
//         base.OnInteract();
//     }
// }
