using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Policies;

public class AgentTestingController : MonoBehaviour
{
    [Header("Combat Infrastructure")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BattleResultHandler battleResultHandler;
    [SerializeField] private CombatAgent2 combatAgent;
    [SerializeField] private PopulateMap populateMap;
    [SerializeField] private StatOverridePanel statPanel;

    [Header("Selection Panel UI")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private Transform toggleContainer;
    [SerializeField] private Toggle togglePrefab;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI resultBannerText;
    [SerializeField] private Toggle humanPlayerToggle;

    [Header("Toggle Colors")]
    [SerializeField] private Color toggleSelectedColor = Color.green;
    [SerializeField] private Color toggleNormalColor   = Color.white;

    [Header("Agent Configurations")]
    [SerializeField] private List<AgentModeConfig> modes = new();


    private int selectedModeIndex = 0;
    private ToggleGroup toggleGroup;
    private List<Toggle> spawnedToggles = new();
    private bool combatIsRunning = false;
    private bool useHumanPlayer = false;

    void Awake()
    {
        battleResultHandler.SetAgentTestingMode(true);

        battleResultHandler.OnCombatFinished += HandleCombatFinished;

        startButton.onClick.AddListener(OnStartPressed);
    }

    void Start()
    {
        BuildToggleList();
        if (resultBannerText != null)
            resultBannerText.gameObject.SetActive(false);
        ShowSelectionPanel(resultText: null);
    }

    void OnDestroy()
    {
        battleResultHandler.OnCombatFinished -= HandleCombatFinished;
    }

    private void BuildToggleList()
    {
        toggleGroup = toggleContainer.gameObject.GetComponent<ToggleGroup>();
        if (toggleGroup == null)
            toggleGroup = toggleContainer.gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = false;

        for (int i = 0; i < modes.Count; i++)
        {
            Toggle t = Instantiate(togglePrefab, toggleContainer);
            t.group = toggleGroup;

            var label = t.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = modes[i].modeName;

            int capturedIndex = i;
            t.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    selectedModeIndex = capturedIndex;
                    RefreshToggleColors();
                } 
            });

            spawnedToggles.Add(t);
        }

        if (spawnedToggles.Count > 0)
        {
            spawnedToggles[0].isOn = true;
            RefreshToggleColors();
        }
            

        if (humanPlayerToggle != null)
        {
            humanPlayerToggle.isOn = false;
            humanPlayerToggle.onValueChanged.AddListener(_ => RefreshHumanPlayerToggleColor());
            RefreshHumanPlayerToggleColor();
        }
            
    }

    private void ShowSelectionPanel(string resultText)
    {
        combatIsRunning = false;
        selectionPanel.SetActive(true);

        if (resultBannerText != null)
        {
            resultBannerText.gameObject.SetActive(resultText != null);
            if (resultText != null)
                resultBannerText.text = resultText;
        }
    }

    private void HideSelectionPanel()
    {
        selectionPanel.SetActive(false);
        combatIsRunning = true;
    }

    private void RefreshToggleColors()
    {
        for (int i = 0; i < spawnedToggles.Count; i++)
        {
            TextMeshProUGUI label = spawnedToggles[i].GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.color = (i == selectedModeIndex) ? toggleSelectedColor : toggleNormalColor;
        }
    }

    private void RefreshHumanPlayerToggleColor()
    {
        if (humanPlayerToggle == null) return;
    
        TextMeshProUGUI label = humanPlayerToggle.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.color = humanPlayerToggle.isOn ? toggleSelectedColor : toggleNormalColor;
    }

    private void OnStartPressed()
    {
        if (combatIsRunning) return;
        if (modes.Count == 0)
        {
            Debug.LogError("[AgentTestingController] No modes configured!");
            return;
        }

        useHumanPlayer = humanPlayerToggle != null && humanPlayerToggle.isOn;

        populateMap.SetStatOverrides(statPanel.GetOverrides());

        AgentModeConfig config = modes[selectedModeIndex];
        ApplyConfig(config);

        HideSelectionPanel();

        turnManager.ResetEnv();
    }

    private void ApplyConfig(AgentModeConfig config)
    {
        CombatControlMode resolvedMode = config.controlMode;

        if (useHumanPlayer)
        {
            resolvedMode = config.controlMode switch
            {
                CombatControlMode.BehaviorTree => CombatControlMode.Player,
                CombatControlMode.MLAgent      => CombatControlMode.PlayerVsAgent,
                CombatControlMode.MultiAgent   => CombatControlMode.PlayerVsAgent,
                _                              => config.controlMode
            };
        }

        battleResultHandler.SetControlMode(resolvedMode);
        turnManager.SetControlMode(resolvedMode);

        bool needsCombatAgent2 = resolvedMode == CombatControlMode.MLAgent
                                || resolvedMode == CombatControlMode.PlayerVsAgent
                                || resolvedMode == CombatControlMode.BTRecording;

        combatAgent.gameObject.SetActive(needsCombatAgent2);

        if (needsCombatAgent2 && config.combatAgent2Model != null)
        {
            BehaviorParameters bp = combatAgent.GetComponent<BehaviorParameters>();
            if (bp != null)
                bp.Model = config.combatAgent2Model;
            else
                Debug.LogWarning("[AgentTestingController] CombatAgent2 has no BehaviorParameters!");
        }

        Debug.Log($"[AgentTestingController] Applied config: {config.modeName} | " +
                $"base mode={config.controlMode} | resolved mode={resolvedMode} | " +
                $"human player={useHumanPlayer} | agent active={needsCombatAgent2}");
    }

    private void HandleCombatFinished(bool monsterWon)
    {
        string result = monsterWon ? "Monsters Won!" : "Player Team Won!";
        Debug.Log($"[AgentTestingController] Combat finished — {result}");

        StartCoroutine(ShowResultNextFrame(result));
    }

    private IEnumerator ShowResultNextFrame(string result)
    {
        yield return null;
        ShowSelectionPanel(resultText: result);
    }
}
