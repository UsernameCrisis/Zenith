using BehaviourTrees;

public class EnemyAIController_1 : EnemyAIControllerBase
{
    // protected override string GetTreeName() => "AggressiveEnemy";
    
    // protected override Node BuildTree()
    // {
    //     Sequence root = new Sequence("Root");

    //     Sequence preAction = new Sequence("PreAction");
    //     preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
    //     preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
    //     root.AddChild(preAction);

    //     Selector combat = new Selector("Combat");

    //     Sequence tryAttack = new Sequence("TryAttack");
    //     tryAttack.AddChild(new Leaf("IsInRange1", new IsInRange(this)));
    //     tryAttack.AddChild(new Leaf("DoAttack1", new Attack(this)));
    //     combat.AddChild(tryAttack);

    //     Sequence moveAndAttack = new Sequence("MoveAndAttack");
    //     moveAndAttack.AddChild(new Leaf("MoveTowardPlayer", new MoveTowardTarget(this)));
    //     moveAndAttack.AddChild(new Leaf("IsInRange2 ", new IsInRange(this)));
    //     moveAndAttack.AddChild(new Leaf("DoAttack2", new Attack(this)));
    //     combat.AddChild(moveAndAttack);
    //     root.AddChild(combat);

    //     return root;
    // }

    protected override string GetTreeName() => "FrontlineTank";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");
        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindHighestThreat", new FindHighestThreat(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence finishThreat = new Sequence("FinishThreat");
        finishThreat.AddChild(new Leaf("CanKillTarget", new CanKillTarget(this)));

        Selector killOrApproach = new Selector("KillOrApproach");

        Sequence killNow = new Sequence("KillNow");
        killNow.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        killNow.AddChild(new Leaf("Attack", new Attack(this)));
        killOrApproach.AddChild(killNow);

        Sequence pursueAndKill = new Sequence("PursueAndKill");
        pursueAndKill.AddChild(new Leaf("MoveToFrontline", new MoveToFrontline(this)));

        Selector tryKillAfterMove = new Selector("TryKillAfterMove");
        Sequence killAfterMove = new Sequence("KillAfterMove");
        killAfterMove.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        killAfterMove.AddChild(new Leaf("Attack", new Attack(this)));
        tryKillAfterMove.AddChild(killAfterMove);

        tryKillAfterMove.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        pursueAndKill.AddChild(tryKillAfterMove);
        killOrApproach.AddChild(pursueAndKill);

        finishThreat.AddChild(killOrApproach);
        combat.AddChild(finishThreat);

        Sequence attackAndReposition = new Sequence("AttackAndReposition");
        attackAndReposition.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        attackAndReposition.AddChild(new Leaf("Attack", new Attack(this)));
        attackAndReposition.AddChild(new Leaf("ImprovePosition", new MoveToFrontline(this)));
        combat.AddChild(attackAndReposition);

        Sequence engageFrontline = new Sequence("EngageFrontline");
        engageFrontline.AddChild(new Leaf("MoveToFrontline", new MoveToFrontline(this)));

        Selector tryAttackAfterMove = new Selector("TryAttackAfterMove");
        Sequence attackIfReached = new Sequence("AttackIfReached");
        attackIfReached.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        attackIfReached.AddChild(new Leaf("Attack", new Attack(this)));
        tryAttackAfterMove.AddChild(attackIfReached);
        
        tryAttackAfterMove.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        engageFrontline.AddChild(tryAttackAfterMove);
        combat.AddChild(engageFrontline);

        root.AddChild(combat);
        return root;
    }
}