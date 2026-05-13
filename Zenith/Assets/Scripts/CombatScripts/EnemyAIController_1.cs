using BehaviourTrees;

public class EnemyAIController_1 : EnemyAIControllerBase
{
    protected override string GetTreeName() => "AggressiveEnemy";
    
    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange1", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("DoAttack1", new Attack(this)));
        combat.AddChild(tryAttack);

        Sequence moveAndAttack = new Sequence("MoveAndAttack");
        moveAndAttack.AddChild(new Leaf("MoveTowardPlayer", new MoveTowardTarget(this)));
        moveAndAttack.AddChild(new Leaf("IsInRange2 ", new IsInRange(this)));
        moveAndAttack.AddChild(new Leaf("DoAttack2", new Attack(this)));
        combat.AddChild(moveAndAttack);
        root.AddChild(combat);

        return root;
    }
}