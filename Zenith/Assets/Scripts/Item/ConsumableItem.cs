using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Inventory/Items/Consumable")]
public class ConsumableItem : BaseItem
{
    public enum ConsumableEffect { Heal, Gold }

    [Header("Consumable Logic")]
    public ConsumableEffect effect;
    public int amount;

    public override void Use()
    {
        PlayerOverworldAttributes player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerOverworldAttributes>();

        if (player != null)
        {
            switch (effect)
            {
                case ConsumableEffect.Heal:
                    player.Heal(amount);
                    break;
                case ConsumableEffect.Gold:
                    player.AddGold(amount);
                    break;
            }
        }
    }
}