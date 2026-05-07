using BehaviourTrees;

public class ClericAIController : EnemyAIControllerBase
{
    private const int HealAmount = 5;
    private const float HealThreshold = 0.5f;
    private const int HealRange = 2;

    protected override string GetTreeName() => "ClericAI";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence reposition = new Sequence("Reposition");
        reposition.AddChild(new Leaf("IsTooClose", new IsTooClose(this)));
        reposition.AddChild(new Leaf("MoveToBackline", new MoveToBackline(this)));
        combat.AddChild(reposition);

        Sequence activeTurn = new Sequence("ActiveTurn");

        Selector tryHealClosest = new Selector("TryHealClosest");
        Sequence healInRange = new Sequence("HealInRange");
        healInRange.AddChild(new Leaf("FindWoundedAllyClosest",
            new FindWoundedAlly(this, HealThreshold, HealRange, preferClosest: true)));

        healInRange.AddChild(new Leaf("HealTarget", new HealTarget(this, HealAmount)));
        tryHealClosest.AddChild(healInRange);

        tryHealClosest.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        activeTurn.AddChild(tryHealClosest);

        Selector tryMoveUsefully = new Selector("TryMoveUsefully");

        Sequence chaseWounded = new Sequence("ChaseWounded");
        chaseWounded.AddChild(new Leaf("FindWoundedAllyAny",
            new FindWoundedAlly(this, HealThreshold)));

        chaseWounded.AddChild(new Leaf("MoveTowardWounded", new MoveTowardPlayer(this)));
        tryMoveUsefully.AddChild(chaseWounded);

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("Attack", new Attack(this)));
        tryMoveUsefully.AddChild(tryAttack);

        tryMoveUsefully.AddChild(new Leaf("MoveToBackline", new MoveToBackline(this)));

        activeTurn.AddChild(tryMoveUsefully);
        combat.AddChild(activeTurn);

        root.AddChild(combat);
        return root;
    }
}
