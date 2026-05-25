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

    public enum Archetype { Warrior, Cleric }

    public void RequestCombatAction(
        string playerManualCommand,
        string battlefieldContext,
        string mainCharacterPosition,
        Action<string, int, int> onActionParsed)
    {
        string systemPrompt = ConstructSystemPrompt();
        string userPrompt = ConstructUserPrompt(playerManualCommand, battlefieldContext, mainCharacterPosition);

        Debug.Log($"<color=cyan>[LLM Prompt] System:</color> {systemPrompt}");
        Debug.Log($"<color=yellow>[LLM Prompt] User:</color> {userPrompt}");

        StartCoroutine(ollama.SendChatRequest(userPrompt, systemPrompt, (response) =>
        {
            Debug.Log($"<color=green>[LLM Raw Response]:</color> {response}");
            ParseAndExecute(response, onActionParsed);
        }, true));
    }

    private string ConstructSystemPrompt()
    {
        string behavior = isAggressive
            ? "prioritize attacking the enemy with the lowest HP"
            : "prioritize healing the ally with the lowest HP";

        string ability = (npcRole == Archetype.Cleric)
            ? "You may use Heal(x,y) targeting an ally's exact grid coordinates. You may use Attack(x,y) if no allies need healing."
            : "You CANNOT use Heal. Always use Attack(x,y).";

        return $"You are a {npcRole} in a turn-based tactical game. {behavior}. {ability}. " +
               $"You can move and act in the same turn, so prefer Attack or Heal when possible. " +
               $"Your movement range is {maxMoveRange} tiles per turn. " +
               "Respond with ONE action only. No explanation. No punctuation after. " +
               "Format: Move(x,y) or Attack(x,y) or Heal(x,y). " +
               "Always use exact grid coordinates from the battlefield state. " +
               "Examples:\n" +
               "Attack(-1,3)\n" +
               "Heal(-2,-1)\n" +
               "Move(1,2)";
    }

    private string ConstructUserPrompt(string playerManualCommand, string battlefieldContext, string mainCharacterPosition)
    {
        bool hasValidCommand = !string.IsNullOrWhiteSpace(playerManualCommand)
                               && playerManualCommand.Trim().Length > 2;

        string commandLine;
        if (hasValidCommand)
        {
            string resolved = Regex.Replace(
                playerManualCommand.Trim(),
                @"\bme\b|\bI\b|\bmyself\b",
                $"the Main Character {mainCharacterPosition}",
                RegexOptions.IgnoreCase);

            commandLine = $"The Main Character {mainCharacterPosition}: {resolved}";
        }
        else
        {
            commandLine = "Player Command: None. Use your own judgment.";
        }

        return $"Battlefield State: {battlefieldContext}\n" +
               $"Your movement range is {maxMoveRange} tiles.\n" +
               commandLine + "\n" +
               "Respond with your action now:";
    }

    private void ParseAndExecute(string rawResponse, Action<string, int, int> callback)
    {
        // Strip common weak-model preamble: "Action:", "Answer:", etc.
        string cleaned = Regex.Replace(rawResponse.Trim(), @"^[\w\s]*:\s*", "").Trim();

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

        // Fallback: try on the raw response in case cleaning broke it
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