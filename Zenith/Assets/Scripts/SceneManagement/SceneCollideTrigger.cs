using UnityEngine;

public class SceneCollideTrigger : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LoadingScreenManager.Load(sceneToLoad);
        }
    }
}
