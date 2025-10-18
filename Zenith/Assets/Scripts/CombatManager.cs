using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private CursorController cursorController;
    [SerializeField] private GameObject MoveNameUI;
    [SerializeField] private TMP_Text EnemyMoveNameText;

    [SerializeField] private GameObject PlayerUI;
    [SerializeField] private GameObject Companion1UI;
    [SerializeField] private GameObject Companion2UI;
    [SerializeField] private Player player;
    [SerializeField] private GameObject companion1Object;
    [SerializeField] private GameObject companion2Object;
    [SerializeField] private Enemy enemy1Object;
    [SerializeField] private Enemy enemy2Object;
    [SerializeField] private Enemy enemy3Object;

    [SerializeField] private GameObject ActMenu;

    private ATB_ProgressBar playerATB;
    private ATB_ProgressBar companion1ATB;
    private ATB_ProgressBar companion2ATB;
    private int playerHP;
    private bool showAct = false;
    private bool canGoNextMove = true;
    //move, from, target
    private Queue<(Move, Character, Character)> moveQueue = new Queue<(Move, Character, Character)>();

    CombatManager(List<Enemy> enemies) {
        if (enemies[0] != null)
        {
            enemy1Object = enemies[0];

            if (enemies[1] != null)
            {
                enemy2Object = enemies[1];

                if (enemies[2] != null)
                {
                    enemy3Object = enemies[2];
                }
            }
        }
    }

    void Start()
    {
        MoveNameUI.SetActive(false);
        disableAct();
        playerATB = PlayerUI.GetComponentInChildren<ATB_ProgressBar>();
        player.canMove(false);
        List<Character> companions = player.GetCompanions();

        playerHP = player.GetHP();

        //temp 
        companions = new List<Character>();

        //show companion only if there is a companion
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

    }


    void Update()
    {
    }

    void FixedUpdate()
    {
        updatePlayerHP();
        updateATB();

        if (moveQueue.Count > 0 && canGoNextMove)
        {
            executeMove(moveQueue.Peek());
            StartCoroutine(hideMoveNameUI());
            moveQueue.Dequeue();
            canGoNextMove = false;
        }
    }

    private void executeMove((Move, Character, Character) value)
    {
        MoveNameUI.SetActive(true);
        EnemyMoveNameText.SetText(value.Item1.getName());
        value.Item3.takeDamage(value.Item1.getDamage());
    }

    void updateATB()
    {
        playerATB.Add(player.getSpeed() / 8);
        if (playerATB.canAct() && !showAct)
        {
            showAct = true;
            enableAct();
        }


        enemy1Object.ATBAdd(enemy1Object.getSpeed() / 8);
        enemy2Object.ATBAdd(enemy2Object.getSpeed() / 8);
        enemy3Object.ATBAdd(enemy3Object.getSpeed() / 8);

        if (enemy1Object.getATB() >= 100)
        {
            moveQueue.Enqueue((enemy1Object.bestMove(), enemy1Object, enemy1Object.bestTarget()));
            enemy1Object.resetATB();
        }
        if (enemy2Object.getATB() >= 100)
        {
            moveQueue.Enqueue((enemy2Object.bestMove(), enemy2Object, enemy2Object.bestTarget()));
            enemy2Object.resetATB();
        }
        if (enemy3Object.getATB() >= 100)
        {
            moveQueue.Enqueue((enemy3Object.bestMove(), enemy3Object, enemy3Object.bestTarget()));
            enemy3Object.resetATB();
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
    private IEnumerator hideMoveNameUI()
    {
        yield return new WaitForSeconds((float)1.5);
        MoveNameUI.SetActive(false);
        canGoNextMove = true;
    }
    public void enqueueMove(Move move, Character from, Character target)
    {
        moveQueue.Enqueue((move, from, target));
    }

    public Enemy getEnemy1()
    {
        return enemy1Object;
    }
    public Enemy getEnemy2()
    {
        return enemy2Object;
    }
    public Enemy getEnemy3()
    {
        return enemy3Object;
    }
}
