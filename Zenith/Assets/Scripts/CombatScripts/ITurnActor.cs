using UnityEngine;

public interface ITurnActor
{
    void BeginTurn(Vector3Int pos, GridData gridData);
    void EndTurn();
    bool IsPlayer { get; }
}
