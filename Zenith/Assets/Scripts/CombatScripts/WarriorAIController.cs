using BehaviourTrees;

public class WarriorAIController : EnemyAIControllerBase
{
    protected override string GetTreeName() => "WarriorAI";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindWeakest", new FindWeakest(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("Attack", new Attack(this)));
        combat.AddChild(tryAttack);

        Sequence moveAndAttack = new Sequence("MoveAndAttack");
        moveAndAttack.AddChild(new Leaf("MoveTowardWeakest", new MoveTowardTarget(this)));

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
