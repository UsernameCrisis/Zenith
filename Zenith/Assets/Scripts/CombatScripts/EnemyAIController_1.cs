using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using BehaviourTrees;

public class EnemyAIController_1 : MonoBehaviour, ITurnActor
{
    private GridData gridData;
    private Vector3Int latestPos;
    private Vector3Int targetPos;
    private BehaviourTree tree;
    [SerializeField] private Grid grid;
    [SerializeField] private MovementPreview previewSystem;

    public bool IsPlayer => false;
    public bool isMoving;


    public IEnumerator ExecuteEnemyTurn(Vector3Int enemyPos)
    {
        var players = gridData.GetAllPlayers();

        CharacterObject enemyChar = gridData.GetTileAt(enemyPos)?.PlacedObject as CharacterObject;
        if (enemyChar == null)
        {
            yield return new WaitForSeconds(0.1f);
            EndTurn();
            yield break;
        }


        Vector3Int targetPos = FindClosestPlayer(enemyPos, players);

        int attackRange = enemyChar.AtkRange;
        if (IsInRange(enemyPos, targetPos, attackRange))
        {
            print("dalam attack pertama");
            gridData.AttackObject(enemyPos, targetPos);
            yield return new WaitForSeconds(1f);
            EndTurn();
            yield break;
        }

        previewSystem.ShowMovementRange(enemyPos, enemyChar.RemainingMoveRange);
        HashSet<Vector3Int> reachable = previewSystem.GetReachableTiles();

        Vector3Int bestMove = FindMoveToward(enemyPos, targetPos, reachable);
        List<Vector3Int> path = previewSystem.FindPathAStar(enemyPos, bestMove);
        previewSystem.ClearAll();
        if (bestMove != enemyPos)
        {
            yield return StartCoroutine(EnemyWalkPath(path, enemyPos, enemyChar));
        }
        print("setelah show move range ai");
        // Check again if can attack
        if (IsInRange(bestMove, targetPos, attackRange))
        {
            gridData.AttackObject(bestMove, targetPos);
        }
        
        yield return new WaitForSeconds(1f);
        EndTurn();
    }

    public IEnumerator EnemyWalkPath(List<Vector3Int> path, Vector3Int currPos, CharacterObject enemyChar)
    {
        Transform enemyTransform = gridData.GetTileAt(currPos).PlacedGameObject.transform;
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
    
        Vector3Int finalPos = path[path.Count - 1];
        latestPos = finalPos;
        gridData.MoveObject(currPos, finalPos);
    
        int distanceMoved = path.Count;
        enemyChar.UseMovement(distanceMoved);
        isMoving = false;
    }

    public Vector3Int FindClosestPlayer(Vector3Int enemyPos, List<(Vector3Int pos, CharacterObject)> players)
    {
        Vector3Int best = enemyPos;
        int bestDist = 999;

        foreach (var p in players)
        {
            int dist = Mathf.Abs(p.pos.x - enemyPos.x) + Mathf.Abs(p.pos.y - enemyPos.y);
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
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) <= range;
    }

    public Vector3Int FindMoveToward(Vector3Int enemyPos, Vector3Int targetPos, HashSet<Vector3Int> reachable)
    {
        Vector3Int best = enemyPos;
        int bestDist = 999;

        foreach (var tile in reachable)
        {
            int dist = Mathf.Abs(tile.x - targetPos.x) + Mathf.Abs(tile.y - targetPos.y);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = tile;
            }
        }

        return best;
    }

    private IEnumerator RunTree()
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
    
    public void BeginTurn(Vector3Int pos, GridData gridData)
    {
        this.gridData = gridData;
        latestPos = pos;

        tree = new BehaviourTree("Enemy");

        Sequence root = new Sequence("Root");
        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindClosest", new FindClosestPlayer(this)));
        root.AddChild(preAction);
        
        Selector attack1 = new Selector("Attack1");
        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange1", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("DoAttack1", new Attack(this)));
        attack1.AddChild(tryAttack);

        attack1.AddChild(new Leaf("MoveTowardPlayer", new MoveTowardPlayer(this)));

        root.AddChild(attack1);

        Sequence attack2 = new Sequence("Attack2");
        attack2.AddChild(new Leaf("IsInRange2", new IsInRange(this)));
        attack2.AddChild(new Leaf("DoAttack2", new Attack(this)));
        root.AddChild(attack2);

        tree.AddChild(root);
        StartCoroutine(RunTree());

        // StartCoroutine(ExecuteEnemyTurn(pos));
    }

    public void EndTurn()
    {
        CharacterObject enemyChar = gridData.GetTileAt(latestPos)?.PlacedObject as CharacterObject;
        enemyChar.ResetMovement();
        TurnManager.Instance.EndTurn();
    }

    public GridData getGridData() {return gridData;}
    public void setTargetPos(Vector3Int pos) {targetPos = pos;}
    public Vector3Int getTargetPos() {return targetPos;}
    public MovementPreview getPreview() {return previewSystem;}
    public Vector3Int getLatestPos() {return latestPos;}
    public void setLatestPos(Vector3Int pos) {latestPos = pos;}
}

