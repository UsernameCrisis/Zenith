using BehaviourTrees;

public class EnemyAIController_2 : EnemyAIControllerBase
{
    // protected override string GetTreeName() => "RangedEnemy";

    // protected override Node BuildTree()
    // {
    //     Sequence root = new Sequence("Root");

    //     Sequence preAction = new Sequence("PreAction");
    //     preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
    //     preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
    //     root.AddChild(preAction);

    //     Selector combat = new Selector("Combat");

    //     Sequence tooCloseDecision = new Sequence("TooCloseDecision");
    //     tooCloseDecision.AddChild(new Leaf("IsTooClose", new IsTooClose(this)));
        
    //     RandomSelector panicDecision = new RandomSelector("PanicDecision");

    //     Sequence panicAttack = new Sequence("PanicAttack");
    //     panicAttack.AddChild(new Leaf("HasLineOfSight", new HasLineOfSight(this)));
    //     panicAttack.AddChild(new Leaf("Attack", new Attack(this)));
    //     panicDecision.AddChild(panicAttack);

    //     panicDecision.AddChild(new Leaf("MoveForRanged", new MoveForRanged(this)));
    //     tooCloseDecision.AddChild(panicDecision);

    //     Sequence tryAttack = new Sequence("TryAttack");
    //     tryAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));

    //     Inverter notTooClose = new Inverter("notTooClose");
    //     notTooClose.AddChild(new Leaf("IsTooClose", new IsTooClose(this)));
    //     tryAttack.AddChild(notTooClose);

    //     tryAttack.AddChild(new Leaf("HasLineOfSight", new HasLineOfSight(this)));
    //     tryAttack.AddChild(new Leaf("Attack", new Attack(this)));

    //     combat.AddChild(tooCloseDecision);
    //     combat.AddChild(tryAttack);

    //     combat.AddChild(new Leaf("MoveForRanged", new MoveForRanged(this)));
    //     root.AddChild(combat);

    //     return root;
    // }

    private const int SafeDistance = 2;

    protected override string GetTreeName() => "RangedSkirmisher";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindMostKillable", new FindMostKillable(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence idealPosition = new Sequence("IdealPosition");
        idealPosition.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        idealPosition.AddChild(new Leaf("Attack", new Attack(this)));
        combat.AddChild(idealPosition);

        // Branch B: An enemy is dangerously close. This is the kiting response.
        // The unit decides whether to attack first (punish the aggressor) or
        // just flee immediately depending on whether it can still fire.
        Sequence tooCloseResponse = new Sequence("TooCloseResponse");
        tooCloseResponse.AddChild(new Leaf("IsTooClose", new IsTooClose(this, SafeDistance)));

        Selector counterAndReposition = new Selector("CounterAndReposition");

        Sequence counterAttack = new Sequence("CounterAttack");
        counterAttack.AddChild(new Leaf("HasLineOfSight", new HasLineOfSight(this)));
        counterAttack.AddChild(new Leaf("Attack", new Attack(this)));
        counterAndReposition.AddChild(counterAttack);

        counterAndReposition.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        tooCloseResponse.AddChild(counterAndReposition);

        tooCloseResponse.AddChild(new Leaf("Reposition", new MoveToAttackRangeOf(this, rangeTolerance: 1)));
        combat.AddChild(tooCloseResponse);

        // Branch C: Default approach — move to optimal range, then try to attack.
        // MoveToAttackRangeOf will position at max attack range if possible,
        // or approach if no good tile is reachable this turn.
        Sequence positionAndFire = new Sequence("PositionAndFire");
        positionAndFire.AddChild(new Leaf("MoveToRange", new MoveToAttackRangeOf(this, rangeTolerance: 1)));

        Selector tryFireAfterMove = new Selector("TryFireAfterMove");
        Sequence fireIfInRange = new Sequence("FireIfInRange");
        fireIfInRange.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        fireIfInRange.AddChild(new Leaf("Attack", new Attack(this)));
        tryFireAfterMove.AddChild(fireIfInRange);
        
        tryFireAfterMove.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        positionAndFire.AddChild(tryFireAfterMove);
        combat.AddChild(positionAndFire);

        root.AddChild(combat);
        return root;
    }
}

