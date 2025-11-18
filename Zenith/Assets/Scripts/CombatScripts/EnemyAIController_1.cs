using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyAIController_1 : MonoBehaviour, ITurnActor
{
    private GridData gridData;
    [SerializeField] private MovementPreview previewSystem;

    public bool IsPlayer => false;


    public void ExecuteEnemyTurn(Vector3Int enemyPos)
    {
        var players = gridData.GetAllPlayers();

        CharacterObject enemyChar = gridData.GetTileAt(enemyPos)?.PlacedObject as CharacterObject;
        if (enemyChar == null) return;

        Vector3Int targetPos = FindClosestPlayer(enemyPos, players);

        int attackRange = enemyChar.AtkRange;
        if (IsInRange(enemyPos, targetPos, attackRange))
        {
            print("dalam attack pertama");
            gridData.AttackObject(enemyPos, targetPos);
            EndTurn();
            return;
        }

        previewSystem.ShowMovementRange(enemyPos, enemyChar.RemainingMoveRange);
        HashSet<Vector3Int> reachable = previewSystem.GetReachableTiles();

        Vector3Int bestMove = FindMoveToward(enemyPos, targetPos, reachable);
        if (bestMove != enemyPos)
        {
            gridData.MoveObject(enemyPos, bestMove);
        }
        print("setelah show move range ai");
        // Check again if can attack
        if (IsInRange(bestMove, targetPos, attackRange))
        {
            gridData.AttackObject(bestMove, targetPos);
        }
        previewSystem.ClearAll();
        EndTurn();
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
        ExecuteEnemyTurn(pos);
    }

    public void EndTurn()
    {
        TurnManager.Instance.EndTurn();
    }
}
