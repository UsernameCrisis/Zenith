using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedPotion", menuName = "Scriptable Objects/SpeedPotion")]
public class SpeedPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerMovement>().ExternalSpeedMultiplier += 0.5f;

        FindAnyObjectByType<PlayerOverworldAttributes>().StarSpeedResetTimer(300);

        base.OnConsume();
    }
}
