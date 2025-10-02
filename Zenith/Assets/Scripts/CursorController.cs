using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class CursorController : MonoBehaviour
{
    [SerializeField] private Image cursor;
    [SerializeField] private Player player;
    [SerializeField] private Companion companion1;
    [SerializeField] private Companion companion2;
    [SerializeField] private GameObject playerATB;
    [SerializeField] private CombatManager combatManager;
    private STATE state = STATE.ACT;
    private int _actCounter = 0;
    private int _actSubmenuCounter = 0;
    private int _targetCounter = 0;
    private Stack<STATE> stateStack = new Stack<STATE>();
    private enum STATE
    {
        ACT,
        ACT_SUBMENU,
        TARGET
    }

    void Start()
    {
        SetEnum(STATE.ACT);
    }

    void Update()
    {
        switch (state)
        {
            case STATE.ACT:
                HandleActState();
                break;
            case STATE.ACT_SUBMENU:
                HandleActSubmenuState();
                break;
            case STATE.TARGET:
                HandleTargetState();
                break;
        }
    }

    private void SetEnum(STATE state)
    {
        this.state = state;
        switch (state)
        {
            case STATE.ACT:
                stateStack.Push(STATE.ACT);
                cursor.transform.position = new Vector3(50, 400, 0);
                break;
            case STATE.ACT_SUBMENU:
                stateStack.Push(STATE.ACT_SUBMENU);
                cursor.transform.position = new Vector3(700, 200, 0);
                break;
            case STATE.TARGET:
                stateStack.Push(STATE.TARGET);
                cursor.transform.position = new Vector3(1400, 420, 0);
                break;
        }
    }

    private void HandleTargetState()
    {
        if (Keyboard.current[Key.LeftArrow].wasPressedThisFrame || Keyboard.current[Key.A].wasPressedThisFrame)
        {
            if (_targetCounter > 0)
            {
                _targetCounter--;
                if (_targetCounter == 0) //left
                    cursor.transform.position = new Vector3(1400, 420, 0);
                else if (_targetCounter == 1) //middle
                    cursor.transform.position = new Vector3(1550, 400, 0);
            }
        }
        if (Keyboard.current[Key.RightArrow].wasPressedThisFrame || Keyboard.current[Key.D].wasPressedThisFrame)
        {
            if (_targetCounter < 2)
            {
                _targetCounter++;
                if (_targetCounter == 1) //middle
                    cursor.transform.position = new Vector3(1550, 400, 0);
                else if (_targetCounter == 2) //right
                    cursor.transform.position = new Vector3(1800, 340, 0);
            }
        }

        if (Keyboard.current[Key.X].wasPressedThisFrame || Keyboard.current[Key.Backspace].wasPressedThisFrame)
        {
            stateStack.Pop();
            SetEnum(stateStack.Pop());
        }


        if (Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.Space].wasPressedThisFrame)
        {
            stateStack.Clear();
            SetEnum(STATE.ACT);
            
            if (_targetCounter == 0) //left
            {
                combatManager.enqueueMove(player.getMoves()[0], player, combatManager.getEnemy1());
            }
            else if (_targetCounter == 1) //middle
            {
                combatManager.enqueueMove(player.getMoves()[0], player, combatManager.getEnemy2());
            }
            else if (_targetCounter == 2) //right
            {
                combatManager.enqueueMove(player.getMoves()[0], player, combatManager.getEnemy3());
            }
            _targetCounter = 0;
            playerATB.GetComponent<ATB_ProgressBar>().resetPlayer();
        }
    }

    private void HandleActSubmenuState()
    {
        throw new NotImplementedException();
    }

    private void HandleActState()
    {
        if (Keyboard.current[Key.UpArrow].wasPressedThisFrame || Keyboard.current[Key.W].wasPressedThisFrame)
        {
            if (_actCounter > 0)
            {
                _actCounter--;
                cursor.transform.position += new Vector3(0, 105, 0);
            }
        }
        if (Keyboard.current[Key.DownArrow].wasPressedThisFrame || Keyboard.current[Key.S].wasPressedThisFrame)
        {
            if (_actCounter < 3)
            {
                _actCounter++;
                cursor.transform.position += new Vector3(0, -105, 0);
            }
        }

        if (Keyboard.current[Key.Enter].wasPressedThisFrame || Keyboard.current[Key.Space].wasPressedThisFrame)
        {
            switch (_actCounter)
            {
                case 0: //attack
                    SetEnum(STATE.TARGET);
                    break;
                case 1: //skill
                    Debug.Log("NYI");
                    break;
                case 2: //magic
                    Debug.Log("NYI");
                    break;
                case 3: //item
                    Debug.Log("NYI");
                    break;
            }
        }
    }

    public void visible(bool b)
    {
        cursor.enabled = b;
    }
}
