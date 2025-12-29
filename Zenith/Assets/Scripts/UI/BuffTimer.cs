using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuffTimer : MonoBehaviour
{
    public TMP_Text timer;
    public Image image;
    private int seconds;

    public void SetVariables(Sprite sprite, int timerseconds)
    {
        image.sprite = sprite;
        seconds = timerseconds;
        
        StartCoroutine(StartCountdown());
    }

    public IEnumerator StartCountdown()
    {
        while (seconds > 0)
        {
            timer.text = TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
            yield return new WaitForSeconds(1);
            seconds--;
        }
    }
}
