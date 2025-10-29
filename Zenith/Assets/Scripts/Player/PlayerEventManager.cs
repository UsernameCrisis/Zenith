using UnityEngine;

public class PlayerEventManager : MonoBehaviour
{
    public void TakeHit()
    {
        StartCoroutine(GameManager.Instance.Player.GetComponent<PlayerMovement>().HitRoutine());
        PlayerHealthUI.Instance.UpdateUI();
    }
    
    public void Die()
    {
        GameManager.Instance.Player.GetComponent<PlayerMovement>().canMove(false);
        GameManager.Instance.Player.GetComponent<PlayerData>().isDead = true;
        GameManager.Instance.Player.GetComponent<PlayerAnimationManager>().Animator.SetBool("isDead", true);

        GameManager.Instance.Player.GetComponent<PlayerMovement>().rb.linearVelocity = Vector3.zero;
        GameManager.Instance.Player.GetComponent<PlayerMovement>().rb.angularVelocity = Vector3.zero;
        GameManager.Instance.Player.GetComponent<PlayerMovement>().rb.constraints = RigidbodyConstraints.FreezeAll;
        StartCoroutine(GameManager.Instance.Player.GetComponent<PlayerUIManager>().DeathVignette());
    }
}
