using UnityEngine;

public class GUI_CombatAgent : MonoBehaviour
{
    [SerializeField] private CombatAgent2 _combatAgent;

    private GUIStyle _defaultStyle = new GUIStyle();
    private GUIStyle _positiveStyle = new GUIStyle();
    private GUIStyle _negativeStyle = new GUIStyle();
    void Start()
    {
        _defaultStyle.fontSize = 20;
        _defaultStyle.normal.textColor = Color.yellow;

        _positiveStyle.fontSize = 20;
        _positiveStyle.normal.textColor = Color.green;

        _negativeStyle.fontSize = 20;
        _negativeStyle.normal.textColor = Color.red;
    }

    private void OnGUI()
    {
        string debugEp = "Episode: " + _combatAgent.CurrEp + " - Step: " + _combatAgent.StepCount;
        string debugRew = "Reward: " + _combatAgent.CumulativeReward.ToString();

        GUIStyle rewardStyle = _combatAgent.CumulativeReward < 0 ? _negativeStyle : _positiveStyle;

        GUI.Label(new Rect(20, 20, 500, 30), debugEp, _defaultStyle);
        GUI.Label(new Rect(20, 60, 500, 30), debugRew, rewardStyle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
