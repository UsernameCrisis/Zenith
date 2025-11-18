using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DialogBox : MonoBehaviour
{
    public TMP_Text text;
    private List<string> _currentDialog;
    private int index = 0;
    void Start()
    {
        gameObject.SetActive(false);
    }
    void Update()
    {
        if (_currentDialog == null) return;
        if (InputSystem.actions.FindAction("Jump").WasPressedThisFrame())
        {
            if (_currentDialog.Count == index){
                gameObject.SetActive(false);
                index = 0;
                return;
            }
            text.text = _currentDialog[index];
            index++;
        }
    }
    
    public void PlayDialog(Dialog dialog)
    {
        if (!gameObject.activeInHierarchy) gameObject.SetActive(true);
        _currentDialog = dialog.dialog;
        text.text = dialog.dialog[index];
        index++;
    }
}
