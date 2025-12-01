using System;
using UnityEngine;


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
        EnemyAIController_1 ai;

        public CheckEnemyExists(EnemyAIController_1 ai)
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

    public class FindClosestPlayer : IStrategy
    {
        EnemyAIController_1 ai;
        
        public FindClosestPlayer(EnemyAIController_1 ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            ai.setTargetPos(ai.FindClosestPlayer(ai.getLatestPos(), ai.getGridData().GetAllFriendlies()));
            return Node.Status.Success;
        }
    }
    
    public class IsInRange : IStrategy
    {
        EnemyAIController_1 ai;

        public IsInRange(EnemyAIController_1 ai)
        {
            this. ai = ai;
        }

        public Node.Status Process()
        {
            var latestPos = ai.getLatestPos();
            var enemyChar = ai.getGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            bool inRange = ai.IsInRange(latestPos, ai.getTargetPos(), enemyChar.AtkRange);
            return inRange ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class Attack : IStrategy
    {
        EnemyAIController_1 ai;

        public Attack(EnemyAIController_1 ai)
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

    public class MoveTowardPlayer : IStrategy
    {
        EnemyAIController_1 ai;
        bool startedMovement = false;

        public MoveTowardPlayer(EnemyAIController_1 ai)
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
            
            latestPos = bestMove;
            return Node.Status.Running;
        }
    }
}