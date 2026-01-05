using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using BehaviourTrees;
public abstract class EnemyAIControllerBase : MonoBehaviour, ITurnActor
{
    protected GridData gridData;
    protected Vector3Int latestPos;
    protected Vector3Int targetPos;
    protected BehaviourTree tree;
    [SerializeField] protected Grid grid;
    [SerializeField] protected MovementPreview previewSystem;
    public bool IsPlayer => false;
    public bool isMoving;

    void Awake()
    {
        grid = FindAnyObjectByType<Grid>();
        previewSystem = FindAnyObjectByType<MovementPreview>();
    }

    public void BeginTurn(Vector3Int pos, GridData gridData)
    {
        this.gridData = gridData;
        latestPos = pos;

        tree = new BehaviourTree(GetTreeName());
        tree.AddChild(BuildTree());

        StartCoroutine(RunTree());
    }

    protected virtual string GetTreeName() => "Enemy";

    protected IEnumerator RunTree()
    {
        var status = tree.Process();

        while (status == Node.Status.Running)
        {
            yield return null;
            status = tree.Process();
        }

        yield return new WaitForSeconds(0.5f);
        EndTurn();
    }

    public void EndTurn()
    {
        CharacterObject enemyChar = gridData.GetTileAt(latestPos)?.PlacedObject as CharacterObject;
        enemyChar.ResetMovement();
        TurnManager.Instance.EndTurn();
    }

    public Vector3Int FindClosest(Vector3Int enemyPos, List<(Vector3Int pos, CharacterObject)> character)
    {
        Vector3Int best = enemyPos;
        int bestDist = int.MaxValue;

        foreach (var p in character)
        {
            int dist = Manhattan(p.pos, enemyPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p.pos;
            }
        }
        return best;
    }

    public bool IsInRange(Vector3Int a, Vector3Int b, int range)
    {
        return Manhattan(a, b) <= range;
    }

    public Vector3Int FindBestRangedTile(Vector3Int current, Vector3Int playerPos, HashSet<Vector3Int> reachable,
                                int minRange, int maxRange)
    {
        Vector3Int best = current;
        float bestScore = float.MinValue;
        bool foundRangedTile = false;

        foreach (var tile in reachable)
        {
            if (!IsValidRangedTile(tile, playerPos, minRange, maxRange))
                continue;

            float score = ScoreTile(tile, playerPos);

            if (score > bestScore)
            {
                bestScore = score;
                best = tile;
                foundRangedTile = true;
            }
        }

        if (foundRangedTile) return best;

        // Jika terlalu jauh akan melakukan ini
        bestScore = float.MinValue;

        foreach (var tile in reachable)
        {
            int dist = Manhattan(tile, playerPos);

            float score = -dist;

            if (score > bestScore)
            {
                bestScore = score;
                best = tile;
            }
        }

        return best;
    }

    private float ScoreTile(Vector3Int tile, Vector3Int playerPos)
    {
        int dist = Manhattan(tile, playerPos);
    
        float distanceScore = dist * 10f;
    
        // Optional
        float moveCost = Manhattan(latestPos, tile);
        return distanceScore;
    }

    private bool IsValidRangedTile(Vector3Int tile, Vector3Int playerPos, int minRange, int maxRange)
    {
        int dist = Manhattan(tile, playerPos);
        if (dist < minRange || dist > maxRange)
            return false;

        return HasLineOfSight(tile, playerPos);
    }

    public Vector3Int FindMoveToward(Vector3Int enemyPos, Vector3Int targetPos, HashSet<Vector3Int> reachable)
    {
        Vector3Int best = enemyPos;
        int bestDist = int.MaxValue;

        foreach (var tile in reachable)
        {
            int dist = Manhattan(tile, targetPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = tile;
            }
        }
        return best;
    }

    public IEnumerator EnemyWalkPath(List<Vector3Int> path, Vector3Int currPos, CharacterObject enemyChar)
    {
        if (path == null || path.Count == 0)
            yield break;

        Transform enemyTransform =
            gridData.GetTileAt(currPos).PlacedGameObject.transform;

        isMoving = true;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 start = enemyTransform.position;
            Vector3 end = grid.CellToWorld(path[i]);

            float t = 0f;
            float speed = 2f;

            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                enemyTransform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }

        Vector3Int finalPos = path[^1];
        latestPos = finalPos;

        gridData.MoveObject(currPos, finalPos);

        enemyChar.UseMovement(path.Count);
        isMoving = false;
    }

    public bool HasLineOfSight(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> line = GetLine(start, end);

        for (int i = 1; i < line.Count - 1; i++)
        {
            TileData tile = gridData.GetTileAt(line[i]);
            if (tile == null)
                continue;

            if (tile.PlacedObject != null && tile.PlacedObject is not CharacterObject)
                return false;
        }

        return true;
    }
    
    public List<Vector3Int> GetLine(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> line = new();

        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector3Int(x0, y0, 0));
            if (x0 == x1 && y0 == y1)
                break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        return line;
    }

    int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
   
    public GridData getGridData() {return gridData;}
    public void setTargetPos(Vector3Int pos) {targetPos = pos;}
    public Vector3Int getTargetPos() {return targetPos;}
    public MovementPreview getPreview() {return previewSystem;}
    public Vector3Int getLatestPos() {return latestPos;}
    public void setLatestPos(Vector3Int pos) {latestPos = pos;}

    protected abstract Node BuildTree();
}
