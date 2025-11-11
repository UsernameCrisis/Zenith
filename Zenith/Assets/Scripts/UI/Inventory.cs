using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : MonoBehaviour
{
    public bool expandable = false;
    private bool active = true;
    public void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
    public void ToggleInventory()
    {
        active = !active;
        gameObject.SetActive(active);
    }
    public void AddItem(Item item)
    {
        GetComponentInChildren<InventoryRight>().AddItem(item);
    }

    void Update()
    {
        if (InputSystem.actions.FindAction("ExitSelect").WasPressedThisFrame())
        {
            if (gameObject.active) gameObject.SetActive(false);
            // if (transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.active) transform.parent.GetComponentInParent<SceneRoot>().ChestUI.gameObject.SetActive(false);
        }
    }
}
