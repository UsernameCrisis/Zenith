using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static InteractableDoor;

public class DoorManager : MonoBehaviour
{
    private readonly List<InteractableDoor> entryDoors = new();
    private readonly List<InteractableDoor> exitDoors = new();

    [Header("Randomization in percentage")]
    public float spawnDoorConsecutiveDestroy = 33f; // decrease in percent for each subsequent spawn
    public float exitDoorDestroy = 7.5f; // percent chance to destroy any exit

    private List<InteractableDoor> availableEntries;

    private void Awake()
    {
        var allDoors = FindObjectsByType<InteractableDoor>(FindObjectsSortMode.None);

        foreach (var door in allDoors)
        {
            if (door.doorType == DoorType.Entry)
                entryDoors.Add(door);
            else
                exitDoors.Add(door);
        }
        availableEntries = new List<InteractableDoor>(entryDoors);
        Shuffle(availableEntries);

        RandomizeConnections();
    }


    private void RandomizeConnections()
    {
        var spawnExits = exitDoors.Where(d => d.doorType == DoorType.Spawn).ToList();
        var normalExits = exitDoors.Where(d => d.doorType == DoorType.Exit).ToList();

        Shuffle(spawnExits);
        Shuffle(normalExits);

        float spawnChance = 100f;
        foreach (var spawnExit in spawnExits.ToList())
        {
            var validEntries = entryDoors.Where(e => e.roomID != spawnExit.roomID).ToList();

            if (validEntries.Count == 0)
            {
                DestroyExitDoor(spawnExit);
                continue;
            }

            if (Random.Range(0f, 100f) > spawnChance)
            {
                DestroyExitDoor(spawnExit);
            }
            else
            {
                var chosen = validEntries[Random.Range(0, validEntries.Count)];
                LinkDoors(spawnExit, chosen);
                availableEntries.Remove(chosen);
            }

            spawnChance -= spawnDoorConsecutiveDestroy;
            if (spawnChance < 0) spawnChance = 0;
        }

        foreach (var exitDoor in normalExits.ToList())
        {
            if (Random.Range(0f, 100f) < exitDoorDestroy)
            {
                DestroyExitDoor(exitDoor);
                continue;
            }

            var validEntries = entryDoors.Where(e => e.roomID != exitDoor.roomID).ToList();

            if (validEntries.Count == 0)
            {
                DestroyExitDoor(exitDoor);
                continue;
            }

            var chosen = validEntries[Random.Range(0, validEntries.Count)];
            LinkDoors(exitDoor, chosen);
            availableEntries.Remove(chosen);
        }
    }

    private void DestroyExitDoor(InteractableDoor door)
    {
        exitDoors.Remove(door);
        Destroy(door.gameObject);
    }

    private void LinkDoors(InteractableDoor exitDoor, InteractableDoor entryDoor)
    {
        exitDoor.linkedDoor = entryDoor;
        entryDoor.linkedDoor = exitDoor;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
