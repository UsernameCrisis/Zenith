using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedPotion", menuName = "Scriptable Objects/SpeedPotion")]
public class SpeedPotion : ItemBaseScript
{
    public override void OnConsume()
    {
        FindAnyObjectByType<PlayerMovement>().BuffSpeed(0.5f, 300);

        base.OnConsume();
    }
}
