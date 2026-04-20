using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatExecutor : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private bool isMoving = false;
    private bool isAttacking = false;
    private TurnManager turnManager;
    private MovementPreview movePreview;
    private Grid grid;
    public bool IsAttacking => isAttacking;
    public bool IsMoving => isMoving;

    void Start()
    {
        grid = GetComponentInChildren<Grid>();
        movePreview = GetComponentInChildren<MovementPreview>();
        turnManager = GetComponent<TurnManager>();
    }

    public void ExecuteMove(CharacterObject character, Vector3Int startPos, Vector3Int targetPos, GridData gridData)
    {
        if (character == null) return;

        List<Vector3Int> path = movePreview.GetMovementSystem().FindPathAStar(startPos, targetPos);
        
        if (path == null || path.Count == 0)
            return;
            
        if (path.Count > character.RemainingMoveRange)
        {
            Debug.Log("Not enough movement points!");
            return;
        }

        if (!gridData.CanPlaceObjectAt(targetPos))
            return;
        // print("before start coroutine or move");
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

        gridData.AttackObject(attackerPos, targetPos);
        attacker.DisableAttack();
        isAttacking = false;

        movePreview.ClearAll();
    }

    private IEnumerator WalkPath(List<Vector3Int> path, Vector3Int startPos, CharacterObject character, GridData gridData)
    {
        Transform charTransform = gridData.GetTileAt(startPos).PlacedGameObject.transform;

        isMoving = true;
        if (animator != null)
            animator.SetBool("isMoving", true);

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

                t += Time.deltaTime * speed;
                charTransform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }
        }

        Vector3Int finalPos = path[^1];

        gridData.MoveObject(startPos, finalPos, grid);
        character.UseMovement(path.Count);

        isMoving = false;

        if (animator != null)
            animator.SetBool("isMoving", false);

        movePreview.ClearAll();
    }

    private void Move(List<Vector3Int> path, Vector3Int startPos, CharacterObject character, GridData gridData)
    {
        Vector3Int finalPos = path[^1];

        // Move instantly in grid logic
        gridData.MoveObject(startPos, finalPos, grid);

        // Update world position immediately
        Transform charTransform = gridData.GetTileAt(finalPos).PlacedGameObject.transform;
        charTransform.position = grid.CellToWorld(finalPos);

        // Consume movement
        character.UseMovement(path.Count);

        movePreview.ClearAll();
    }

    public void ResetState()
    {
        isMoving = false;
        isAttacking = false;
        StopAllCoroutines();
    }
}
