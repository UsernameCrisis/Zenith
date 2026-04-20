using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using BehaviourTrees;
public abstract class EnemyAIControllerBase : MonoBehaviour, ITurnActor
{
    protected GridData gridData;
    protected Vector3Int targetPos;
    protected BehaviourTree tree;
    private CombatExecutor combatExecutor;
    private CharacterObject myCharacter;
    protected Grid grid;
    protected MovementPreview previewSystem;
    public bool IsPlayer => false;
    protected bool isTurnComplete = false;
    public bool IsTurnComplete() => isTurnComplete;
    public bool isMoving;
    protected TurnManager turnManager;
    public TurnManager getTurnManager() => turnManager;

    void Awake()
    {
        Transform parent = transform.parent;
        grid = parent.parent.GetComponentInChildren<Grid>();
        previewSystem = GetComponentInParent<MovementPreview>();
        combatExecutor = GetComponentInParent<CombatExecutor>();
        turnManager = GetComponentInParent<TurnManager>();
    }
    void Start()
    {
        
    }

    public void BeginTurn(GridData gridData, CharacterObject character)
    {
        this.gridData = gridData;
        myCharacter = character;
        isTurnComplete = false;

        tree = new BehaviourTree(GetTreeName());
        Node root = BuildTree();
        if (root == null)
        {
            Debug.LogError("BuildTree returned null!");
            isTurnComplete = true;
            return;
        }
        tree.AddChild(root);
        
        StartCoroutine(RunTree());
    }

    protected virtual string GetTreeName() => "Enemy";

    protected IEnumerator RunTree()
    {
        if (tree == null)
        {
            Debug.LogError("Tree is null!");
            yield break;
        }
        Node.Status status;
        try
        {
            status = tree.Process();
        } catch (System.Exception e)
        {
            Debug.LogError($"Tree process crash: {e}");
            yield break;
        }
        

        while (status == Node.Status.Running)
        {
            if (turnManager == null || turnManager.State != CombatState.Playing)
                yield break;
            yield return null;
            try
            {
                status = tree.Process();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Tree process crash (loop): {e}");
                yield break;
            }
        }
        if (turnManager == null || turnManager.State != CombatState.Playing)
            yield break;

        yield return new WaitForSeconds(0.2f);
        EndTurn();
    }

    public void EndTurn()
    {
        if (gridData == null)
        {
            Debug.LogError("gridData is null in EndTurn!");
            isTurnComplete = true;
            return;
        }
        CharacterObject enemyChar = gridData.GetTileAt(GetCurrentPosition())?.PlacedObject as CharacterObject;
        if (enemyChar != null)
        {
            enemyChar.ResetMovement();
            enemyChar.EnableAttack();
        }
        isTurnComplete = true;
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
        float moveCost = Manhattan(GetCurrentPosition(), tile);
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
    
    public Vector3Int GetCurrentPosition()
    {
        if (myCharacter != null)
            return myCharacter.Position;

        Debug.LogError("AI character not found!");
        return grid.WorldToCell(transform.position);
    }
    public void ForceComplete()
    {
        isTurnComplete = true;
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
    public CombatExecutor GetCombatExecutor() => combatExecutor;
    protected abstract Node BuildTree();
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
