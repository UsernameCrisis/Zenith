using UnityEngine.UI;
using UnityEngine;

public class ATB_ProgressBar : MonoBehaviour
{
    [SerializeField] private Image innerBar;
    public int maxValue = 100;
    public int currentValue = 0;
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
}
