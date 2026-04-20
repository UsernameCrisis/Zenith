
public interface ITurnActor
{
    void BeginTurn(GridData gridData, CharacterObject character);
    void EndTurn();
    bool IsPlayer { get; }
    bool IsTurnComplete();
}
