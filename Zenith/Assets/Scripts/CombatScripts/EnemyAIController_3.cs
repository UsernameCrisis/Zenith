using System.Collections.Generic;
using BehaviourTrees;

public class EnemyAIController_3 : EnemyAIControllerBase
{
    // protected override string GetTreeName() => "HitAndRunEnemy";

    // protected override Node BuildTree()
    // {
    //     Sequence root = new Sequence("Root");

    //     Sequence preAction = new Sequence("PreAction");
    //     preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
    //     preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
    //     root.AddChild(preAction);

    //     Selector combat = new Selector("Attack1");

    //     Sequence tryAttack = new Sequence("TryAttack");
    //     tryAttack.AddChild(new Leaf("IsInRange1", new IsInRange(this)));
    //     tryAttack.AddChild(new Leaf("DoAttack1", new Attack(this)));
    //     tryAttack.AddChild(new Leaf("MoveRandom1", new MoveToRandomTile(this)));
    //     combat.AddChild(tryAttack);

    //     ProbabilitySelector randomMove2 = new ProbabilitySelector("RandomMove", new List<float> { 0.7f, 0.3f });
    //     randomMove2.AddChild(new Leaf("MoveTowardPlayer", new MoveTowardTarget(this)));
    //     randomMove2.AddChild(new Leaf("MoveRandom2", new MoveToRandomTile(this)));
    //     combat.AddChild(randomMove2);

    //     root.AddChild(combat);

    //     return root;
    // }

    protected override string GetTreeName() => "AggressiveMelee";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindMostKillable", new FindMostKillable(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence attackFirst = new Sequence("AttackFirst");
        attackFirst.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        attackFirst.AddChild(new Leaf("Attack", new Attack(this)));
        attackFirst.AddChild(new Leaf("CloseIn", new MoveTowardTarget(this)));
        combat.AddChild(attackFirst);

        Sequence moveAndAttack = new Sequence("MoveAndAttack");
        moveAndAttack.AddChild(new Leaf("MoveTowardTarget", new MoveTowardTarget(this)));

        Selector tryAttackAfterMove = new Selector("TryAttackAfterMove");
        Sequence attackIfInRange = new Sequence("AttackIfInRange");
        attackIfInRange.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        attackIfInRange.AddChild(new Leaf("Attack", new Attack(this)));
        tryAttackAfterMove.AddChild(attackIfInRange);

        tryAttackAfterMove.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        moveAndAttack.AddChild(tryAttackAfterMove);
        combat.AddChild(moveAndAttack);

        root.AddChild(combat);
        return root;
    }
}
