using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneCollideTrigger : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
