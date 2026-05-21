using UnityEngine;
using System.Collections.Generic;
using System;

public class MovementPreview : MonoBehaviour
{
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private GameObject pathPrefab;
    [SerializeField] private GameObject enemyHighlightPrefab;
    [SerializeField] private int maxRange = 3; // Default move range if not specified
    private PopulateMap populateMap;
    private Grid grid;
    private MovementSystem movementSystem;

    private readonly List<GameObject> activeHighlights = new();
    private readonly List<GameObject> activePath = new();

    void Start()
    {
        populateMap = GetComponentInChildren<PopulateMap>();
        grid = GetComponentInChildren<Grid>();

        GridData gridData = populateMap.objectsData;
        movementSystem = new MovementSystem(gridData);
    }

    public void ShowMovementRange(Vector3Int startPos, int moveRange = -1)
    {
        if (moveRange < 0)
            moveRange = maxRange;

        HashSet<Vector3Int> reachable = movementSystem.ComputeReachableTiles(startPos, moveRange);

        foreach (var tile in reachable)
            SpawnHighlight(tile, highlightPrefab, activeHighlights);
    }

    public void ShowAttackableTiles(Vector3Int startPos, int attackRange)
    {
        HashSet<Vector3Int> attackable = movementSystem.ComputeAttackableTiles(startPos, attackRange);

        GameObject prefab = enemyHighlightPrefab != null ? enemyHighlightPrefab : highlightPrefab;
        foreach (var pos in attackable)
            SpawnHighlight(pos, prefab, activeHighlights);
    }

    public void ShowPathPreview(Vector3Int targetTile)
    {
        ClearPath();

        if (!movementSystem.IsTileReachable(targetTile))
            return;

        List<Vector3Int> path = movementSystem.FindPathAStar(movementSystem.StartTilePos, targetTile);

        if (path.Count == 0)
        {
            Debug.Log("No path found to target.");
            return;
        }

        foreach (var step in path)
            SpawnHighlight(step, pathPrefab, activePath);
    }

    public void ClearAll()
    {
        ClearHighlights();
        ClearPath();
    }
    
    public void ClearHighlights()
    {
        foreach (var h in activeHighlights)
            if (h != null) Destroy(h);
        activeHighlights.Clear();
    }

    private void ClearPath()
    {
        foreach (var p in activePath)
            if (p != null) Destroy(p);
        activePath.Clear();
    }

    private void SpawnHighlight(Vector3Int gridPos, GameObject prefab, List<GameObject> list)
    {
        Vector3 worldPos = grid.CellToWorld(gridPos);
        GameObject obj = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        list.Add(obj);
    }

    public bool IsTileReachable(Vector3Int tilePos) => movementSystem.IsTileReachable(tilePos);
    public bool IsTileAttackable(Vector3Int tilePos) => movementSystem.IsTileAttackable(tilePos);
    public HashSet<Vector3Int> GetReachableTiles() => movementSystem.ReachableTiles;
    public HashSet<Vector3Int> ComputeReachableTiles(Vector3Int startPos, int moveRange) =>
        movementSystem.ComputeReachableTiles(startPos, moveRange);
    public HashSet<Vector3Int> ComputeAttackableTiles(Vector3Int startPos, int attackRange) =>
        movementSystem.ComputeAttackableTiles(startPos, attackRange);
    public List<Vector3Int> FindPathAStar(Vector3Int start, Vector3Int goal) =>
        movementSystem.FindPathAStar(start, goal);
    public int PathCost(Vector3Int start, Vector3Int goal) => movementSystem.PathCost(start, goal);
    public int PathCostWithBlocked(Vector3Int start, Vector3Int goal, HashSet<Vector3Int> blocked)
    {
        return movementSystem.PathCostWithBlocked(start, goal, blocked);
    }
    public bool HasLineOfSight(Vector3Int start, Vector3Int end) => movementSystem.HasLineOfSight(start, end);
    public Vector3Int StartTilePos => movementSystem.StartTilePos;
}

public class PriorityQueue<T>
{
    private readonly List<(T item, int priority)> heap = new();

    public int Count => heap.Count;

    public void Enqueue(T item, int priority)
    {
        heap.Add((item, priority));
        SiftUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("PriorityQueue is empty.");

        T best = heap[0].item;

        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);

        if (heap.Count > 0)
            SiftDown(0);

        return best;
    }

    public T Peek()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("PriorityQueue is empty.");
        return heap[0].item;
    }

    public void Clear() => heap.Clear();

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[parent].priority <= heap[index].priority)
                break;

            Swap(index, parent);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        int count = heap.Count;

        while (true)
        {
            int left     = 2 * index + 1;
            int right    = 2 * index + 2;
            int smallest = index;

            if (left  < count && heap[left].priority  < heap[smallest].priority) smallest = left;
            if (right < count && heap[right].priority < heap[smallest].priority) smallest = right;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b) =>
        (heap[a], heap[b]) = (heap[b], heap[a]);
}
