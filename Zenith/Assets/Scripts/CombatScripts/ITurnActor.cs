using UnityEngine;

public interface ITurnActor
{
    void BeginTurn(GridData gridData);
    void EndTurn();
    bool IsPlayer { get; }
}
