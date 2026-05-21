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

    public enum Archetype { Warrior, Cleric }

    public void RequestCombatAction(string playerManualCommand, string battlefieldContext, Action<string, int, int> onActionParsed)
    {
        string systemPrompt = ConstructSystemPrompt();

        string userPrompt = $"Battlefield State: {battlefieldContext}. " +
                            $"max movement context** " +
                            $"Player Command: {(string.IsNullOrEmpty(playerManualCommand) ? "None, use your own judgment." : playerManualCommand)}";

        StartCoroutine(ollama.SendChatRequest(userPrompt, systemPrompt, (response) =>
        {
            ParseAndExecute(response, onActionParsed);
        }, true));
    }

    private string ConstructSystemPrompt()
    {
        string behavior = isAggressive ? "prioritize attacking enemies especially with low health" : "prioritize survival and positioning away from enemies";
        string ability = (npcRole == Archetype.Cleric) ? "You can use Heal(x,y) on allies." : "You cannot heal.";

        return $"You are a {npcRole} in a tactical game. {behavior}. {ability}. " +
               "Output ONLY the action in format: Action(x,y). No conversational text, no explanations. " +
               "Valid actions: Move, Attack, Heal. Example: Attack(2,1).";
    }

    private void ParseAndExecute(string rawResponse, Action<string, int, int> callback)
    {
        Match match = Regex.Match(rawResponse, @"(\w+)\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)");

        if (match.Success)
        {
            string action = match.Groups[1].Value;
            int x = int.Parse(match.Groups[2].Value);
            int y = int.Parse(match.Groups[3].Value);

            Debug.Log($"AI Decision Parsed: {action} at ({x},{y})");
            callback?.Invoke(action, x, y); // passing the parameters to action function ExecuteNPCAction
        }
        else
        {
            Debug.LogWarning($"AI returned invalid format: '{rawResponse}'. Defaulting to Move(0,0)");
            callback?.Invoke("Move", 0, 0); // fallback to do nothing, move to, or to behavior tree
        }
    }
}