using UnityEngine;
using UnityEngine.SceneManagement;

public class OverworldEnemy : MonoBehaviour
{
    // private bool loaded = false;
    void OnTriggerEnter(Collider other)
    {
        // if (loaded == false && other.CompareTag("Player")){
        //     loaded = true;
        //     HideScene(SceneManager.GetActiveScene());
        //     SceneManager.LoadScene("Combat_test1", LoadSceneMode.Additive);
        //     GameManager.Instance.CurrentEnemy = this.gameObject;
        // }
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.SaveAndLoadScene("Combat_test1");
        }
    }

    // void HideScene(Scene scene)
    // {
    //     foreach (GameObject root in scene.GetRootGameObjects())
    //     {
    //         root.SetActive(false);
    //     }
    // }
}
