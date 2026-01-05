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

        Selector attack1 = new Selector("Attack1");

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange1", new IsInRange(this)));

        RandomSelector randomMove1 = new RandomSelector("RandomMove1");
        randomMove1.AddChild(new Leaf("DoAttack1", new Attack(this)));
        randomMove1.AddChild(new Leaf("MoveRandom1", new MoveToRandomTile(this)));
        tryAttack.AddChild(randomMove1);

        attack1.AddChild(tryAttack);

        ProbabilitySelector randomMove2 = new ProbabilitySelector("RandomMove", new List<float> { 0.7f, 0.3f });
        randomMove2.AddChild(new Leaf("MoveTowardPlayer", new MoveTowardPlayer(this)));
        randomMove2.AddChild(new Leaf("MoveRandom2", new MoveToRandomTile(this)));
        attack1.AddChild(randomMove2);

        root.AddChild(attack1);

        Sequence attack2 = new Sequence("Attack2");
        attack2.AddChild(new Leaf("IsInRange2", new IsInRange(this)));
        attack2.AddChild(new Leaf("DoAttack2", new Attack(this)));
        root.AddChild(attack2);

        return root;
    }
}
