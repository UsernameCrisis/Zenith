using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private CursorController cursorController;
    [SerializeField] private GameObject EnemyMoveNameUI;
    [SerializeField] private TMP_Text EnemyMoveNameText;

    [SerializeField] private GameObject PlayerUI;
    [SerializeField] private GameObject Companion1UI;
    [SerializeField] private GameObject Companion2UI;

    void Start()
    {
        EnemyMoveNameUI.SetActive(false);
    }


    void Update()
    {
        
    }
}
