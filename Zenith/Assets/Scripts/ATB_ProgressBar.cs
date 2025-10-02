using UnityEngine.UI;
using UnityEngine;
using System;

//harusnya ini ATB yg keliatan (player, companion)
public class ATB_ProgressBar : ATB_System
{
    [SerializeField] private Image innerBar;
    [SerializeField] private CombatManager combatManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        setValue(currentValue);
    }

    void setValue(float value)
    {
        if (innerBar == null) return;
        float fillAmount = (float)value / maxValue;
        innerBar.fillAmount = fillAmount;
    }

    public void Add(float amount)
    {
        if (currentValue < maxValue)
        {
            currentValue += amount;
        }
    }

    public bool canAct()
    {
        return currentValue >= maxValue;
    }
    public void reset()
    {
        currentValue = 0;
    }
    public void resetPlayer()
    {
        currentValue = 0;
        combatManager.disableAct();
    }

    public float getProgress()
    {
        return currentValue;
    }
}
