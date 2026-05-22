public class FloatingEyeAgent : UnitAgentBase
{
    protected override float DamageTakenBase       => 0.015f;
    protected override float DamageTakenPowerScale => 0.03f;
    protected override float InRangeBonus => 0.003f;
}