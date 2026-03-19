using UnityEngine;

[CreateAssetMenu(fileName = "New Junk", menuName = "Inventory/Items/Junk")]
public class JunkItem : BaseItem
{
    public override void Use()
    {
        Debug.Log("This item has no use other than selling.");
    }
}