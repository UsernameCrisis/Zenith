using System;
using UnityEngine;
using System.Linq;


namespace BehaviourTrees
{
    public interface IStrategy
    {
        Node.Status Process();
        void Reset()
        {
            //Noop
        }
    }

    public class ActionStrategy : IStrategy
    {
        readonly Action action;

        public ActionStrategy(Action action)
        {
            this.action = action;
        }

        public Node.Status Process()
        {
            action();
            return Node.Status.Success;
        }
    }

    public class Condition : IStrategy
    {
        readonly Func<bool> predicate;
        public Condition(Func<bool> predicate)
        {
            this.predicate = predicate;
        }

        public Node.Status Process() => predicate() ? Node.Status.Success : Node.Status.Failure;
    }

    public class CheckEnemyExists : IStrategy
    {
        EnemyAIControllerBase ai;

        public CheckEnemyExists(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            bool temp = ai.getGridData().GetTileAt(ai.getLatestPos())?.PlacedObject is CharacterObject;
            // Debug.Log(temp);
            return temp ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class FindClosest : IStrategy
    {
        EnemyAIControllerBase ai;
        
        public FindClosest(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            ai.setTargetPos(ai.FindClosest(ai.getLatestPos(), ai.getGridData().GetAllFriendlies()));
            return Node.Status.Success;
        }
    }
    
    public class IsInRange : IStrategy
    {
        EnemyAIControllerBase ai;

        public IsInRange(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var latestPos = ai.getLatestPos();
            var enemyChar = ai.getGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            bool inRange = ai.IsInRange(latestPos, ai.getTargetPos(), enemyChar.AtkRange);
            Debug.Log(inRange);
            return inRange ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class IsTooClose : IStrategy
    {
        EnemyAIControllerBase ai;

        public IsTooClose(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var enemyPos = ai.getLatestPos();
            var targetPos = ai.getTargetPos();

            int dist = Mathf.Abs(enemyPos.x - targetPos.x) + Mathf.Abs(enemyPos.y - targetPos.y);
            int minRange = 2;
            Debug.Log("dist: "+dist);
            Debug.Log(dist < minRange);
            return dist < minRange ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class HasLineOfSight : IStrategy
    {
        EnemyAIControllerBase ai;
        public HasLineOfSight(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            bool hasLineOfSight = ai.HasLineOfSight(ai.getLatestPos(), ai.getTargetPos());
            return hasLineOfSight ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class Attack : IStrategy
    {
        EnemyAIControllerBase ai;

        public Attack(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var latestPos = ai.getLatestPos();
            if (ai.getGridData().GetTileAt(latestPos)?.PlacedObject is not CharacterObject)
                return Node.Status.Failure;

            ai.getGridData().AttackObject(latestPos, ai.getTargetPos());
            return Node.Status.Success;
        } 
    }

    public class MoveToRandomTile : IStrategy
    {
        EnemyAIControllerBase ai;
        bool startedMovement = false;
        Vector3Int targetTile;

        public MoveToRandomTile(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var latestPos = ai.getLatestPos();
            Debug.Log(latestPos);
            if (startedMovement) return ai.isMoving ? Node.Status.Running : Node.Status.Success;

            var enemyChar = ai.getGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            Debug.Log(enemyChar);
            ai.getPreview().ShowMovementRange(latestPos, enemyChar.RemainingMoveRange);
            var reachable = ai.getPreview().GetReachableTiles();

            if (reachable == null || reachable.Count == 0)
            {
                ai.getPreview().ClearAll();
                return Node.Status.Failure;
            }

            reachable.Remove(latestPos);

            if (reachable.Count == 0)
            {
                ai.getPreview().ClearAll();
                return Node.Status.Success;
            }

            Vector3Int bestMove = reachable.ElementAt(UnityEngine.Random.Range(0, reachable.Count));

            ai.setLatestPos(bestMove);

            if (bestMove == latestPos)
            {
                ai.getPreview().ClearAll();
                return Node.Status.Success;
            }

            var path = ai.getPreview().FindPathAStar(latestPos, bestMove);

            ai.getPreview().ClearAll();

            ai.StartCoroutine(ai.EnemyWalkPath(path, latestPos, enemyChar)
            );

            startedMovement = true;
            return Node.Status.Running;
        }
    }

    public class MoveForRanged : IStrategy
    {
        EnemyAIControllerBase ai;
        bool startedMovement = false;

        public MoveForRanged(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var latestPos = ai.getLatestPos();
            if (startedMovement) return ai.isMoving ? Node.Status.Running : Node.Status.Success;
            
            var enemyChar = ai.getGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            var targetPos = ai.getTargetPos();

            int minRange = 2;
            int maxRange = enemyChar.AtkRange;

            if ((Mathf.Abs(latestPos.x - targetPos.x) + Mathf.Abs(latestPos.y - targetPos.y)) >= minRange &&
                (Mathf.Abs(latestPos.x - targetPos.x) + Mathf.Abs(latestPos.y - targetPos.y)) <= maxRange &&
                ai.HasLineOfSight(latestPos, targetPos))
            {
                return Node.Status.Success;
            }

            ai.getPreview().ShowMovementRange(latestPos, enemyChar.RemainingMoveRange);
            var reachable = ai.getPreview().GetReachableTiles();

            Vector3Int bestMove = ai.FindBestRangedTile(latestPos, targetPos, reachable, minRange, maxRange);
            
            if (bestMove == latestPos) return Node.Status.Success;
            ai.setLatestPos(bestMove);
            Debug.Log("Move For Ranged is executed");
            var path = ai.getPreview().FindPathAStar(latestPos, bestMove);
            
            ai.getPreview().ClearAll();
            ai.StartCoroutine(ai.EnemyWalkPath(path, latestPos, enemyChar));
            
            startedMovement = true;
            return Node.Status.Running;
        }
    }

    public class MoveTowardPlayer : IStrategy
    {
        EnemyAIControllerBase ai;
        bool startedMovement = false;

        public MoveTowardPlayer(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var latestPos = ai.getLatestPos();
            if (startedMovement) return ai.isMoving ? Node.Status.Running : Node.Status.Success;
            
            var enemyChar = ai.getGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            ai.getPreview().ShowMovementRange(latestPos, enemyChar.RemainingMoveRange);
            var reachable = ai.getPreview().GetReachableTiles();

            Vector3Int bestMove = ai.FindMoveToward(latestPos, ai.getTargetPos(), reachable);
            ai.setLatestPos(bestMove);

            if (bestMove == latestPos)
                return Node.Status.Success;
            
            var path = ai.getPreview().FindPathAStar(latestPos, bestMove);
            ai.getPreview().ClearAll();
            ai.StartCoroutine(ai.EnemyWalkPath(path, latestPos, enemyChar));
            startedMovement = true;
            return Node.Status.Running;
        }
    }
}