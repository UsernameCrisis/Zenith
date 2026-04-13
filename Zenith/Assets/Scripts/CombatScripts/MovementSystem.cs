using System.Collections.Generic;
using UnityEngine;

public class MovementSystem
{
    private readonly GridData gridData;
    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        new(1, 0, 0),
        new(-1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0)
    };

    public HashSet<Vector3Int> ReachableTiles { get; private set; } = new();
    public HashSet<Vector3Int> AttackableTiles { get; private set; } = new();
    public Vector3Int StartTilePos { get; private set; }

    public MovementSystem(GridData gridData)
    {
        this.gridData = gridData;
    }

    public HashSet<Vector3Int> ComputeReachableTiles(Vector3Int startPos, int moveRange)
    {
        StartTilePos = startPos;

        Queue<Vector3Int> frontier = new();
        Dictionary<Vector3Int, int> distance = new();
        HashSet<Vector3Int> visited = new();

        frontier.Enqueue(startPos);
        visited.Add(startPos);
        distance[startPos] = 0;

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();
            int currentDist = distance[current];

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;

                if (visited.Contains(next)) continue;
                if (currentDist + 1 > moveRange) continue;
                if (!gridData.IsWithinBounds(next)) continue;

                TileData tile = gridData.GetTileAt(next);
                PlacedObject placed = tile?.PlacedObject;

                if (placed == null)
                {
                    Enqueue(next, currentDist + 1);
                    continue;
                }

                if (placed.ObjectType == ObjectType.Static || placed.ObjectType == ObjectType.RandomProp)
                    continue;

                if (placed is CharacterObject)
                    continue;

                void Enqueue(Vector3Int pos, int dist)
                {
                    frontier.Enqueue(pos);
                    visited.Add(pos);
                    distance[pos] = dist;
                }
            }
        }

        visited.Remove(startPos);
        ReachableTiles = visited;
        return visited;
    }

    public bool IsTileReachable(Vector3Int tilePos) => ReachableTiles.Contains(tilePos);

    public HashSet<Vector3Int> ComputeAttackableTiles(Vector3Int startPos, int attackRange = 1)
    {
        StartTilePos = startPos;
        HashSet<Vector3Int> result = new();

        TileData startTile = gridData.GetTileAt(startPos);
        if (startTile?.PlacedObject is not CharacterObject attacker)
        {
            AttackableTiles = result;
            return result;
        }

        int startTeam = attacker.Team;

        for (int x = -attackRange; x <= attackRange; x++)
        {
            for (int y = -attackRange; y <= attackRange; y++)
            {
                if (x == 0 && y == 0) continue;
                if (Mathf.Abs(x) + Mathf.Abs(y) > attackRange) continue;

                Vector3Int tilePos = startPos + new Vector3Int(x, y, 0);
                if (!gridData.IsWithinBounds(tilePos)) continue;

                TileData tile = gridData.GetTileAt(tilePos);
                if (tile?.PlacedObject is CharacterObject target && target.Team != startTeam)
                {
                    if (HasLineOfSight(startPos, tilePos))
                        result.Add(tilePos);
                }
            }
        }

        AttackableTiles = result;
        return result;
    }

    public bool IsTileAttackable(Vector3Int tilePos) => AttackableTiles.Contains(tilePos);

    public List<Vector3Int> FindPathAStar(Vector3Int start, Vector3Int goal)
    {
        PriorityQueue<Vector3Int> openSet = new();
        openSet.Enqueue(start, 0);

        Dictionary<Vector3Int, Vector3Int> cameFrom = new();
        Dictionary<Vector3Int, int> costSoFar = new();
        cameFrom[start] = start;
        costSoFar[start] = 0;

        while (openSet.Count > 0)
        {
            Vector3Int current = openSet.Dequeue();
            if (current == goal) break;

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;
                if (!ReachableTiles.Contains(next)) continue;

                int newCost = costSoFar[current] + 1;
                if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                {
                    costSoFar[next] = newCost;
                    openSet.Enqueue(next, newCost + Heuristic(next, goal));
                    cameFrom[next] = current;
                }
            }
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    public bool HasLineOfSight(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> line = GetLine(start, end);

        for (int i = 1; i < line.Count - 1; i++)
        {
            TileData tile = gridData.GetTileAt(line[i]);
            if (tile == null) continue;
            if (tile.PlacedObject != null && tile.PlacedObject is not CharacterObject)
                return false;
        }

        return true;
    }

    // HELPERS

    private List<Vector3Int> ReconstructPath(
        Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int start, Vector3Int goal)
    {
        List<Vector3Int> path = new();
        if (!cameFrom.ContainsKey(goal)) return path;

        Vector3Int current = goal;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }

    private List<Vector3Int> GetLine(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> line = new();

        int x0 = start.x, y0 = start.y;
        int x1 = end.x,   y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector3Int(x0, y0, 0));
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx)  { err += dx; y0 += sy; }
        }

        return line;
    }

    private int Heuristic(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
}
