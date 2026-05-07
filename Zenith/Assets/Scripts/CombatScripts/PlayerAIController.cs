using BehaviourTrees;

public class PlayerAIController : EnemyAIControllerBase
{
    private const float LowHealthThreshold = 0.3f;
    private const int ClericID = 2;

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

        kiteAndRetreat.AddChild(new Leaf("MoveTowardCleric", new MoveTowardCleric(this, ClericID)));
        combat.AddChild(kiteAndRetreat);

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("Attack", new Attack(this)));
        combat.AddChild(tryAttack);

        combat.AddChild(new Leaf("MoveTowardEnemy", new MoveTowardPlayer(this)));
        root.AddChild(combat);
        return root;
    }
}
