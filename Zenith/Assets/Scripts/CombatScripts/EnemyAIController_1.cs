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

        Selector attack1 = new Selector("Attack1");

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange1", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("DoAttack1", new Attack(this)));
        attack1.AddChild(tryAttack);

        attack1.AddChild(new Leaf("MoveTowardPlayer", new MoveTowardPlayer(this)));
        root.AddChild(attack1);

        Sequence attack2 = new Sequence("Attack2");
        attack2.AddChild(new Leaf("IsInRange2", new IsInRange(this)));
        attack2.AddChild(new Leaf("DoAttack2", new Attack(this)));
        root.AddChild(attack2);

        return root;
    }
}