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

    public class AlwaysSucceed : IStrategy
    {
        public Node.Status Process() => Node.Status.Success;
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
            bool temp = ai.GetGridData().GetTileAt(ai.GetCurrentPosition())?.PlacedObject is CharacterObject;

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
            ai.SetTargetPos(ai.FindClosest(ai.GetCurrentPosition(), ai.GetGridData().GetAllFriendlies()));
            return Node.Status.Success;
        }
    }

    public class IsLowHealth : IStrategy
    {
        private readonly EnemyAIControllerBase ai;
        private readonly float threshold;

        public IsLowHealth(EnemyAIControllerBase ai, float threshold = 0.3f)
        {
            this.ai = ai;
            this.threshold = threshold;
        }

        public Node.Status Process()
        {
            Vector3Int pos = ai.GetCurrentPosition();
            CharacterObject character = ai.GetGridData().GetTileAt(pos)?.PlacedObject as CharacterObject;

            if (character == null)
                return Node.Status.Failure;

            float healthPercent = (float)character.HP / character.MaxHp;
            return healthPercent <= threshold ? Node.Status.Success : Node.Status.Failure;
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
            var enemyChar = ai.GetGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
            if (enemyChar == null)
                return Node.Status.Failure;
            bool inRange = ai.IsInRange(latestPos, ai.GetTargetPos(), enemyChar.AtkRange);

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
            var targetPos = ai.GetTargetPos();

            int dist = Mathf.Abs(enemyPos.x - targetPos.x) + Mathf.Abs(enemyPos.y - targetPos.y);
            int minRange = 3;
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
            bool hasLineOfSight = ai.HasLineOfSight(ai.GetCurrentPosition(), ai.GetTargetPos());
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
            CharacterObject enemyChar = ai.GetGridData().GetTileAt(latestPos)?.PlacedObject as CharacterObject;
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

            executor.ExecuteAttack(enemyChar, latestPos, ai.GetTargetPos(), ai.GetGridData());
            startedAttack = true;
            return Node.Status.Running;
        } 

        public void Reset()
        {
            startedAttack = false;
        }
    }

    public class HealTarget : IStrategy
    {
        private readonly EnemyAIControllerBase ai;
        private readonly int healAmount;

        public HealTarget(EnemyAIControllerBase ai, int healAmount = 5)
        {
            this.ai = ai;
            this.healAmount = healAmount;
        }

        public Node.Status Process()
        {
            Vector3Int targetPos = ai.GetTargetPos();
            CharacterObject target = ai.GetGridData().GetTileAt(targetPos)?.PlacedObject as CharacterObject;

            if (target == null || target.HP <= 0)
                return Node.Status.Failure;

            target.Heal(healAmount);

            Debug.Log($"Cleric healed {target.Name} for {healAmount} HP. " +
                    $"Current HP: {target.HP}/{target.MaxHp}");

            return Node.Status.Success;
        }
    }

    public class MoveToRandomTile : MoveStrategyBase
    {
        public MoveToRandomTile(EnemyAIControllerBase ai) : base(ai) {}

        protected override Vector3Int SelectTargetTile(Vector3Int current, HashSet<Vector3Int> reachableTile, CharacterObject character)
        {
            HashSet<Vector3Int> reachable = new HashSet<Vector3Int>(ai.GetPreview().GetReachableTiles());
            reachable.Remove(current);

            if (reachable.Count == 0)
                return current;

            return reachable.ElementAt(UnityEngine.Random.Range(0, reachable.Count));
        }

    }

    public class MoveToBackline : MoveStrategyBase
    {
        public MoveToBackline(EnemyAIControllerBase ai) : base(ai) {}

        protected override Vector3Int SelectTargetTile(
            Vector3Int current,
            HashSet<Vector3Int> reachable,
            CharacterObject character)
        {
            var enemies = ai.GetGridData().GetEnemyTeamUnit(character.Team);

            if (enemies == null || enemies.Count == 0) return current;

            Vector3Int bestTile = current;
            int bestMinDist = int.MinValue;

            foreach (var tile in reachable)
            {
                int minEnemyCost  = int.MaxValue;

                foreach (var (enemyPos, enemy) in enemies)
                {
                    if (enemy.HP <= 0) continue;

                    int cost = ai.GetPreview().PathCost(enemyPos, tile);

                    if (cost < minEnemyCost)
                        minEnemyCost  = cost;
                }

                if (minEnemyCost  > bestMinDist)
                {
                    bestMinDist = minEnemyCost ;
                    bestTile = tile;
                }
            }

            return bestTile;
        }
    }

    public class MoveAwayFromEnemy : MoveStrategyBase
    {
        public MoveAwayFromEnemy(EnemyAIControllerBase ai) : base(ai) {}

        protected override Vector3Int SelectTargetTile(
            Vector3Int current,
            HashSet<Vector3Int> reachable,
            CharacterObject character)
        {
            return ai.FindMoveAwayFrom(current, ai.GetTargetPos(), reachable);
        }
    }

    public class MoveTowardCleric : MoveStrategyBase
    {
        private readonly int clericID;

        public MoveTowardCleric(EnemyAIControllerBase ai, int clericID = 2) : base(ai)
        {
            this.clericID = clericID;
        }

        protected override Vector3Int SelectTargetTile(
            Vector3Int current,
            HashSet<Vector3Int> reachable,
            CharacterObject character)
        {
            var allies = ai.GetGridData().GetUnitsByTeam(character.Team);
            Vector3Int? clericPos = null;

            foreach (var (pos, ally) in allies)
            {
                if (ally == character) continue;
                if (ally.ID == clericID && ally.HP > 0)
                {
                    clericPos = pos;
                    break;
                }
            }

            if (clericPos.HasValue)
                return ai.FindMoveToward(current, clericPos.Value, reachable);

            return ai.FindMoveAwayFrom(current, ai.GetTargetPos(), reachable);
        }
    }

    public class FindWeakest : IStrategy
    {
        private readonly EnemyAIControllerBase ai;

        public FindWeakest(EnemyAIControllerBase ai)
        {
            this.ai = ai;
        }

        public Node.Status Process()
        {
            var enemies = ai.GetGridData().GetEnemyTeamUnit(
                ai.GetGridData().GetTileAt(ai.GetCurrentPosition())?.PlacedObject is CharacterObject c
                    ? c.Team : 1);

            if (enemies == null || enemies.Count == 0)
                return Node.Status.Failure;

            Vector3Int currentPos = ai.GetCurrentPosition();
            Vector3Int bestPos = currentPos;
            int lowestHP = int.MaxValue;
            int bestDist = int.MaxValue;

            foreach (var (pos, character) in enemies)
            {
                if (character.HP <= 0) continue;

                int dist = Mathf.Abs(currentPos.x - pos.x) + Mathf.Abs(currentPos.y - pos.y);

                if (character.HP < lowestHP || (character.HP == lowestHP && dist < bestDist))
                {
                    lowestHP = character.HP;
                    bestDist = dist;
                    bestPos = pos;
                }
            }

            ai.SetTargetPos(bestPos);
            return Node.Status.Success;
        }
    }

    public class FindWoundedAlly : IStrategy
    {
        private readonly EnemyAIControllerBase ai;
        private readonly float healThreshold;
        private readonly int healRange;
        private readonly bool preferClosest;

        public FindWoundedAlly(EnemyAIControllerBase ai, float healThreshold = 0.5f,
                                int healRange = int.MaxValue, bool preferClosest = false)
        {
            this.ai = ai;
            this.healThreshold = healThreshold;
            this.healRange = healRange;
            this.preferClosest = preferClosest;
        }

        public Node.Status Process()
        {
            Vector3Int currentPos = ai.GetCurrentPosition();
            CharacterObject self = ai.GetGridData().GetTileAt(currentPos)?.PlacedObject as CharacterObject;

            if (self == null)
                return Node.Status.Failure;

            var allies = ai.GetGridData().GetUnitsByTeam(self.Team);

            Vector3Int bestPos = currentPos;
            int lowestHP = int.MaxValue;
            int closestDist = int.MaxValue;
            bool foundWounded = false;

            foreach (var (pos, ally) in allies)
            {
                // if (ally == self) continue;
                if (ally.HP <= 0) continue;

                float healthPercent = (float)ally.HP / ally.MaxHp;

                if (healthPercent > healThreshold) continue;

                int dist = Mathf.Abs(currentPos.x - pos.x) + Mathf.Abs(currentPos.y - pos.y);
                if (dist > healRange) continue;

                if (preferClosest)
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        bestPos = pos;
                        foundWounded = true;
                    }
                }
                else
                {
                    if (ally.HP < lowestHP)
                    {
                        lowestHP = ally.HP;
                        bestPos = pos;
                        foundWounded = true;
                    }
                }
            }

            if (!foundWounded)
                return Node.Status.Failure;

            ai.SetTargetPos(bestPos);
            return Node.Status.Success;
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
            var targetPos = ai.GetTargetPos();

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
            return ai.FindMoveToward(current, ai.GetTargetPos(), reachable);
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
            
            GridData gridData = ai.GetGridData();
            Vector3Int latestPos = ai.GetCurrentPosition();
            CharacterObject enemyChar = gridData.GetTileAt(latestPos)?.PlacedObject as CharacterObject;

            if (enemyChar == null)
                return Node.Status.Failure;

            HashSet<Vector3Int> reachable = ai.GetPreview().ComputeReachableTiles(latestPos, enemyChar.RemainingMoveRange);

            if (reachable == null || reachable.Count == 0)
            {
                ai.GetPreview().ClearAll();
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