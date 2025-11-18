using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChestItems", menuName = "Scriptable Objects/ChestItems")]
public class ChestItems : ScriptableObject
{
    public List<List<Item>> items;
}
