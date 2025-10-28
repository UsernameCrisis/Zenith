using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    [SerializeField] public Animator Animator;
    [SerializeField] private GameObject shadow;

    void Update()
    {
        Animator.SetInteger("stateRun", GameManager.Instance.Player.GetComponent<PlayerMovement>().IsMoving() ? (GameManager.Instance.Player.GetComponent<PlayerMovement>().IsSprinting() ? 2 : 1) : 0);
    }

    void DeathShadow() {
        if (shadow != null)
            shadow.SetActive(false);
    }
}
