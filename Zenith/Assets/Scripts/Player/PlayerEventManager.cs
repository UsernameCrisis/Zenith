using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerEventManager : MonoBehaviour
{
    public void TakeHit()
    {
        StartCoroutine(GameManager.Instance.Player.GetComponent<PlayerMovement>().HitRoutine());
        PlayerHealthUI.Instance.UpdateUI();
    }

    public IEnumerator Die()
    {
        GameManager.Instance.Player.GetComponent<PlayerMovement>().canMove(false);
        GameManager.Instance.Player.GetComponent<PlayerData>().isDead = true;
        GameManager.Instance.Player.GetComponent<PlayerAnimationManager>().Animator.SetBool("isDead", true);

        GameManager.Instance.Player.GetComponent<PlayerMovement>().rb.linearVelocity = Vector3.zero;
        GameManager.Instance.Player.GetComponent<PlayerMovement>().rb.angularVelocity = Vector3.zero;
        GameManager.Instance.Player.GetComponent<PlayerMovement>().rb.constraints = RigidbodyConstraints.FreezeAll;
        yield return StartCoroutine(GameManager.Instance.Player.GetComponent<PlayerUIManager>().DeathVignette());

        Respawn();
    }
    

    private void Respawn()
    {
        GameManager.Instance.Player.transform.position = new Vector3(0, 0, 0);
        GameManager.Instance.Player.PlayerData.resetHP();
        //others
        SceneManager.LoadScene("SampleScene");
    }
}
