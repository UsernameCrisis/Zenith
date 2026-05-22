public class SlimeAgent : UnitAgentBase
{
    protected override float DamageTakenBase => 0.005f;
    protected override float DamageTakenPowerScale => 0.01f;
    protected override float InRangeBonus => 0.004f;
}
