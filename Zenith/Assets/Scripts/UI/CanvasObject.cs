using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "CanvasObject", menuName = "Scriptable Objects/CanvasObject")]
public class CanvasObject : ScriptableObject
{
    public string name;
    public GameObject prefab;

    void Awake()
    {
        prefab.SetActive(false);
    }
}
