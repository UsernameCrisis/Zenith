using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatExecutor : MonoBehaviour
{
    private bool isMoving = false;
    private bool isAttacking = false;
    private bool isHealing = false;
    private TurnManager turnManager;
    private MovementPreview movePreview;
    private Grid grid;
    public bool IsAttacking => isAttacking;
    public bool IsMoving => isMoving;
    public bool IsHealing => isHealing;

    void Start()
    {
        grid = GetComponentInChildren<Grid>();
        movePreview = GetComponentInChildren<MovementPreview>();
        turnManager = GetComponent<TurnManager>();
    }

    public void ExecuteMove(CharacterObject character, Vector3Int startPos, Vector3Int targetPos, GridData gridData)
    {
        if (character == null) return;

        List<Vector3Int> path = movePreview.FindPathAStar(startPos, targetPos);
        movePreview.ClearAll();
        
        if (path == null || path.Count == 0)
            return;
            
        if (path.Count > character.RemainingMoveRange)
        {
            Debug.Log("Not enough movement points!");
            return;
        }

        if (!gridData.CanPlaceObjectAt(targetPos))
            return;

        isMoving = true;

        if (turnManager.GetUseAnimation())
            StartCoroutine(WalkPath(path, startPos, character, gridData));
        else
            Move(path, startPos, character, gridData);
    }

    public void ExecuteAttack(CharacterObject attacker, Vector3Int attackerPos, Vector3Int targetPos, GridData gridData)
    {
        if (attacker == null) return;
        if (attackerPos == targetPos) return;

        isAttacking = true;

        if (turnManager.GetUseAnimation())
            StartCoroutine(AttackRoutine(attacker, attackerPos, targetPos, gridData));
        else
            InstantAttack(attacker, attackerPos, targetPos, gridData);

        movePreview.ClearAll();
    }

    public void ExecuteHeal(CharacterObject healer, Vector3Int healerPos, Vector3Int targetPos, GridData gridData, int healAmount)
    {
        if (healer == null) return;
        
        isHealing = true;

        if (turnManager.GetUseAnimation())
            StartCoroutine(HealRoutine(healer, healerPos, targetPos, gridData, healAmount));
        else
            InstantHeal(healer, healerPos, targetPos, gridData, healAmount);
    }

    private IEnumerator AttackRoutine(CharacterObject attacker, Vector3Int attackerPos, Vector3Int targetPos, GridData gridData)
    {

        var view = attacker.View;

        if (view == null)
        {
            Debug.LogWarning("No UnitView found!");
            yield break;
        }

        view.FaceTarget(attackerPos.x, targetPos.x);

        bool finished = false;

        void OnFinished() => finished = true;
        view.OnAttackFinished += OnFinished;

        view.PlayAttack(attackerPos, targetPos, gridData);

        attacker.DisableAttack();
        yield return new WaitUntil(() => finished);
        view.OnAttackFinished -= OnFinished;

        isAttacking = false;
    }

    private void InstantAttack(CharacterObject attacker, Vector3Int attackerPos, Vector3Int targetPos, GridData gridData)
    {
        isAttacking = true;
        gridData.AttackObject(attackerPos, targetPos);
        attacker.DisableAttack();
        isAttacking = false;
    }

    private IEnumerator HealRoutine(CharacterObject healer, Vector3Int healerPos, Vector3Int targetPos, GridData gridData, int healAmount)
    {
        var healerView = healer.View;

        if (healerView == null)
        {
            Debug.LogWarning($"[CombatExecutor] HealRoutine: no UnitView found on {healer.Name}! Falling back to instant heal.");
            InstantHeal(healer, healerPos, targetPos, gridData, healAmount);
            yield break;
        }

        CharacterObject target = gridData.GetTileAt(targetPos)?.PlacedObject as CharacterObject;
        if (target == null || target.HP <= 0)
        {
            Debug.LogWarning($"[CombatExecutor] HealRoutine: target at {targetPos} is null or dead.");
            isHealing = false;
            yield break;
        }

        healerView.FaceTarget(healerPos.x, targetPos.x);

        target.Heal(healAmount);

        bool finished = false;
        void OnFinished() => finished = true;
        healerView.OnHealFinished += OnFinished;

        healerView.PlayHeal(target);

        yield return new WaitUntil(() => finished);
        healerView.OnHealFinished -= OnFinished;

        isHealing = false;
    }

    private void InstantHeal(CharacterObject healer, Vector3Int healerPos, Vector3Int targetPos, GridData gridData, int healAmount)
    {
        CharacterObject target = gridData.GetTileAt(targetPos)?.PlacedObject as CharacterObject;
        if (target != null && target.HP > 0)
            target.Heal(healAmount);

        isHealing = false;
    }

    private IEnumerator WalkPath(List<Vector3Int> path, Vector3Int startPos, CharacterObject character, GridData gridData)
    {
        Transform charTransform = gridData.GetTileAt(startPos).PlacedGameObject.transform;

        character.View?.SetMoving(isMoving);

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 start = charTransform.position;
            Vector3 end = grid.CellToWorld(path[i]);
            

            float t = 0f;
            float speed = 2f;

            while (t < 1f)
            {
                if (charTransform == null)
                    yield break;

                Vector3 direction = end - charTransform.position;
                character.View.SetFacing(direction.x);

                t += Time.deltaTime * speed;
                charTransform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }

        Vector3Int finalPos = path[^1];

        gridData.MoveObject(startPos, finalPos, grid);
        character.UseMovement(path.Count);

        isMoving = false;

        character.View?.SetMoving(isMoving);

        movePreview.ClearAll();
    }

    private void Move(List<Vector3Int> path, Vector3Int startPos, CharacterObject character, GridData gridData)
    {
        Vector3Int finalPos = path[^1];

        gridData.MoveObject(startPos, finalPos, grid);

        Transform charTransform = gridData.GetTileAt(finalPos).PlacedGameObject.transform;
        charTransform.position = grid.CellToWorld(finalPos);

        character.UseMovement(path.Count);
        movePreview.ClearAll();
        isMoving = false;
    }

    public void ResetState()
    {
        isMoving = false;
        isAttacking = false;
        StopAllCoroutines();
    }
}
