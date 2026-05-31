using UnityEngine;
using Unity.MLAgents.Policies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.InferenceEngine;

[Serializable]
public class AgentModeConfig
{
    public string modeName;
    public CombatControlMode controlMode;
    [Tooltip("Leave null for BehaviorTree and MultiAgent modes")]
    public ModelAsset combatAgent2Model;
    public int episodesToRun = 500;
}

[Serializable]
public struct UnitDeathRecord
{
    public int turn;
    public int unitID;
    public int team;
}

public class TestRecorder : MonoBehaviour
{
    [Header("Agent Configurations")]
    [SerializeField] private List<AgentModeConfig> modes = new();
    [SerializeField] private int currentModeIndex = 0;

    [Header("Recording")]
    [SerializeField] private bool isGatheringData = false;

    [Header("References")]
    [SerializeField] private BattleResultHandler battleResultHandler;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private CombatAgent2 combatAgent2;

    private const int ID_PLAYER = 0;
    private const int ID_CLERIC = 1;
    private const int ID_WARRIOR = 2;
    private const int ID_FLOATINGEYE = 3;
    private const int ID_SLIME = 4;
    private const int ID_BAT = 5;

    private AgentModeConfig activeConfig;
    private int episodesCompleted = 0;
    private bool isStopping = false;

    private int damageByMonsters = 0;
    private int damageByPlayers = 0;
    private List<UnitDeathRecord> deathsThisEpisode = new();
    private List<(CharacterObject character, Action<int> handler)> damageSubscriptions = new();

    private StreamWriter csvWriter;
    private string outputPath;

    public bool ShouldStop => isStopping;

    void Awake()
    {
        if (!isGatheringData) return;
        if (modes == null || modes.Count == 0)
        {
            Debug.LogError("[TestRecorder] No AgentModeConfigs defined!");
            enabled = false;
            return;
        }

        if (currentModeIndex < 0 || currentModeIndex >= modes.Count)
        {
            Debug.LogError($"[TestRecorder] currentModeIndex {currentModeIndex} is out of range!");
            enabled = false;
            return;
        }

        activeConfig = modes[currentModeIndex];

        battleResultHandler.SetControlMode(activeConfig.controlMode);

        if (activeConfig.combatAgent2Model != null && combatAgent2 != null)
        {
            BehaviorParameters bp = combatAgent2.GetComponent<BehaviorParameters>();
            if (bp != null)
                bp.Model = activeConfig.combatAgent2Model;
            else
                Debug.LogWarning("[TestRecorder] CombatAgent2 has no BehaviorParameters component.");
        }

        string dir = Path.Combine(Application.dataPath, "..", "TestResults");
        Directory.CreateDirectory(dir);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        outputPath = Path.Combine(dir, $"{activeConfig.modeName}_{timestamp}_episodes.csv");

        csvWriter = new StreamWriter(outputPath, append: false, encoding: Encoding.UTF8);
        WriteCSVHeader();

        Debug.Log($"[TestRecorder] Mode: {activeConfig.modeName} | Target: {activeConfig.episodesToRun} episodes | Output: {outputPath}");
    }

    private bool wasRecordingLastFrame = false;

    void Update()
    {
        if (isGatheringData && !wasRecordingLastFrame)
        {
            damageByMonsters = 0;
            damageByPlayers  = 0;
            deathsThisEpisode.Clear();
            Debug.Log("[TestRecorder] Recording started — per-episode accumulators reset.");
        }

        wasRecordingLastFrame = isGatheringData;
    }

    void OnEnable()
    {
        battleResultHandler.OnEnvReset += HandleEpisodeStarted;
        battleResultHandler.OnCharacterDied += HandleCharacterDied;
    }

    void OnDisable()
    {
        battleResultHandler.OnEnvReset -= HandleEpisodeStarted;
        battleResultHandler.OnCharacterDied -= HandleCharacterDied;
        UnsubscribeAllDamage();
    }

    private void HandleEpisodeStarted(GridData newGridData)
    {
        damageByMonsters = 0;
        damageByPlayers = 0;
        deathsThisEpisode.Clear();

        UnsubscribeAllDamage();
        foreach (var (_, character) in newGridData.GetAllUnits())
            SubscribeDamage(character);
    }

    private void HandleCharacterDied(CharacterObject character)
    {
        if (!isGatheringData) return;
        deathsThisEpisode.Add(new UnitDeathRecord
        {
            turn = turnManager.currentTurn,
            unitID = character.ID,
            team = character.Team
        });
    }

