using UnityEngine.UI;
using UnityEngine;

public class ATB_ProgressBar : ATB_System
{
    [SerializeField] private Image innerBar;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        setValue(currentValue);
    }

    void setValue(int value)
    {
        if (innerBar == null) return;
        float fillAmount = (float)value / maxValue;
        innerBar.fillAmount = fillAmount;
    }

    public void Add(int amount)
    {
        if (currentValue < maxValue)
        {
            currentValue += amount;
        }
    }
}
