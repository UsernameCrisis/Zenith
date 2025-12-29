using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedPotion", menuName = "Scriptable Objects/SpeedPotion")]
public class SpeedPotionC : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<Player>().BuffSpeed(5, 300);

        base.OnConsume();
    }
}
