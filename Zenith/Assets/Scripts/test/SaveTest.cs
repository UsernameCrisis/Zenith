using System.Collections.Generic;
using UnityEngine;

public class SaveTest : MonoBehaviour
{
    public Inventory inventory;
    void OnTriggerEnter(Collider other)
    {
        GameManager.Instance.SaveAndLoadScene("ruins");
    }
}
