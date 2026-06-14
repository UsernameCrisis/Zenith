using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class CombatLLMController : MonoBehaviour, ITurnActor
{
    [Header("AI Brains")]
    public CombatLLMChatManager warriorBrain;
    public CombatLLMChatManager clericBrain;

    [Header("UI & Input")]
    public GameObject commandOverlay;
    public GameObject inputElements;
    public TMP_InputField playerCommandInput;
    public TextMeshProUGUI statusText;

    [Header("Settings")]
    [SerializeField] private string clericNpcName = "Iris";
    [SerializeField] private string warriorNpcName = "Thorek";
    [SerializeField] private int clericHealAmount = 10;
    [SerializeField] private int healRange = 2;

    private CombatExecutor combatExecutor;
    private MovementPreview previewSystem;
    private TurnManager turnManager;
    private GridData gridData;
    private CharacterObject currentUnit;
    private bool isTurnComplete = false;
    public bool IsPlayer => false;
    public bool IsTurnComplete() => isTurnComplete;

    void Awake()
    {
        combatExecutor = GetComponentInParent<CombatExecutor>();
        previewSystem = GetComponentInParent<MovementPreview>();
        turnManager = GetComponentInParent<TurnManager>();
    }

    public void BeginTurn(GridData gridData, CharacterObject character)
    {
        this.gridData = gridData;
        this.currentUnit = character;
        isTurnComplete = false;
        OpenCommandInterface(character.Name);
    }

    public void EndTurn() { }

    public void OpenCommandInterface(string npcName)
    {
        commandOverlay.SetActive(true);
        inputElements.SetActive(true);
        statusText.text = $"Command {npcName} (or leave blank)";
        playerCommandInput.text = "";
        playerCommandInput.ActivateInputField();
    }

    public void OnSendButtonPressed()
    {
        inputElements.SetActive(false);
        StartCoroutine(RunLLMTurn());
    }

    public IEnumerator RunLLMTurn()
    {
        string npcName = currentUnit.Name;
        string battlefieldInfo = GenerateBattlefieldContext();
        string playerOrder = playerCommandInput.text;
        string mainCharPos = GetMainCharacterPosition();

        CombatLLMChatManager brain = (npcName == warriorNpcName) ? warriorBrain : clericBrain;

        statusText.text = $"{npcName} is thinking...";

        string llmAction = null;
        int llmX = 0, llmY = 0;
        bool responseReceived = false;

        brain.RequestCombatAction(playerOrder, battlefieldInfo, mainCharPos, (action, x, y) =>
        {
            playerCommandInput.text = "";
            llmAction = action;
            llmX = x;
            llmY = y;
            responseReceived = true;
        });

        // Timeout so a hung Ollama request doesn't freeze the game forever
        float elapsed = 0f;
        float timeout = 45f;
        while (!responseReceived && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!responseReceived)
        {
            Debug.LogWarning("[LLM] Request timed out. Running BT fallback.");
            yield return StartCoroutine(RunBTFallback());
            FinishTurn();
            yield break;
        }

        if (turnManager.State != CombatState.Playing)
        {
            Debug.LogWarning("Combat State is not Playing");
            yield break;
        }

        statusText.text = $"{npcName} decided to {llmAction} at ({llmX},{llmY})!";
        Debug.Log($"[Combat] {npcName} performs {llmAction} at tile {llmX}, {llmY}");

        yield return new WaitForSeconds(0.8f);
        yield return StartCoroutine(ExecuteWithFallback(llmAction, llmX, llmY));
        yield return new WaitForSeconds(1.0f);

        FinishTurn();
    }

    private string GetMainCharacterPosition()
    {
        var allies = gridData.GetUnitsByTeam(currentUnit.Team);
        foreach (var (pos, ally) in allies)
        {
            if (ally.Name == "Main Character" && ally.HP > 0)
                return $"({pos.x},{pos.y})";
        }
        return "(unknown)";
    }

    private IEnumerator ExecuteWithFallback(string action, int x, int y)
    {
        Vector3Int currentPos = currentUnit.Position;
        Vector3Int targetPos = new Vector3Int(x, y, 0);

        if (!gridData.IsWithinBounds(targetPos))
        {
            Debug.LogWarning($"[LLM] Target {targetPos} is out of bounds. Running BT fallback.");
            yield return StartCoroutine(RunBTFallback());
            yield break;
        }

        switch (action.ToLower())
        {
            case "attack":
                yield return StartCoroutine(ExecuteAttackIntent(currentPos, targetPos));
                break;
            case "move":
                yield return StartCoroutine(ExecuteMoveIntent(currentPos, targetPos));
                break;
            case "heal":
                bool isCleric = currentUnit.Name == clericNpcName && clericBrain != null &&
                                clericBrain.npcRole == CombatLLMChatManager.Archetype.Cleric;
                if (isCleric)
                    yield return StartCoroutine(ExecuteHealIntent(currentPos, targetPos));
                else
                {
                    Debug.LogWarning("[LLM] Heal requested by non-cleric. Running BT fallback.");
                    yield return StartCoroutine(RunBTFallback());
                }
                break;
            default:
                Debug.LogWarning("Unknown action received: " + action);
                yield return StartCoroutine(RunBTFallback());
                break;
        }
    }

    private IEnumerator ExecuteAttackIntent(Vector3Int fromPos, Vector3Int targetPos)
    {
        CharacterObject target = gridData.GetTileAt(targetPos)?.PlacedObject as CharacterObject;

        if (target == null || target.HP <= 0 || target.Team == currentUnit.Team)
        {
            Debug.LogWarning($"[LLM] Invalid attack target at {targetPos}. Running BT fallback.");
            yield return StartCoroutine(RunBTFallback());
            yield break;
        }

        HashSet<Vector3Int> attackable = previewSystem.ComputeAttackableTiles(fromPos, currentUnit.AtkRange);
        if (attackable.Contains(targetPos) && currentUnit.CanStillAttack())
        {
            combatExecutor.ExecuteAttack(currentUnit, fromPos, targetPos, gridData);
            yield return new WaitUntil(() => !combatExecutor.IsAttacking);
            yield break;
        }

        bool moved = false;
        if (currentUnit.CanStillMove)
        {
            Vector3Int approach = FindClosestReachableTileToTarget(fromPos, targetPos);
            if (approach != fromPos)
            {
                combatExecutor.ExecuteMove(currentUnit, fromPos, approach, gridData);
                yield return new WaitUntil(() => !combatExecutor.IsMoving);
                moved = true;
                fromPos = currentUnit.Position;
            }
        }

        if (currentUnit.CanStillAttack())
        {
            attackable = previewSystem.ComputeAttackableTiles(fromPos, currentUnit.AtkRange);
            if (attackable.Contains(targetPos))
            {
                combatExecutor.ExecuteAttack(currentUnit, fromPos, targetPos, gridData);
                yield return new WaitUntil(() => !combatExecutor.IsAttacking);
                yield break;
            }
        }

        if (!moved)
        {
            Debug.LogWarning("[LLM] Can't reach attack range and can't move. Running BT fallback.");
            yield return StartCoroutine(RunBTFallback());
        }
    }

    private IEnumerator ExecuteMoveIntent(Vector3Int fromPos, Vector3Int targetPos)
    {
        if (!currentUnit.CanStillMove)
            yield break;

        HashSet<Vector3Int> reachable = previewSystem.ComputeReachableTiles(fromPos, currentUnit.RemainingMoveRange);

        Vector3Int destination = reachable.Contains(targetPos)
            ? targetPos
            : FindClosestReachableTileToTarget(fromPos, targetPos);

        if (destination == fromPos)
            yield break;

        combatExecutor.ExecuteMove(currentUnit, fromPos, destination, gridData);
        yield return new WaitUntil(() => !combatExecutor.IsMoving);
    }

    private IEnumerator ExecuteHealIntent(Vector3Int fromPos, Vector3Int targetPos)
    {
        CharacterObject healTarget = gridData.GetTileAt(targetPos)?.PlacedObject as CharacterObject;

        if (healTarget == null || healTarget.HP <= 0 || healTarget.Team != currentUnit.Team)
        {
            Debug.LogWarning("[LLM] Invalid heal target. Running BT fallback.");
            yield return StartCoroutine(RunBTFallback());
            yield break;
        }

        int dist = Mathf.Abs(fromPos.x - targetPos.x) + Mathf.Abs(fromPos.y - targetPos.y);

        if (dist > healRange && currentUnit.CanStillMove)
        {
            Vector3Int approach = FindClosestReachableTileToTarget(fromPos, targetPos);
            if (approach != fromPos)
            {
                combatExecutor.ExecuteMove(currentUnit, fromPos, approach, gridData);
                yield return new WaitUntil(() => !combatExecutor.IsMoving);
                fromPos = currentUnit.Position;
                dist = Mathf.Abs(fromPos.x - targetPos.x) + Mathf.Abs(fromPos.y - targetPos.y);
            }
        }

        if (dist <= healRange)
        {
            combatExecutor.ExecuteHeal(currentUnit, fromPos, targetPos, gridData, clericHealAmount);
            Debug.Log($"[LLM] {currentUnit.Name} healed {healTarget.Name} for {clericHealAmount} HP.");
        }
        else
        {
            Debug.Log("[LLM] Heal: moved but still out of range. Skipping heal this turn.");
        }
    }

    private IEnumerator RunBTFallback()
    {
        statusText.text = $"{currentUnit.Name}: using Behavior Tree fallback";

        Vector3Int? pos = gridData.GetPositionOf(currentUnit);
        if (pos == null) yield break;

        TileData tile = gridData.GetTileAt(pos.Value);
        EnemyAIControllerBase bt = tile?.PlacedGameObject?.GetComponent<EnemyAIControllerBase>();
        if (bt == null)
        {
            Debug.LogWarning($"[LLM] No EnemyAIControllerBase found on {currentUnit.Name}. Skipping fallback.");
            yield break;
        }
        bt.BeginTurn(gridData, currentUnit);
        yield return new WaitUntil(() => bt.IsTurnComplete());
    }

    private string GenerateBattlefieldContext()
    {
        if (gridData == null) return "No battlefield data available.";

        Vector3Int selfPos = currentUnit.Position;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.Append($"You: {currentUnit.Name} at ({selfPos.x},{selfPos.y}), ");
        sb.Append($"HP {currentUnit.HP}/{currentUnit.MaxHp}, ");
        sb.Append($"AtkRange {currentUnit.AtkRange}. ");

        var allies = gridData.GetUnitsByTeam(currentUnit.Team);
        sb.Append("Allies: ");
        bool hasAlly = false;
        foreach (var (pos, ally) in allies)
        {
            if (ally == currentUnit || ally.HP <= 0) continue;
            int dist = Mathf.Abs(selfPos.x - pos.x) + Mathf.Abs(selfPos.y - pos.y);
            sb.Append($"{ally.Name} ({pos.x},{pos.y}) HP {ally.HP}/{ally.MaxHp} dist {dist}; ");
            hasAlly = true;
        }
        if (!hasAlly) sb.Append("none; ");

        var enemies = gridData.GetEnemyTeamUnit(currentUnit.Team);
        sb.Append("Enemies: ");
        bool hasEnemy = false;
        foreach (var (pos, enemy) in enemies)
        {
            if (enemy.HP <= 0) continue;
            int dist = Mathf.Abs(selfPos.x - pos.x) + Mathf.Abs(selfPos.y - pos.y);
            sb.Append($"{enemy.Name} ({pos.x},{pos.y}) HP {enemy.HP}/{enemy.MaxHp} dist {dist}; ");
            hasEnemy = true;
        }
        if (!hasEnemy) sb.Append("none; ");

        return sb.ToString();
    }

    private Vector3Int FindClosestReachableTileToTarget(Vector3Int from, Vector3Int target)
    {
        HashSet<Vector3Int> reachable = previewSystem.ComputeReachableTiles(from, currentUnit.RemainingMoveRange);

        Vector3Int best = from;
        int bestCost = int.MaxValue;

        foreach (var tile in reachable)
        {
            int cost = previewSystem.PathCost(tile, target);
            if (cost < bestCost)
            {
                bestCost = cost;
                best = tile;
            }
        }

        return best;
    }

    private void FinishTurn()
    {
        currentUnit.ResetMovement();
        currentUnit.EnableAttack();
        statusText.text = "";
        commandOverlay.SetActive(false);
        previewSystem.ClearAll();
        isTurnComplete = true;
    }
}