using System;
using System.Collections;
using UnityEngine;

public class UnitView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool defaultFacingRight = true;
    [SerializeField] private GameObject healEffectPrefab;

    private GridData gridData;
    private Vector3Int attackerPos;
    private Vector3Int targetPos;
    private CharacterObject boundCharacter;
    private TurnManager turnManager;
    private CharacterObject healTargetCharacter;

    public event Action OnAttackFinished;
    public event Action OnDeathFinished;
    public event Action OnHealFinished;

    public void Bind(CharacterObject character, TurnManager turnManager)
    {
        boundCharacter = character;
        this.turnManager = turnManager;

        character.OnTakenDamage += HandleTakenDamage;
        character.OnDied += HandleDeath;
    }

    public void SetMoving(bool isMoving)
    {
        animator.SetBool("isMoving", isMoving);
    }

    public void SetFacing(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f) return;

        bool shouldFaceRight = directionX > 0;
        bool flip = shouldFaceRight != defaultFacingRight;
        spriteRenderer.flipX = flip;
    }

    public void PlayAttack(Vector3Int attackerPos, Vector3Int targetPos, GridData gridData)
    {
        this.gridData = gridData;
        this.attackerPos = attackerPos;
        this.targetPos = targetPos;
        animator.SetTrigger("attack");
    }

    public void OnAttackAnimationEnd()
    {
        OnAttackFinished?.Invoke();
    }

    public void OnAttackHit()
    {
        gridData?.AttackObject(attackerPos, targetPos);
    }

    public void PlayHit()
    {
        if (!turnManager.GetUseAnimation()) return;
        animator.SetTrigger("hit");
    }

    private void HandleTakenDamage(int damage)
    {
        PlayHit();
    }

    private void HandleDeath(CharacterObject character)
    {
        PlayDeath();
    }

    public void PlayDeath()
    {
        if (!turnManager.GetUseAnimation()) return;
        animator.SetTrigger("death");
    }

    public void OnDeathAnimationEnd()
    {
        OnDeathFinished?.Invoke();
    }

    public void PlayHeal(CharacterObject target)
    {
        healTargetCharacter = target;
        animator.SetTrigger("heal");
    }

    public void OnHealHit()
    {
        if (healTargetCharacter == null) return;

        UnitView targetView = healTargetCharacter.View;
        if (targetView != null)
            targetView.PlayHealReceived();
    }

    public void OnHealAnimationEnd()
    {
        OnHealFinished?.Invoke();
    }

    public void PlayHealReceived()
    {
        if (!turnManager.GetUseAnimation()) return;

        if (healEffectPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, 0.5f, 0f);
            Instantiate(healEffectPrefab, spawnPos, Quaternion.identity);
        }
    }

    public void FaceTarget(float attackerXPos, float targetXPos)
    {
        float directionX = targetXPos - attackerXPos;

        if (Mathf.Abs(directionX) > 0.01f)
        {
            SetFacing(directionX);
        }
    }

    private void OnDestroy()
    {
        if (boundCharacter != null)
        {
            boundCharacter.OnTakenDamage -= HandleTakenDamage;
            boundCharacter.OnDied -= HandleDeath;
        }
    }
}
