using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

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
            bool temp = ai.getGridData().GetTileAt(ai.GetCurrentPosition())?.PlacedObject is CharacterObject;

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
            ai.setTargetPos(ai.FindClosest(ai.GetCurrentPosition(), ai.getGridData().GetAllFriendlies()));
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
            var latestPos = ai.GetCurrentPosition();
            var enemyChar = ai.getGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            if (enemyChar == null)
                return Node.Status.Failure;
            bool inRange = ai.IsInRange(latestPos, ai.getTargetPos(), enemyChar.AtkRange);

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
            var enemyPos = ai.GetCurrentPosition();
            var targetPos = ai.getTargetPos();

            int dist = Mathf.Abs(enemyPos.x - targetPos.x) + Mathf.Abs(enemyPos.y - targetPos.y);
            int minRange = 2;
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
            bool hasLineOfSight = ai.HasLineOfSight(ai.GetCurrentPosition(), ai.getTargetPos());
            return hasLineOfSight ? Node.Status.Success : Node.Status.Failure;
        }
    }

    public class Attack : IStrategy
    {
        EnemyAIControllerBase ai;
        private bool startedAttack = false;

        public Attack(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var latestPos = ai.GetCurrentPosition();
            CharacterObject enemyChar = ai.getGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            if (enemyChar is null)
                return Node.Status.Failure;
            CombatExecutor executor = ai.GetCombatExecutor();
            if (startedAttack)
            {
                if (executor.IsAttacking)
                    return Node.Status.Running;
                startedAttack = false;
                return Node.Status.Success;
            }

            executor.ExecuteAttack(enemyChar, latestPos, ai.getTargetPos(), ai.getGridData());
            startedAttack = true;
            return Node.Status.Running;
        } 

        public void Reset()
        {
            startedAttack = false;
        }
    }

    public class MoveToRandomTile : MoveStrategyBase
    {
        public MoveToRandomTile(EnemyAIControllerBase ai) : base(ai) {}

        protected override Vector3Int SelectTargetTile(Vector3Int current, HashSet<Vector3Int> reachableTile, CharacterObject character)
        {
            HashSet<Vector3Int> reachable = new HashSet<Vector3Int>(ai.getPreview().GetReachableTiles());
            reachable.Remove(current);

            if (reachable.Count == 0)
                return current;

            return reachable.ElementAt(UnityEngine.Random.Range(0, reachable.Count));
        }

    }

    public class MoveForRanged : MoveStrategyBase
    {
        public MoveForRanged(EnemyAIControllerBase ai) : base(ai) {}

        protected override Vector3Int SelectTargetTile(
            Vector3Int current,
            HashSet<Vector3Int> reachable,
            CharacterObject character)
        {
            var targetPos = ai.getTargetPos();

            int minRange = 2;
            int maxRange = character.AtkRange;

            int dist = Mathf.Abs(current.x - targetPos.x) + Mathf.Abs(current.y - targetPos.y);

            if (dist >= minRange && dist <= maxRange &&
                ai.HasLineOfSight(current, targetPos))
            {
                return current;
            }
            return ai.FindBestRangedTile(current, targetPos, reachable, minRange, maxRange);
        }
    }

    public class MoveTowardPlayer : MoveStrategyBase
    {
        public MoveTowardPlayer(EnemyAIControllerBase ai) : base(ai) {}

        protected override Vector3Int SelectTargetTile(
            Vector3Int current,
            HashSet<Vector3Int> reachable,
            CharacterObject character)
        {
            return ai.FindMoveToward(current, ai.getTargetPos(), reachable);
        }
    }

    public abstract class MoveStrategyBase : IStrategy
    {
        protected EnemyAIControllerBase ai;
        protected bool startedMovement = false;

        public MoveStrategyBase(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            CombatExecutor executor = ai.GetCombatExecutor();
    
            if (startedMovement)
            {
                if (executor.IsMoving)
                    return Node.Status.Running;

                startedMovement = false;
                return Node.Status.Success;
            }
            
            GridData gridData = ai.getGridData();
            Vector3Int latestPos = ai.GetCurrentPosition();
            CharacterObject enemyChar = gridData.GetTileAt(latestPos)?.PlacedObject as CharacterObject;

            if (enemyChar == null)
                return Node.Status.Failure;

            ai.getPreview().ShowMovementRange(latestPos, enemyChar.RemainingMoveRange);
            HashSet<Vector3Int> reachable = ai.getPreview().GetReachableTiles();

            if (reachable == null || reachable.Count == 0)
            {
                ai.getPreview().ClearAll();
                return Node.Status.Failure;
            }

            Vector3Int bestMove = SelectTargetTile(latestPos, reachable, enemyChar);
            
            if (bestMove == latestPos)
            {
                startedMovement = false;
                return Node.Status.Success;
            }
                

            executor.ExecuteMove(enemyChar, latestPos, bestMove, gridData);

            startedMovement = true;
            return Node.Status.Running;
        }

        public void Reset()
        {
            startedMovement = false;
        }

        protected abstract Vector3Int SelectTargetTile(
            Vector3Int current,
            HashSet<Vector3Int> reachable,
            CharacterObject character
        );
    }
}