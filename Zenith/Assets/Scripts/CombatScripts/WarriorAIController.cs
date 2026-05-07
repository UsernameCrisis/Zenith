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

        combat.AddChild(new Leaf("MoveTowardWeakest", new MoveTowardPlayer(this)));

        root.AddChild(combat);
        return root;
    }
}
