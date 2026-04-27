using BehaviourTrees;

public class EnemyAIController_2 : EnemyAIControllerBase
{
    protected override string GetTreeName() => "RangedEnemy";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence tooCloseDecision = new Sequence("TooCloseDecision");
        tooCloseDecision.AddChild(new Leaf("IsTooClose", new IsTooClose(this)));
        
        RandomSelector panicDecision = new RandomSelector("PanicDecision");

        Sequence panicAttack = new Sequence("PanicAttack");
        panicAttack.AddChild(new Leaf("HasLineOfSight", new HasLineOfSight(this)));
        panicAttack.AddChild(new Leaf("Attack", new Attack(this)));
        panicDecision.AddChild(panicAttack);

        panicDecision.AddChild(new Leaf("MoveForRanged", new MoveForRanged(this)));
        tooCloseDecision.AddChild(panicDecision);

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));

        Inverter notTooClose = new Inverter("notTooClose");
        notTooClose.AddChild(new Leaf("IsTooClose", new IsTooClose(this)));
        tryAttack.AddChild(notTooClose);

        tryAttack.AddChild(new Leaf("HasLineOfSight", new HasLineOfSight(this)));
        tryAttack.AddChild(new Leaf("Attack", new Attack(this)));

        combat.AddChild(tooCloseDecision);
        combat.AddChild(tryAttack);

        combat.AddChild(new Leaf("MoveForRanged", new MoveForRanged(this)));
        root.AddChild(combat);

        return root;
    }
}

