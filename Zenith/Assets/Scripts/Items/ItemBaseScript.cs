using UnityEngine;

public abstract class ItemBaseScript: ScriptableObject
{
    public virtual void OnConsume()
    {
        FindAnyObjectByType<OverworldUI>().itemActionMenu.gameObject.SetActive(false);
    }
}