    public void FinalizeEpisode(bool monsterTeamWon, GridData currentGridData)
    {
        if (!isGatheringData) return;
        if (isStopping) return;

        int playerUnitsAlive = currentGridData.GetUnitsByTeam(1).Count;
        int monsterUnitsAlive = currentGridData.GetUnitsByTeam(2).Count;
        int totalTurns = turnManager.currentTurn;

        float rewardFloatingEye = 0f;
        float rewardSlime       = 0f;
        float rewardBat         = 0f;
        float rewardTotal       = 0f;

        if (activeConfig.controlMode == CombatControlMode.MultiAgent)
        {
            foreach (UnitAgentBase agent in turnManager.ActiveUnitAgents)
            {
                float r = agent.CumulativeReward;
                if (agent is FloatingEyeAgent) rewardFloatingEye = r;
                else if (agent is SlimeAgent)rewardSlime = r;
                else if (agent is BatAgent) rewardBat = r;
            }
            rewardTotal = rewardFloatingEye + rewardSlime + rewardBat;
        }
        else if (activeConfig.controlMode == CombatControlMode.MLAgent)
        {
            rewardTotal = combatAgent2 != null ? combatAgent2.CumulativeReward : 0f;
        }

        UpdateAccumulators(monsterTeamWon, totalTurns, playerUnitsAlive, monsterUnitsAlive,
                            damageByMonsters, damageByPlayers, rewardTotal);

        WriteCSVRow(monsterTeamWon, totalTurns, playerUnitsAlive, monsterUnitsAlive,
                    damageByMonsters, damageByPlayers, rewardTotal, rewardFloatingEye,
                    rewardSlime, rewardBat, deathsThisEpisode);

        episodesCompleted++;
        Debug.Log($"[TestRecorder] Episode {episodesCompleted}/{activeConfig.episodesToRun} complete. MonsterWon={monsterTeamWon}");

        if (episodesCompleted >= activeConfig.episodesToRun)
        {
            isStopping = true;
            WriteSummary();
            csvWriter.Flush();
            csvWriter.Close();
            Debug.Log($"[TestRecorder] All {activeConfig.episodesToRun} episodes complete. Results saved to {outputPath}");
        }
    }

    private void SubscribeDamage(CharacterObject character)
    {
        int team = character.Team;

        Action<int> handler = (damage) =>
        {
            if (team == 2)
                damageByPlayers += damage;
            else
                damageByMonsters += damage;
        };

        character.OnTakenDamage += handler;
        damageSubscriptions.Add((character, handler));
    }

    private void UnsubscribeAllDamage()
    {
        foreach (var (character, handler) in damageSubscriptions)
        {
            if (character != null)
                character.OnTakenDamage -= handler;
        }
        damageSubscriptions.Clear();
    }

    private void WriteCSVHeader()
    {
        csvWriter.WriteLine(
            "episode,agent_mode,monster_won,total_turns," +
            "player_units_alive,monster_units_alive," +
            "damage_by_monsters,damage_by_players," +
            "agent_total_reward,floatingeye_reward,slime_reward,bat_reward," +
            "player_death_turn,cleric_death_turn,warrior_death_turn," +
            "floatingeye_death_turn,slime_death_turn,bat_death_turn");
        csvWriter.Flush();
    }

    private void WriteCSVRow(bool monsterWon, int totalTurns, int playerUnitsAlive,
                            int monsterUnitsAlive, int dmgByMonsters, int dmgByPlayers,
                            float rewardTotal, float rewardFloatingEye, float rewardSlime,
                            float rewardBat, List<UnitDeathRecord> deaths)
    {
        int[] deathTurns = new int[] { -1, -1, -1, -1, -1, -1 };
        foreach (var death in deaths)
        {
            if (death.unitID >= 0 && death.unitID <= 5)
                deathTurns[death.unitID] = death.turn;
        }

        csvWriter.WriteLine(
            $"{episodesCompleted + 1}," +
            $"{activeConfig.modeName}," +
            $"{(monsterWon ? 1 : 0)}," +
            $"{totalTurns}," +
            $"{playerUnitsAlive}," +
            $"{monsterUnitsAlive}," +
            $"{dmgByMonsters}," +
            $"{dmgByPlayers}," +
            $"{rewardTotal:F4}," +
            $"{rewardFloatingEye:F4}," +
            $"{rewardSlime:F4}," +
            $"{rewardBat:F4}," +
            $"{deathTurns[ID_PLAYER]}," +
            $"{deathTurns[ID_CLERIC]}," +
            $"{deathTurns[ID_WARRIOR]}," +
            $"{deathTurns[ID_FLOATINGEYE]}," +
            $"{deathTurns[ID_SLIME]}," +
            $"{deathTurns[ID_BAT]}");

        csvWriter.Flush();
    }

