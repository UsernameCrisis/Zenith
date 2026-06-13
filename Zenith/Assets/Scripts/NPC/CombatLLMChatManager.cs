using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class CombatLLMChatManager : MonoBehaviour
{
    [Header("References")]
    public OllamaChatProvider ollama;

    [Header("NPC Settings")]
    public Archetype npcRole;
    public bool isAggressive;
    public int maxMoveRange = 3;
    public int healRange = 2;

    public enum Archetype { Warrior, Cleric }

    public void RequestCombatAction(
        string playerManualCommand,
        string battlefieldContext,
        string mainCharacterPosition,
        Action<string, int, int> onActionParsed)
    {
        string systemPrompt = ConstructSystemPrompt(playerManualCommand, mainCharacterPosition);
        string userPrompt = ConstructUserPrompt(battlefieldContext);

        Debug.Log($"<color=cyan>[LLM Prompt] System:</color> {systemPrompt}");
        Debug.Log($"<color=yellow>[LLM Prompt] User:</color> {userPrompt}");

        StartCoroutine(ollama.SendChatRequest(userPrompt, systemPrompt, (response) =>
        {
            Debug.Log($"<color=green>[LLM Raw Response]:</color> {response}");
            ParseAndExecute(response, onActionParsed);
        }, true));
    }

    private string ConstructSystemPrompt(string playerManualCommand, string mainCharacterPosition)
    {
        string role = npcRole == Archetype.Cleric ? "Cleric" : "Warrior";

        string defaultBehavior = (npcRole == Archetype.Cleric)
            ? (isAggressive
                ? "Attack the enemy with the lowest HP."
                : "Heal the ally with the lowest HP. Attack only if all allies are full HP.")
            : "Attack the enemy with the lowest HP.";

        string actions = (npcRole == Archetype.Cleric)
            ? $"Actions: Move(x,y) | Attack(x,y) on enemy | Heal(x,y) on ally."
            : "Actions: Move(x,y) | Attack(x,y) on enemy. Never use Heal.";

        string playerOverride = "";
        bool hasCommand = !string.IsNullOrWhiteSpace(playerManualCommand)
                          && playerManualCommand.Trim().Length > 2;
        if (hasCommand)
        {
            string resolved = Regex.Replace(
                playerManualCommand.Trim(),
                @"\bme\b|\bI\b|\bmyself\b",
                $"the Main Character at {mainCharacterPosition}",
                RegexOptions.IgnoreCase);

            playerOverride = $"\nRULE: You MUST follow this player order: \"{resolved}\". This overrides your default behavior.";
        }

        string format =
            "Reply with ONE line only. No explanation. Exact format:\n" +
            "Move(x,y)  or  Attack(x,y)  or  Heal(x,y)\n" +
            "Use only coordinates that appear in the battlefield state.";

        return $"You are a {role} in a turn-based tactics game.\n" +
               $"Move range: {maxMoveRange} tiles.\n" +
               $"{defaultBehavior}\n" +
               $"{actions}\n" +
               $"{format}" +
               playerOverride;
    }

    private string ConstructUserPrompt(string battlefieldContext)
    {
        return $"Battlefield:\n{battlefieldContext}\nYour action:";
    }

    private void ParseAndExecute(string rawResponse, Action<string, int, int> callback)
    {
        string cleaned = Regex.Replace(
            rawResponse.Trim(),
            @"^(Action|Answer|Response|Decision|Output|Choice)\s*:\s*",
            "", RegexOptions.IgnoreCase).Trim();

        Match match = Regex.Match(
            cleaned,
            @"(Move|Attack|Heal)\s*[\(\[]\s*(-?\d+)\s*,\s*(-?\d+)\s*[\)\]]",
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            string action = match.Groups[1].Value;
            int x = int.Parse(match.Groups[2].Value);
            int y = int.Parse(match.Groups[3].Value);
            Debug.Log($"<color=white>[AI Decision]</color> {action}({x},{y})");
            callback?.Invoke(action, x, y);
            return;
        }

        Match fallback = Regex.Match(
            rawResponse,
            @"(Move|Attack|Heal)\s*[\(\[]\s*(-?\d+)\s*,\s*(-?\d+)\s*[\)\]]",
            RegexOptions.IgnoreCase);

        if (fallback.Success)
        {
            string action = fallback.Groups[1].Value;
            int x = int.Parse(fallback.Groups[2].Value);
            int y = int.Parse(fallback.Groups[3].Value);
            Debug.Log($"<color=orange>[AI Decision - fallback parse]</color> {action}({x},{y})");
            callback?.Invoke(action, x, y);
            return;
        }

        Debug.LogWarning($"[LLM] Unparseable response: '{rawResponse}'. Defaulting to Move(0,0).");
        callback?.Invoke("Move", 0, 0);
    }
}