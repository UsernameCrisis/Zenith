using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip sceneMusic;
    [SerializeField] private float transitionFadeTime = 1f;

    void Start()
    {
        if (AudioManager.Instance != null && sceneMusic != null)
        {
            print("playing music");
            AudioManager.Instance.PushMusic(sceneMusic, transitionFadeTime);
        }
    }

    void OnDestroy()
    {
        if (AudioManager.Instance != null && sceneMusic != null)
        {
            AudioManager.Instance.RemoveMusic(sceneMusic, transitionFadeTime);
        }
    }
}
