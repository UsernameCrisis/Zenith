using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogBox : MonoBehaviour
{
    public TMP_Text text;
    private List<String> _currentDialog;
    private int index = 0;

    void Update()
    {
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
    
    public void PlayDialog(List<String> dialog)
    {
        Debug.Log("2");
        _currentDialog = dialog;
        text.text = dialog[index];
        index++;
    }
}