    private void WriteSummary()
    {
        csvWriter.Flush();

        csvWriter.WriteLine();
        csvWriter.WriteLine("=== SUMMARY ===");
        csvWriter.WriteLine(
            "agent_mode,total_episodes,total_wins,total_losses," +
            "outcome,mean_turns," +
            "mean_dmg_by_monsters,mean_dmg_by_players," +
            "mean_player_units_alive,mean_monster_units_alive," +
            "mean_agent_reward");

        float meanPlayerAlive  = episodesCompleted > 0
            ? totalPlayerAliveAccum  / episodesCompleted : 0f;
        float meanMonsterAlive = episodesCompleted > 0
            ? totalMonsterAliveAccum / episodesCompleted : 0f;

        // WIN ROW
        float meanTurnsWin = totalWins > 0 ? totalTurnsOnWinAccum / totalWins : 0f;
        float meanDmgMonWin = totalWins > 0 ? totalDmgMonsterOnWinAccum / totalWins : 0f;
        float meanDmgPlyWin = totalWins > 0 ? totalDmgPlayerOnWinAccum / totalWins : 0f;
        float meanRewardWin = totalWins > 0 ? totalRewardOnWinAccum / totalWins : 0f;

        csvWriter.WriteLine(
            $"{activeConfig.modeName}," +
            $"{episodesCompleted}," +
            $"{totalWins}," +
            $"{totalLosses}," +
            $"WIN," +
            $"{meanTurnsWin:F2}," +
            $"{meanDmgMonWin:F2}," +
            $"{meanDmgPlyWin:F2}," +
            $"{meanPlayerAlive:F4}," +
            $"{meanMonsterAlive:F4}," +
            $"{meanRewardWin:F4}");

        // LOSS ROW
        float meanTurnsLoss = totalLosses > 0 ? totalTurnsOnLossAccum / totalLosses : 0f;
        float meanDmgMonLoss = totalLosses > 0 ? totalDmgMonsterOnLossAccum / totalLosses : 0f;
        float meanDmgPlyLoss = totalLosses > 0 ? totalDmgPlayerOnLossAccum / totalLosses : 0f;
        float meanRewardLoss = totalLosses > 0 ? totalRewardOnLossAccum / totalLosses : 0f;

        csvWriter.WriteLine(
            $"{activeConfig.modeName}," +
            $"{episodesCompleted}," +
            $"{totalWins}," +
            $"{totalLosses}," +
            $"LOSS," +
            $"{meanTurnsLoss:F2}," +
            $"{meanDmgMonLoss:F2}," +
            $"{meanDmgPlyLoss:F2}," +
            $"{meanPlayerAlive:F4}," +
            $"{meanMonsterAlive:F4}," +
            $"{meanRewardLoss:F4}");
    }

    private int totalWins = 0;
    private int totalLosses = 0;
    private float totalTurnsOnWinAccum = 0f;
    private float totalTurnsOnLossAccum = 0f;
    private float totalDmgMonsterOnWinAccum = 0f;
    private float totalDmgMonsterOnLossAccum = 0f;
    private float totalDmgPlayerOnWinAccum = 0f;
    private float totalDmgPlayerOnLossAccum = 0f;
    private float totalPlayerAliveAccum = 0f;
    private float totalMonsterAliveAccum = 0f;
    private float totalRewardAccum = 0f;
    private float totalRewardOnWinAccum = 0f;
    private float totalRewardOnLossAccum = 0f;

    private void UpdateAccumulators(bool monsterWon, int turns,
        int playerAlive, int monsterAlive,
        int dmgByMonsters, int dmgByPlayers, float rewardTotal)
    {
        totalPlayerAliveAccum += playerAlive;
        totalMonsterAliveAccum += monsterAlive;
        totalRewardAccum       += rewardTotal;

        if (monsterWon)
        {
            totalWins++;
            totalTurnsOnWinAccum += turns;
            totalDmgMonsterOnWinAccum   += dmgByMonsters;
            totalDmgPlayerOnWinAccum    += dmgByPlayers;
            totalRewardOnWinAccum       += rewardTotal;
        }
        else
        {
            totalLosses++;
            totalTurnsOnLossAccum       += turns;
            totalDmgMonsterOnLossAccum  += dmgByMonsters;
            totalDmgPlayerOnLossAccum   += dmgByPlayers;
            totalRewardOnLossAccum      += rewardTotal;
        }
    }

    void OnDestroy()
    {
        if (csvWriter != null)
        {
            if (!isStopping && episodesCompleted > 0)
            {
                Debug.LogWarning($"[TestRecorder] Stopped early after {episodesCompleted} episodes. Writing partial summary.");
                WriteSummary();
            }
            csvWriter.Flush();
            csvWriter.Close();
        }
    }
}
