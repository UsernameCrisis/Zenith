using UnityEngine;

public class OverworldAnchor : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.SetOverworldRoot(this.gameObject);
    }
}