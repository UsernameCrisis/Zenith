using UnityEngine;

public class Inventory : MonoBehaviour
{
    private bool active = true;

    public void ToggleInventory() { active = !active; this.gameObject.SetActive(active); }
}
