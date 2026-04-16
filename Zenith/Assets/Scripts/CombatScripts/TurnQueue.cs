using UnityEngine;
using System.Collections.Generic;

public class TurnQueue
{
    public List<CharacterObject> allUnits;
    public List<CharacterObject> queue = new();
    private int visibleCount = 5;
    private int bufferSize = 10;

    public TurnQueue(List<CharacterObject> units, int bufferSize = 10, int visibleCount = 5)
    {
        this.allUnits = units;
        this.bufferSize = bufferSize;
        this.visibleCount = visibleCount;
        RefillFull();
    }

    private void RefillFull()
    {
        queue.Clear();
        queue.AddRange(GenerateTurnOrder(allUnits, bufferSize));
    }

    public List<CharacterObject> GenerateTurnOrder(List<CharacterObject> units, int bufferSize)
    {
        List<CharacterObject> order = new List<CharacterObject>();
        Dictionary<CharacterObject, float> meters = new Dictionary<CharacterObject, float>();

        foreach (var u in units)
            meters[u] = u.CurrentATB;

        while (order.Count < bufferSize)
        {
            if (units.Count == 0)
                break;

            CharacterObject next = null;
            float minTime = float.MaxValue;
            foreach (var u in units)
            {
                if (u.Speed <= 0f)
                    continue;

                float timeToAct = Mathf.Max(0f, 100f - meters[u]) / u.Speed;
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

            foreach (var u in units)
                meters[u] += u.Speed * minTime;
                
            meters[next] -= 100f;
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
            RefillFull();
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

        var more = GenerateTurnOrder(allUnits, needed);
        queue.AddRange(more);
    }

    public List<CharacterObject> GetVisibleTurns()
    {
        return queue.GetRange(0, Mathf.Min(visibleCount, queue.Count));
    }

    public void Remove(CharacterObject character)
    {
        allUnits.Remove(character);
        queue.RemoveAll(c => c == character);
        Debug.Log($"After remove → allUnits: {allUnits.Count}, queue: {queue.Count}");
        if (queue.Count <= visibleCount)
            RefillFull();
    }

}
