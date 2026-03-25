using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    public BaseItem potion;
    public BaseItem sword;
    public BaseItem junk;

    void Update()
    {
        // Press 1, 2, or 3 to add items while the game is running
        if (Input.GetKeyDown(KeyCode.Alpha1)) InventoryManager.Instance.AddItem(potion, 1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) InventoryManager.Instance.AddItem(sword, 1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) InventoryManager.Instance.AddItem(junk, 1);
    }
}