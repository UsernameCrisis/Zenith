using TMPro;
using Unity.VisualScripting;
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
    [SerializeField] private Player player;

    private ATB_ProgressBar playerATB;
    private ATB_ProgressBar companion1ATB;
    private ATB_ProgressBar companion2ATB;

    void Start()
    {
        EnemyMoveNameUI.SetActive(false);
        playerATB = PlayerUI.GetComponent<ATB_ProgressBar>();
        companion1ATB = Companion1UI.GetComponent<ATB_ProgressBar>();
        companion2ATB = Companion2UI.GetComponent<ATB_ProgressBar>();
    }


    void Update()
    {
        // playerATB.Add(1);
    }
}
