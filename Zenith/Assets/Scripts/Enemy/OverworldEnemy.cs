using UnityEngine;

public class OverworldEnemy : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            GameManager.Instance.SaveAndLoadScene("Combat_test1");
    }
}
