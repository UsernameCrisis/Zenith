using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using BehaviourTrees;
public abstract class EnemyAIControllerBase : MonoBehaviour, ITurnActor
{
    protected GridData gridData;
    protected Vector3Int targetPos;
    protected BehaviourTree tree;
    protected Grid grid;
    protected MovementPreview previewSystem;
    protected TurnManager turnManager;

    private CombatExecutor combatExecutor;
    private CharacterObject myCharacter;
    
    private bool isTurnComplete = false;
    public bool IsTurnComplete() => isTurnComplete;
    private bool isMoving = false;
    public bool IsMoving => isMoving;
    public bool IsPlayer => false;

    void Awake()
    {
        Transform parent = transform.parent;
        grid = parent.parent.GetComponentInChildren<Grid>();
        previewSystem = GetComponentInParent<MovementPreview>();
        combatExecutor = GetComponentInParent<CombatExecutor>();
        turnManager = GetComponentInParent<TurnManager>();
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

    public void ForceComplete()
    {
        isTurnComplete = true;
    }

    // Public AI utility methods

    public Vector3Int FindClosest(Vector3Int enemyPos, List<(Vector3Int pos, CharacterObject)> character)
    {
        Vector3Int best = enemyPos;
        int bestCost = int.MaxValue;
        MovementSystem ms = previewSystem.GetMovementSystem();

        foreach (var p in character)
        {
            int cost = ms.PathCost(enemyPos, p.pos);
            if (cost < bestCost)
            {
                bestCost = cost;
                best = p.pos;
            }
        }
        return best;
    }

    public bool IsInRange(Vector3Int a, Vector3Int b, int range)
    {
        return Manhattan(a, b) <= range;
    }

    public Vector3Int FindMoveToward(Vector3Int enemyPos, Vector3Int targetPos, HashSet<Vector3Int> reachable)
    {
        Vector3Int best = enemyPos;
        int bestCost = int.MaxValue;
        MovementSystem ms = previewSystem.GetMovementSystem();

        foreach (var tile in reachable)
        {
            int cost = ms.PathCost(tile, targetPos);
            if (cost < bestCost)
            {
                bestCost = cost;
                best = tile;
            }
        }
        return best;
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
        MovementSystem ms = previewSystem.GetMovementSystem();

        foreach (var tile in reachable)
        {
            int cost = ms.PathCost(tile, playerPos);

            float score = cost == int.MaxValue ? float.MinValue : -cost;

            if (score > bestScore)
            {
                bestScore = score;
                best = tile;
            }
        }

        return best;
    }

    public bool HasLineOfSight(Vector3Int start, Vector3Int end)
    {
        return previewSystem.GetMovementSystem().HasLineOfSight(start, end);
    }

    public Vector3Int GetCurrentPosition()
    {
        if (myCharacter != null)
            return myCharacter.Position;

        Debug.LogError("AI character not found!");
        return grid.WorldToCell(transform.position);
    }

    // Private helpers

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

    private int Manhattan(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    public GridData GetGridData() => gridData;
    public void SetTargetPos(Vector3Int pos) => targetPos = pos;
    public Vector3Int GetTargetPos() => targetPos;
    public MovementPreview GetPreview() => previewSystem;
    public CombatExecutor GetCombatExecutor() => combatExecutor;
    public TurnManager GetTurnManager() => turnManager;
    protected abstract Node BuildTree();
    void OnDestroy()
    {
        StopAllCoroutines();
    }
}
