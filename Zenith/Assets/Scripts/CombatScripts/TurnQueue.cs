using UnityEngine;
using System.Collections.Generic;

public class TurnQueue
{
    public List<CharacterObject> units = new();
    private int index = 0;

    public TurnQueue(List<CharacterObject> units)
    {
        this.units = units;
    }

    public CharacterObject GetCurrent()
    {
        return units[index];
    }

    public void Next()
    {
        index = (index + 1) % units.Count;
    }

    public void Remove(CharacterObject character)
    {
        int removeIndex = units.IndexOf(character);
        if (removeIndex == -1) return;

        // Adjust index if needed
        if (removeIndex <= index && index > 0)
            index--;

        units.RemoveAt(removeIndex);

        index = Mathf.Clamp(index, 0, Mathf.Max(0, units.Count - 1));
    }

}
