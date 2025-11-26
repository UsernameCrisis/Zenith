using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyAIController_1 : MonoBehaviour, ITurnActor
{
    private GridData gridData;
    private Vector3Int latestPos;
    [SerializeField] private Grid grid;
    [SerializeField] private MovementPreview previewSystem;

    public bool IsPlayer => false;


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

    private IEnumerator EnemyWalkPath(List<Vector3Int> path, Vector3Int currPos, CharacterObject enemyChar)
    {
        Transform enemyTransform = gridData.GetTileAt(currPos).PlacedGameObject.transform;
    
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
    }

    private Vector3Int FindClosestPlayer(Vector3Int enemyPos, List<(Vector3Int pos, CharacterObject)> players)
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

    private bool IsInRange(Vector3Int a, Vector3Int b, int range)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) <= range;
    }

    private Vector3Int FindMoveToward(Vector3Int enemyPos, Vector3Int targetPos, HashSet<Vector3Int> reachable)
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

    public void BeginTurn(Vector3Int pos, GridData gridData)
    {
        this.gridData = gridData;
        StartCoroutine(ExecuteEnemyTurn(pos));
    }

    public void EndTurn()
    {
        CharacterObject enemyChar = gridData.GetTileAt(latestPos)?.PlacedObject as CharacterObject;
        enemyChar.ResetMovement();
        TurnManager.Instance.EndTurn();
    }
}
