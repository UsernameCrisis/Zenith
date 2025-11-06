using UnityEngine;
using UnityEngine.UI;

public class SampleSceneHPController : MonoBehaviour
{
    public Image hpbar;
    void Update()
    {
        hpbar.fillAmount = GetComponentInParent<SampleEnemy>().GetHPPercentage();
    }
}
