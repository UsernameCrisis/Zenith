using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleResultHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private TurnOrderUI turnOrderUI;
    [SerializeField] private CombatAgent2 combatAgent;
    [SerializeField] private CombatOverMenu combatOverUI;

    [Header("Settings")]
    [SerializeField] private CombatControlMode controlMode = CombatControlMode.Player;
    [SerializeField] private int currentAgentTeam = 2;
    [SerializeField] private bool showTurnOrderUI = true;
    [SerializeField] private bool isRecordingMode = false;
    [SerializeField] private int maxTurn = 50;

    [Header("Test Recording")]
    [SerializeField] private TestRecorder testRecorder;

    public int MaxTurn => maxTurn;
    public void SetControlMode(CombatControlMode mode)
    {
        controlMode = mode;
    }
    public GridData GridData { private get; set; }
    public List<string> DefeatedEnemyNames { get; private set; } = new();
    public System.Action<CharacterObject> OnCharacterDied;
    public System.Action<GridData> OnEnvReset;
    private PopulateMap mapPopulator;
    private TurnManager turnManager;
    void Awake()
    {
        turnManager  = GetComponent<TurnManager>();
        bool agentNeeded = controlMode == CombatControlMode.MLAgent || 
                            controlMode == CombatControlMode.PlayerVsAgent ||
                            controlMode == CombatControlMode.BTRecording;
        combatAgent.gameObject.SetActive(agentNeeded);
    }
    void Start()
    {
        mapPopulator = GetComponentInChildren<PopulateMap>();
    }

    public void SubscribeCharacterDeath(CharacterObject character)
    {
        character.OnDied += HandleCharacterDied;
    }

    public void UnsubscribeCharacterDeath(CharacterObject character)
    {
        character.OnDied -= HandleCharacterDied;
    }

    private void HandleCharacterDied(CharacterObject character)
    {
        if (controlMode == CombatControlMode.MLAgent ||
            controlMode == CombatControlMode.PlayerVsAgent)
        {
            combatAgent.OnUnitKilled(character);
        }

        if (controlMode == CombatControlMode.MultiAgent)
        {
            foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
                agent.OnUnitKilled(character);
        }

        OnCharacterDied?.Invoke(character);
    }

    public bool CheckBattleEnd()
    {
        int aliveAgentTeam  = GridData.GetUnitsByTeam(currentAgentTeam).Count;
        int aliveOpponentTeam = GridData.GetUnitsByTeam(currentAgentTeam == 1 ? 2 : 1).Count;

        if (aliveOpponentTeam == 0) return HandleVictory();
        if (aliveAgentTeam == 0) return HandleDefeat();

        return false;
    }

    private bool HandleVictory()
    {
        if (controlMode == CombatControlMode.MLAgent || 
        controlMode == CombatControlMode.BTRecording)
        {
            combatAgent.OnVictory();
            StartCoroutine(EndEpisodeNextFrame());
            return true;
        }

        if (controlMode == CombatControlMode.PlayerVsAgent)
        {
            combatAgent.OnVictory();
            ShowPlayerDefeatUI();
            return true;
        }

        if (controlMode == CombatControlMode.Demonstration)
        {
            gridSelect.ExitCharacter();
            GetComponent<TurnManager>().StopTurnLoop();
            StartCoroutine(ResetAfterDelay());
            return true;
        }

        if (controlMode == CombatControlMode.MultiAgent)
        {
            foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
                agent.OnVictory();
            StartCoroutine(EndEpisodeNextFrameMultiAgent());
            return true;
        }

        // SceneManager.UnloadSceneAsync("Combat_test1");
        // ShowScene(SceneManager.GetActiveScene());
        // Destroy(GameManager.Instance.CurrentEnemy);
        // GameManager.Instance.CurrentEnemy = null;

        if (controlMode == CombatControlMode.BehaviorTree)
        {
            FinalizeTestEpisode(monsterWon: true); // if current agent team = 2, monster won
            StartCoroutine(BehaviorTreeResetAfterDelay());
            return true;
        }

        // Player mode, pass defeated enemy names up to GameManager.
        gridSelect.ExitCharacter();
        turnOrderUI.gameObject.SetActive(false);

        foreach (var name in DefeatedEnemyNames)
            GameManager.Instance.defeatedEnemyNames.Add(name);

        Debug.Log($"Enemies defeated: {string.Join(", ", GameManager.Instance.defeatedEnemyNames)}");

        combatOverUI.OnButtonSelected += HandleCombatOverButton;
        combatOverUI.Show(true);
        return true;
    }

    private bool HandleDefeat()
    {
        if (controlMode == CombatControlMode.MLAgent || 
        controlMode == CombatControlMode.BTRecording)
        {
            combatAgent.OnDefeat();
            StartCoroutine(EndEpisodeNextFrame());
            return true;
        }

        if (controlMode == CombatControlMode.PlayerVsAgent)
        {
            combatAgent.OnDefeat();
            ShowPlayerVictoryUI();
            return true;
        }

        if (controlMode == CombatControlMode.Demonstration)
        {
            gridSelect.ExitCharacter();
            GetComponent<TurnManager>().StopTurnLoop();
            StartCoroutine(ResetAfterDelay());
            return true;
        }

        if (controlMode == CombatControlMode.MultiAgent)
        {
            foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
                agent.OnDefeat();
            StartCoroutine(EndEpisodeNextFrameMultiAgent());
            return true;
        }

        if (controlMode == CombatControlMode.BehaviorTree)
        {
            FinalizeTestEpisode(monsterWon: false); // if current agent team = 2, monster lost
            StartCoroutine(BehaviorTreeResetAfterDelay());
            return true;
        }

        ShowPlayerDefeatUI();
        return true;
    }

    private void HandleCombatOverButton(string result)
    {
        combatOverUI.OnButtonSelected -= HandleCombatOverButton;

        if (result == "Victory")
        {
            GameManager.Instance.EndCombat();
        }
        else if (result == "Defeat")
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Tavern");
        }
    }

    public bool HandleTurnEnd(int currentTurn)
    {
        if (controlMode == CombatControlMode.MLAgent)
        {
            if (currentTurn > maxTurn)
            {
                combatAgent.OnDefeat();
                StartCoroutine(EndEpisodeNextFrame());
                return true;
            }
            combatAgent.OnGlobalTurnEnd();
        }

        if (controlMode == CombatControlMode.MultiAgent)
        {
            if (currentTurn > maxTurn)
            {
                foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
                    agent.OnDefeat();
                StartCoroutine(EndEpisodeNextFrameMultiAgent());
                return true;
            }

            foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
            {
                if (!agent.IsDeadThisEpisode)
                    agent.OnGlobalTurnEnd();
            }
        }

        if (controlMode == CombatControlMode.Demonstration)
        {
            if (currentTurn > maxTurn)
            {
                gridSelect.ExitCharacter();
                GetComponent<TurnManager>().StopTurnLoop();
                StartCoroutine(ResetAfterDelay());
                return true;
            }
        }

        if (controlMode == CombatControlMode.BehaviorTree)
        {
            if (currentTurn > maxTurn)
            {
                FinalizeTestEpisode(monsterWon: false);
                StartCoroutine(BehaviorTreeResetAfterDelay());
                return true;
            }
        }
        return false;
    }

    public void ResetEnv()
    {
        Cleanup();
        GetComponent<CombatExecutor>().ResetState();
        
        DefeatedEnemyNames.Clear();

        mapPopulator = GetComponentInChildren<PopulateMap>();
        mapPopulator.Generate();

        GridData newGridData = mapPopulator.objectsData;
        GridData = newGridData;

        if (combatAgent && combatAgent.gameObject.activeSelf)
        {
            combatAgent.ResetState();
            combatAgent.SetGridData(newGridData);
            combatAgent.SetAgentTeam(currentAgentTeam);
        }

        if (controlMode == CombatControlMode.MultiAgent)
        {
            foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
                agent.SetGridData(newGridData);
        }

        OnEnvReset?.Invoke(newGridData);

        if (showTurnOrderUI)
            turnOrderUI.Refresh(new List<CharacterObject>());
    }

    private void Cleanup()
    {
        if (GridData == null) return;

        foreach (var unit in GridData.GetAllUnits())
        {
            if (unit.character != null)
                UnsubscribeCharacterDeath(unit.character);
        }
    }

    private void ShowPlayerVictoryUI()
    {
        gridSelect.ExitCharacter();
        turnOrderUI.gameObject.SetActive(false);

        foreach (var name in DefeatedEnemyNames)
            GameManager.Instance.defeatedEnemyNames.Add(name);

        combatOverUI.OnButtonSelected += HandleCombatOverButton;
        combatOverUI.Show(true);
    }

    private void ShowPlayerDefeatUI()
    {
        gridSelect.ExitCharacter();
        turnOrderUI.gameObject.SetActive(false);
        combatOverUI.OnButtonSelected += HandleCombatOverButton;
        combatOverUI.Show(false);
    }

    private IEnumerator EndEpisodeNextFrame()
    {
        yield return null;

        if (isRecordingMode)
        {
            float safetyTimer = 0f;
            while (!combatAgent.IsBTActionFullyProcessed() && safetyTimer < 2f)
            {
                safetyTimer += Time.deltaTime;
                yield return null;
            }
            combatAgent.ForceComplete();
        }

        bool monsterWon = GridData.GetUnitsByTeam(currentAgentTeam).Count > 0;
        FinalizeTestEpisode(monsterWon);

        if (testRecorder != null && testRecorder.ShouldStop)
        {
            turnManager.StopTurnLoop();
            yield break;
        }

        combatAgent.EndEpisode();
    }

    private IEnumerator EndEpisodeNextFrameMultiAgent()
    {
        yield return null;

        if (isRecordingMode)
        {
            float safetyTimer = 0f;
            bool allProcessed = false;

            while (!allProcessed && safetyTimer < 2f)
            {
                allProcessed = true;

                foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
                {
                    if (!agent.IsBTActionFullyProcessed())
                    {
                        allProcessed = false;
                        break;
                    }
                }

                if (!allProcessed)
                {
                    safetyTimer += Time.deltaTime;
                    yield return null;
                }
            }

            if (safetyTimer >= 2f)
                Debug.LogWarning("EndEpisodeNextFrameMultiAgent: safety timeout waiting for BT actions.");

            foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
                agent.ForceComplete();
        }

        bool monsterWon = GridData.GetUnitsByTeam(currentAgentTeam).Count > 0;
        FinalizeTestEpisode(monsterWon);

        if (testRecorder != null && testRecorder.ShouldStop)
        {
            turnManager.StopTurnLoop();
            yield break;
        }

        foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
            agent.EndEpisode();
        turnManager.ResetEnv();
    }

    private void FinalizeTestEpisode(bool monsterWon)
    {
        if (testRecorder == null) return;
        testRecorder.FinalizeEpisode(monsterWon, GridData);
    }

    private IEnumerator BehaviorTreeResetAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);

        if (testRecorder != null && testRecorder.ShouldStop)
        {
            turnManager.StopTurnLoop();
            yield break;
        }

        GetComponent<TurnManager>().ResetEnv();
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        GetComponent<TurnManager>().ResetEnv();
    }
}
