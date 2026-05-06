using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleResultHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSystem gridSelect;
    [SerializeField] private TurnOrderUI turnOrderUI;
    [SerializeField] private CombatAgent combatAgent;
    [SerializeField] private CombatOverMenu combatOverUI;

    [Header("Settings")]
    [SerializeField] private CombatControlMode controlMode = CombatControlMode.Player;
    [SerializeField] private int currentAgentTeam = 1;
    [SerializeField] private bool showTurnOrderUI = true;
    [SerializeField] private int maxTurn = 50;

    public int MaxTurn => maxTurn;
    public GridData GridData { private get; set; }
    public List<string> DefeatedEnemyNames { get; private set; } = new();
    public System.Action<CharacterObject> OnCharacterDied;
    public System.Action<GridData> OnEnvReset;
    private PopulateMap mapPopulator;
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
        if (controlMode == CombatControlMode.MLAgent)
        {
            if (character.Team == currentAgentTeam)
                combatAgent.AddReward(-0.5f);
            else
                combatAgent.AddReward(0.5f);
        }

        OnCharacterDied?.Invoke(character);
    }

    public bool CheckBattleEnd()
    {
        int aliveAllies  = GridData.GetUnitsByTeam(currentAgentTeam).Count;
        int aliveEnemies = GridData.GetUnitsByTeam(currentAgentTeam == 1 ? 2 : 1).Count;

        if (aliveEnemies == 0) return HandleVictory();
        if (aliveAllies  == 0) return HandleDefeat();

        return false;
    }

    private bool HandleVictory()
    {
        if (controlMode == CombatControlMode.MLAgent)
        {
            gridSelect.ExitCharacter();
            combatAgent.AddReward(1f);
            StartCoroutine(EndEpisodeNextFrame());
            return true;
        }

        // SceneManager.UnloadSceneAsync("Combat_test1");
        // ShowScene(SceneManager.GetActiveScene());
        // Destroy(GameManager.Instance.CurrentEnemy);
        // GameManager.Instance.CurrentEnemy = null;

        // Player mode, pass defeated enemy names up to GameManager.
        gridSelect.ExitCharacter();
        turnOrderUI.gameObject.SetActive(false);
        combatOverUI.Show(true);

        foreach (var name in DefeatedEnemyNames)
            GameManager.Instance.defeatedEnemyNames.Add(name);

        Debug.Log($"Enemies defeated: {string.Join(", ", GameManager.Instance.defeatedEnemyNames)}");

        combatOverUI.OnButtonSelected += HandleCombatOverButton;
        combatOverUI.Show(true);
        return true;
    }

    private bool HandleDefeat()
    {
        if (controlMode == CombatControlMode.MLAgent)
        {
            combatAgent.AddReward(-1f);
            StartCoroutine(EndEpisodeNextFrame());
            return true;
        }
        gridSelect.ExitCharacter();
        turnOrderUI.gameObject.SetActive(false);
        combatOverUI.OnButtonSelected += HandleCombatOverButton;
        combatOverUI.Show(false);
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
        if (controlMode != CombatControlMode.MLAgent) return false;

        if (currentTurn > maxTurn)
        {
            int enemiesAlive = GridData.GetUnitsByTeam(currentAgentTeam == 1 ? 2 : 1).Count;
            float penalty = -0.5f - (0.5f * (enemiesAlive / 3f));
            combatAgent.AddReward(penalty);
            StartCoroutine(EndEpisodeNextFrame());
            return true;
        }

        combatAgent.AddReward(-0.01f);
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

        combatAgent.SetGridData(newGridData);
        combatAgent.SetAgentTeam(currentAgentTeam);

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

    private IEnumerator EndEpisodeNextFrame()
    {
        yield return null;
        combatAgent.EndEpisode();
    }
}
