using System.Collections.Generic;
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
    [SerializeField] private GameObject companion1Object;
    [SerializeField] private GameObject companion2Object;
    [SerializeField] private GameObject enemy1Object;
    [SerializeField] private GameObject enemy2Object;
    [SerializeField] private GameObject enemy3Object;

    [SerializeField] private GameObject ActMenu;

    private ATB_ProgressBar playerATB;
    private ATB_ProgressBar companion1ATB;
    private ATB_ProgressBar companion2ATB;
    private int playerHP;
    private bool showAct = false;

    void Start()
    {
        EnemyMoveNameUI.SetActive(false);
        disableAct();
        playerATB = PlayerUI.GetComponentInChildren<ATB_ProgressBar>();
        player.canMove(false);
        List<Character> companions = player.GetCompanions();

        playerHP = player.GetHP();
        
        //temp 
        companions = new List<Character>();

        if (companions.Count > 0)
        {
            if (companions[0] == null)
            {
                enableCompanion1(false);
            }
            else
            {
                companion1Object = companions[0].gameObject;
            }

            if (companions.Count > 1)
            {
                if (companions[1] == null)
                {
                    enableCompanion2(false);
                }
                else
                {
                    companion2Object = companions[1].gameObject;
                }
            }
            else
            {
                companion2Object.SetActive(false);
            }
        }
        else
        {
            enableCompanion1(false);
            enableCompanion2(false);
        }

        EnemyMoveNameUI.SetActive(false);
    }


    void Update()
    {
    }

    void FixedUpdate()
    {
        updatePlayerHP();
        updateATB();
    }
    void updateATB()
    {
        playerATB.Add(player.getSpeed() / 8);
        if (playerATB.canAct() && !showAct)
        {
            showAct = true;
            enableAct();
        }
    }

    void enableCompanion1(bool b)
    {
        Companion1UI.SetActive(b);
        companion1Object.SetActive(b);
    }
    void enableCompanion2(bool b)
    {
        Companion2UI.SetActive(b);
        companion2Object.SetActive(b);
    }
    void updatePlayerHP()
    {
        PlayerUI.GetComponent<CharacterCombatData>().setHP(player.GetHP());
    }
    public void disableAct()
    {
        showAct = false;
        cursorController.visible(false);
        ActMenu.SetActive(false);
    }
    public void enableAct()
    {
        cursorController.visible(true);
        ActMenu.SetActive(true);
    }
}
