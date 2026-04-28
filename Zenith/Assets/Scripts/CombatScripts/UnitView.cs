using UnityEngine;

public class UnitView : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float lastFacing = 1f;

    public void SetMoving(bool isMoving)
    {
        animator.SetBool("isMoving", isMoving);
    }

    public void SetFacing(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.01f) return;

        lastFacing = Mathf.Sign(directionX);
        spriteRenderer.flipX = lastFacing < 0;
    }
}
