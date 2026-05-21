using System.Collections.Generic;
using BehaviourTrees;

public class ClericAIController : EnemyAIControllerBase
{
    private const int HealAmount = 10;
    private const float HealThreshold = 0.5f;
    private const int HealRange = 2;
    private const int SafeDistance = 3;
    private const float AttackWeight = 0.4f;
    private const float RepositionWeight = 0.6f;

    protected override string GetTreeName() => "ClericAI";

    protected override Node BuildTree()
    {
        Sequence root = new Sequence("Root");

        Sequence preAction = new Sequence("PreAction");
        preAction.AddChild(new Leaf("CheckExists", new CheckEnemyExists(this)));
        preAction.AddChild(new Leaf("FindClosest", new FindClosest(this)));
        root.AddChild(preAction);

        Selector combat = new Selector("Combat");

        Sequence aloneMode = new Sequence("AloneMode");
        aloneMode.AddChild(new Leaf("IsAloneOnTeam", new IsAloneOnTeam(this)));

        Selector aloneRangedCombat = new Selector("AloneRangedCombat");
        Sequence attackIfInRange = new Sequence("AttackIfInRange");
        attackIfInRange.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        attackIfInRange.AddChild(new Leaf("Attack", new Attack(this)));
        aloneRangedCombat.AddChild(attackIfInRange);

        Sequence repositionAndFire = new Sequence("RepositionAndFire");
        repositionAndFire.AddChild(new Leaf("MoveToRange", new MoveToAttackRangeOf(this, rangeTolerance: 1)));

        Selector tryFireAfterReposition = new Selector("TryFireAfterReposition");
        Sequence fireIfReached = new Sequence("FireIfReached");
        fireIfReached.AddChild(new Leaf("IsAlreadyInRange", new IsAlreadyInRange(this)));
        fireIfReached.AddChild(new Leaf("Attack", new Attack(this)));
        tryFireAfterReposition.AddChild(fireIfReached);
        tryFireAfterReposition.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));

        repositionAndFire.AddChild(tryFireAfterReposition);
        aloneRangedCombat.AddChild(repositionAndFire);
        aloneMode.AddChild(aloneRangedCombat);
        combat.AddChild(aloneMode);

        Sequence reposition = new Sequence("Reposition");
        reposition.AddChild(new Leaf("IsTooClose", new IsTooClose(this, SafeDistance)));
        reposition.AddChild(new Leaf("MoveToBackline", new MoveToBackline(this, SafeDistance)));
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

        chaseWounded.AddChild(new Leaf("MoveTowardWounded", new MoveTowardWounded(this, HealRange)));

        Selector tryHealAfterMove = new Selector("TryHealAfterMove");
        Sequence healAfterMove = new Sequence("HealAfterMove");
        healAfterMove.AddChild(new Leaf("FindWoundedAllyInRange",
            new FindWoundedAlly(this, HealThreshold, HealRange, preferClosest: true)));

        healAfterMove.AddChild(new Leaf("HealTarget", new HealTarget(this, HealAmount)));
        tryHealAfterMove.AddChild(healAfterMove);

        tryHealAfterMove.AddChild(new Leaf("AlwaysSucceed", new AlwaysSucceed()));
        chaseWounded.AddChild(tryHealAfterMove);
        tryMoveUsefully.AddChild(chaseWounded);

        ProbabilitySelector combatOrFollow = new ProbabilitySelector("CombatOrFollow", 
        new List<float> { AttackWeight, RepositionWeight });

        Sequence tryAttack = new Sequence("TryAttack");
        tryAttack.AddChild(new Leaf("IsInRange", new IsInRange(this)));
        tryAttack.AddChild(new Leaf("Attack", new Attack(this)));
        combatOrFollow.AddChild(tryAttack);

        Selector followOrBackline = new Selector("FollowOrBackline");

        Sequence followTeam = new Sequence("FollowTeam");
        followTeam.AddChild(new Leaf("IsTooFarFromTeam", new IsTooFarFromTeam(this, HealRange)));
        followTeam.AddChild(new Leaf("MoveTowardTeam", new MoveTowardTeam(this, HealRange)));
        followOrBackline.AddChild(followTeam);

        followOrBackline.AddChild(new Leaf("MoveToBackline", new MoveToBackline(this, SafeDistance)));
        combatOrFollow.AddChild(followOrBackline);
        tryMoveUsefully.AddChild(combatOrFollow);

        activeTurn.AddChild(tryMoveUsefully);
        combat.AddChild(activeTurn);

        root.AddChild(combat);
        return root;
    }
}
