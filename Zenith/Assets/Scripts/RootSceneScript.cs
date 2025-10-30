using Unity.VisualScripting;
using UnityEngine;

public class RootSceneScript : MonoBehaviour
{    
    void Awake()
    {
        if (GameManager.Instance == null)
        {
            transform.parent.AddComponent<GameManager>();
        }
    }
}
