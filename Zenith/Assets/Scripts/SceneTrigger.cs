using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    [SerializeField] string SceneName;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("hit");
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(SceneName);
        }
    }
}
