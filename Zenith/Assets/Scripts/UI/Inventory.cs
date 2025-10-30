using UnityEngine;

public class Inventory : MonoBehaviour
{
    private bool active = true;
    public void ToggleInventory() { active = !active; gameObject.SetActive(active); }
}
