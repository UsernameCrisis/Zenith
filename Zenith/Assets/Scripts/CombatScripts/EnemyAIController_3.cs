using System.Collections.Generic;
using BehaviourTrees;

public class EnemyAIController_3 : EnemyAIControllerBase
{
    protected override string GetTreeName() => "HitAndRunEnemy";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Attack1");

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange1", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("DoAttack1", new Attack(this)));
        tryAttack.AddChild(new Leaf("MoveRandom1", new MoveToRandomTile(this)));
        combat.AddChild(tryAttack);

        ProbabilitySelector randomMove2 = new ProbabilitySelector("RandomMove", new List<float> { 0.7f, 0.3f });
        randomMove2.AddChild(new Leaf("MoveTowardPlayer", new MoveTowardPlayer(this)));
        randomMove2.AddChild(new Leaf("MoveRandom2", new MoveToRandomTile(this)));
        combat.AddChild(randomMove2);

        root.AddChild(combat);

        return root;
    }
}
