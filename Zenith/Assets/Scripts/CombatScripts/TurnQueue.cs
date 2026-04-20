using UnityEngine;
using System.Collections.Generic;

public class TurnQueue
{
    public List<CharacterObject> allUnits;
    public List<CharacterObject> queue = new();
    private Dictionary<CharacterObject, float> simulationMeters = new();
    private int visibleCount = 5;
    private int bufferSize = 10;

    public TurnQueue(List<CharacterObject> units, int bufferSize = 10, int visibleCount = 5)
    {
        this.allUnits = units;
        this.bufferSize = bufferSize;
        this.visibleCount = visibleCount;
        foreach (var u in units)
            simulationMeters[u] = u.CurrentATB;
        RefillFull();
    }

    private void RefillFull()
    {
        queue.Clear();
        queue.AddRange(GenerateTurnOrder(bufferSize));
    }

    public List<CharacterObject> GenerateTurnOrder(int bufferSize)
    {
        List<CharacterObject> order = new List<CharacterObject>();
        int safetyLimit = bufferSize *Mathf.Max(allUnits.Count, 1)  * 100;
        int iterations = 0;

        while (order.Count < bufferSize)
        {
            if (iterations++ > safetyLimit)
            {
                Debug.LogError("GenerateTurnOrded hit safety limit, infinite loop detected!");
                break;
            }

            if (allUnits.Count == 0)
                break;

            CharacterObject next = null;
            float minTime = float.MaxValue;
            foreach (var u in allUnits)
            {
                if (u.Speed <= 0f)
                    continue;

                float timeToAct = Mathf.Max(0f, 100f - simulationMeters[u]) / u.Speed;
                if (timeToAct < minTime)
                {
                    minTime = timeToAct;
                    next = u;
                } else if (Mathf.Approximately(timeToAct, minTime))
                {
                    if (next == null || u.Speed > next.Speed)
                        next = u;
                }
            }

            if (next == null)
                break;

            foreach (var u in allUnits)
                simulationMeters[u] += u.Speed * minTime;
                
            simulationMeters[next] -= 100f;
            order.Add(next);
        }

        return order;
    }

    public CharacterObject GetCurrent()
    {
        if (queue == null || queue.Count == 0)
            return null;

        return queue[0];
    }


    public CharacterObject PopNext()
    {
        Debug.Log($"PopNext → queue count before: {queue.Count}");
        if (queue.Count == 0)
            RefillFull();
        
        if (queue.Count == 0)
            return null;

        CharacterObject next = queue[0];
        queue.RemoveAt(0);

        // EnsureBuffer();
        if (queue.Count <= visibleCount)
        {
            EnsureBuffer();
        }

        return next;
    }

    private void EnsureBuffer()
    {
        if (queue.Count > visibleCount)
            return;

        int needed = bufferSize - queue.Count;
        if (needed <= 0)
            return;

        var more = GenerateTurnOrder(needed);
        queue.AddRange(more);
    }

    public List<CharacterObject> GetVisibleTurns()
    {
        return queue.GetRange(0, Mathf.Min(visibleCount, queue.Count));
    }

    public void Remove(CharacterObject character)
    {
        allUnits.Remove(character);
        simulationMeters.Remove(character);
        queue.RemoveAll(c => c == character);
        Debug.Log($"After remove → allUnits: {allUnits.Count}, queue: {queue.Count}");
        // if (queue.Count <= visibleCount)
        //     RefillFull();
    }

}
