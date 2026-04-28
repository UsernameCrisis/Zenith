using UnityEngine;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class TrainingMapGenerator
{
    private readonly int width;
    private readonly int height;
    private readonly int minX;
    private readonly int maxX;
    private readonly int minY;
    private readonly int maxY;
    private readonly int teamDist;
    private readonly int obstacleID;
    private readonly float obstacleDensity;
    private readonly int clusterLimit;
    private readonly int minTraversablePaths;
    private readonly bool useMaxFlow;
    private readonly bool isFullyRandom;

    // Delegates provided by PopulateMap so this class never touches
    // GridData or GameObjects directly.
    private readonly Func<Vector3Int, bool> isOccupied;
    private readonly Func<Vector3Int, bool> isWalkable;
    private readonly Action<Vector3Int, int> placeObject;
    private readonly Action<Vector3Int> removeLastObject;
    private Vector3Int team1Center;
    private Vector3Int team2Center;

    public TrainingMapGenerator(
        int width, int height,
        int minX, int maxX, int minY, int maxY,
        int teamDist, int obstacleID,
        float obstacleDensity, int clusterLimit,
        int minTraversablePaths,
        bool useMaxFlow, bool isFullyRandom,
        Func<Vector3Int, bool> isOccupied,
        Func<Vector3Int, bool> isWalkable,
        Action<Vector3Int, int> placeObject,
        Action<Vector3Int> removeLastObject)
    {
        this.width = width;
        this.height = height;
        this.minX = minX;
        this.maxX = maxX;
        this.minY = minY;
        this.maxY = maxY;
        this.teamDist = teamDist;
        this.obstacleID = obstacleID;
        this.obstacleDensity = obstacleDensity;
        this.clusterLimit = clusterLimit;
        this.minTraversablePaths = minTraversablePaths;
        this.useMaxFlow = useMaxFlow;
        this.isFullyRandom = isFullyRandom;
        this.isOccupied = isOccupied;
        this.isWalkable = isWalkable;
        this.placeObject = placeObject;
        this.removeLastObject = removeLastObject;
    }

    public void Generate()
    {
        SpawnTeams();
        SpawnObstacles();
    }

    private void SpawnTeams()
    {
        team1Center = GetRandomEmptyTile();
        if (team1Center.x == int.MinValue)
        {
            Debug.LogError("SpawnTeams: could not find empty tile for team 1!");
            return;
        }
    
        int maxAttempts = 200;
        int attempts = 0;
        do
        {
            team2Center = GetRandomEmptyTile();
            attempts++;

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("SpawnTeams: could not satisfy team distance, using closest available.");
                break;
            }
        }
        while (team2Center.x != int.MinValue && Vector3Int.Distance(team1Center, team2Center) < teamDist);
        if (team2Center.x == int.MinValue)
        {
            Debug.LogError("SpawnTeams: could not find empty tile for team 2!");
            return;
        }

        SpawnTeam(team1Center, 4, 6); // team 1 IDs
        SpawnTeam(team2Center, 1, 3); // team 2 IDs
    }

    private void SpawnTeam(Vector3Int center, int minID, int maxID)
    {
        int units = 3;
        int attempts = 0;
        int maxAttempts = 200;

        while (units > 0 && attempts < maxAttempts)
        {
            attempts++;

            int dx = Random.Range(-1, 2);
            int dy = Random.Range(-1, 2);

            Vector3Int pos = new Vector3Int(center.x + dx, center.y + dy, 0);

            if (!IsInsideBounds(pos)) continue;

            if (isOccupied(pos)) continue;

            int id = Random.Range(minID, maxID + 1);

            placeObject(pos, id);

            units--;
        }
        if (units > 0)
            Debug.LogWarning($"SpawnTeam: only spawned {3 - units}/3 units after {maxAttempts} attempts.");
    }

    // --- Obstacle spawning ---

    private void SpawnObstacles()
    {
        var checker = new MapConnectivityChecker(
            isWalkable,
            width, height,
            minX, minY,
            minTraversablePaths);

        int totalTiles = width * height;
        int targetObstacles = Mathf.RoundToInt(totalTiles * obstacleDensity);
        int placed = 0;
        int attempts = totalTiles * 3;

        for (int i = 0; i < attempts && placed < targetObstacles; i++)
        {
            Vector3Int pos = GetRandomEmptyTile();

            if (!IsInsideBounds(pos)) continue;
            if (isOccupied(pos)) continue;

            if (Vector3Int.Distance(pos, team1Center) <= 2 ||
                Vector3Int.Distance(pos, team2Center) <= 2)
                continue;

            if (isFullyRandom && CountObstacleNeighbors(pos) > clusterLimit)
                continue;

            placeObject(pos, obstacleID);

            int pathCount = useMaxFlow
                ? checker.MaxFlow(team1Center, team2Center)
                : checker.CountPaths(team1Center, team2Center);

            if (pathCount < minTraversablePaths)
            {
                removeLastObject(pos);
                continue;
            }
            placed++;
        }
    }

    // --- Helpers ---

    private int CountObstacleNeighbors(Vector3Int pos)
    {
        int count = 0;
        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var d in dirs)
        {
            var neighbor = pos + d;
            if (!IsInsideBounds(neighbor)) continue;
            if (!isWalkable(neighbor)) count++;
        }
        return count;
    }

    private Vector3Int GetRandomEmptyTile()
    {
        for (int i = 0; i < 100; i++)
        {
            int x = Random.Range(minX, maxX);
            int y = Random.Range(minY, maxY);
            Vector3Int pos = new Vector3Int(x, y, 0);

            if (IsInsideBounds(pos) && !isOccupied(pos))
                return pos;
        }

        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (!isOccupied(pos)) return pos;
            }

        Debug.LogError("TrainingMapGenerator: no empty tile found!");
        return new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    private bool IsInsideBounds(Vector3Int pos) =>
        pos.x >= minX && pos.x <= maxX &&
        pos.y >= minY && pos.y <= maxY;
}
