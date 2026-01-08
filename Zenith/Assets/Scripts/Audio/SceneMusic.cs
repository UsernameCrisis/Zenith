using UnityEngine;

public class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip sceneMusic;

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            print("playing music");
            AudioManager.Instance.PlayMusic(sceneMusic);
        }
           
    }
}
