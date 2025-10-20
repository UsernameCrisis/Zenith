using UnityEngine;

public abstract class SceneScript : MonoBehaviour
{
    Player _player;
    GameObject playerObject;

    public void Initialize(Player player)
    {
        _player = player;
        
        playerObject = GameObject.FindWithTag("Player");
        playerObject = _player.gameObject;
    }
}
