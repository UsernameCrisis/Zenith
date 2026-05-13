using System.Collections.Generic;
using BehaviourTrees;

public class PlayerAIController : EnemyAIControllerBase
{
    private const float LowHealthThreshold = 0.3f;
    private const int ClericID = 2;
    private const float FleeWeight = 0.7f;
    private const float FightWeight = 0.3f;

    protected override string GetTreeName() => "PlayerAI";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence kiteAndRetreat = new Sequence("KiteAndRetreat");
        kiteAndRetreat.AddChild(new Leaf("IsLowHealth", new IsLowHealth(this, LowHealthThreshold)));

        Selector tryKiteOrSkip = new Selector("TryKiteOrSkip");

        Sequence tryKiteAttack = new Sequence("TryKiteAttack");
        tryKiteAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryKiteAttack.AddChild(new Leaf("Attack", new Attack(this)));
        tryKiteOrSkip.AddChild(tryKiteAttack);

        tryKiteOrSkip.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        kiteAndRetreat.AddChild(tryKiteOrSkip);

        ProbabilitySelector fightOrFlight = new ProbabilitySelector("FightOrFlight", new List<float> { FleeWeight, FightWeight });

        fightOrFlight.AddChild(new Leaf("MoveTowardCleric", new MoveTowardCleric(this, ClericID)));

        Sequence standAndFight = new Sequence("StandAndFight");
        Selector tryFightBack = new Selector("TryFightBack");

        Sequence tryAttackInPlace = new Sequence("TryAttackInPlace");
        tryAttackInPlace.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryAttackInPlace.AddChild(new Leaf("Attack", new Attack(this)));
        tryFightBack.AddChild(tryAttackInPlace);

        tryFightBack.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        standAndFight.AddChild(tryFightBack);
        fightOrFlight.AddChild(standAndFight);
        kiteAndRetreat.AddChild(fightOrFlight);
        combat.AddChild(kiteAndRetreat);

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("Attack", new Attack(this)));
        combat.AddChild(tryAttack);

        Sequence moveAndAttack = new Sequence("MoveAndAttack");
        moveAndAttack.AddChild(new Leaf("MoveTowardEnemy", new MoveTowardTarget(this)));

        Selector tryAttackAfterMove = new Selector("TryAttackAfterMove");
        Sequence tryAttackAfterMoveSeq = new Sequence("TryAttackAfterMoveSeq");
        tryAttackAfterMoveSeq.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryAttackAfterMoveSeq.AddChild(new Leaf("Attack", new Attack(this)));
        tryAttackAfterMove.AddChild(tryAttackAfterMoveSeq);

        tryAttackAfterMove.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        moveAndAttack.AddChild(tryAttackAfterMove);
        combat.AddChild(moveAndAttack);
        root.AddChild(combat);
        return root;
    }
}
