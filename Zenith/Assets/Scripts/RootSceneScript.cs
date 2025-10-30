using Unity.VisualScripting;
using UnityEngine;

public class RootSceneScript : MonoBehaviour
{    void Start()
    {
        if (GameManager.Instance == null)
        {
            this.AddComponent<GameManager>();
        }

        var existingPlayer = GetComponentInChildren<Player>();
        if (existingPlayer != null)
        {
            Destroy(existingPlayer.gameObject);
        }
        
        GameManager.Instance.Player.transform.SetParent(transform);
    }
}
