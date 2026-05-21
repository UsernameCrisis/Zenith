using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CombatLLMController : MonoBehaviour
{
    [Header("AI Brains")]
    public CombatLLMChatManager warriorBrain;
    public CombatLLMChatManager clericBrain;

    [Header("UI & Input")]
    public GameObject commandOverlay;
    public GameObject inputElements;
    public TMP_InputField playerCommandInput;
    public TextMeshProUGUI statusText;

    private string currentActingNPC;

    public void OpenCommandInterface(string npcName)
    {
        currentActingNPC = npcName;
        commandOverlay.SetActive(true);
        inputElements.SetActive(true);
        statusText.text = $"Command {npcName} (or leave blank)";
        playerCommandInput.text = "";
        playerCommandInput.ActivateInputField();
    }

    public void OnSendButtonPressed()
    {
        inputElements.SetActive(false);

        StartNPCTurn(currentActingNPC);
    }

    public void StartNPCTurn(string npcName)
    {
        string battlefieldInfo = GenerateBattlefieldContext();
        string playerOrder = playerCommandInput.text;

        CombatLLMChatManager brain = (npcName == "Thorek") ? warriorBrain : clericBrain;

        statusText.text = $"{npcName} is thinking...";

        brain.RequestCombatAction(playerOrder, battlefieldInfo, (action, x, y) =>
        {
            playerCommandInput.text = "";
            ExecuteNPCAction(npcName, action, x, y);
        });
    }

    private void ExecuteNPCAction(string npcName, string action, int x, int y)
    {
        statusText.text = $"{npcName} decided to {action} at ({x},{y})!";
        Debug.Log($"[Combat] {npcName} performs {action} at tile {x}, {y}");

        Invoke("CloseUI", 1.0f);

        // execute npc actions...
    }

    private void CloseUI()
    {
        commandOverlay.SetActive(false);
        inputElements.SetActive(false);
        statusText.text = "";
    }

    private string GenerateBattlefieldContext()
    {
        // generate battle scene context
        return "Ally at (1,1), Enemy at (5,5), Health: 85%. No cover nearby.";
    }
}