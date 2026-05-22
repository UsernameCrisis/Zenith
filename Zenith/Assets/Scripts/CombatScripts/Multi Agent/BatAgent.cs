public class BatAgent : UnitAgentBase
{
    protected override float DamageDealtBase => 0.015f;
    protected override float DamageDealtPowerScale => 0.03f;
    protected override float InRangeBonus => 0.005f;
    protected override float UnitDeathPenalty => 0.15f;
}
