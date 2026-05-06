using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using static OllamaChatProvider;

public class DialogueManager : MonoBehaviour
{
    [Header("External References")]
    public PlayerMovement player;

    [Header("UI Containers")]
    public GameObject dialoguePanel;
    public GameObject optionsPanel;
    public RectTransform leftPortraitRect;
    public RectTransform rightPortraitRect;
    public RectTransform textPanelRect;

    [Header("Interaction Buttons")]
    public GameObject talkButton;
    public GameObject giftButton;
    public GameObject shopButton;
    public GameObject partyButton;

    [Header("AI Chat Settings")]
    public GameObject chatInputPanel;
    public TMP_InputField chatInputField;
    public OllamaChatProvider ollamaProvider;

    [Header("UI Content")]
    public Image leftPortrait;
    public Image rightPortrait;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Animation Settings")]
    public float animDuration = 0.5f;
    public float offscreenOffset = 500f;

    [Header("Text Settings")]
    public float typingSpeed = 0.02f;

    [Header("Memory Settings")]
    public int maxMemoryLines = 4;
    private List<string> currentConversationHistory = new List<string>();

    private DialogueData currentDialogue;
    private string currentNPCID;
    private string lastFullResponse;
    private int index;
    private bool isTyping;
    private bool isDialogueActive;
    private bool isShowingOptions;
    private bool canInteract = true;
    private bool isAIResponding;
    private bool isWaitingForAI;
    private Coroutine typeRoutine;

    private Vector2 leftTargetPos;
    private Vector2 rightTargetPos;
    private Vector2 textTargetPos;

    private void Awake()
    {
        leftTargetPos = leftPortraitRect.anchoredPosition;
        rightTargetPos = rightPortraitRect.anchoredPosition;
        textTargetPos = textPanelRect.anchoredPosition;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetDailyTalkLimits();
        }
    }

    private void Start()
    {
        dialoguePanel.SetActive(false);
        optionsPanel.SetActive(false);
        isDialogueActive = false;
        isShowingOptions = false;

        chatInputField.onSelect.AddListener(delegate { BlockInteractions(true); });
        chatInputField.onDeselect.AddListener(delegate { BlockInteractions(false); });
    }

    private void BlockInteractions(bool isTyping)
    {
        canInteract = !isTyping;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive || isShowingOptions || !canInteract;
    }

    public void StartDialogue(DialogueData data, string npcID)
    {
        if (isDialogueActive || isShowingOptions || !canInteract) return;

        if (player != null) player.canMove(false);

        currentNPCID = npcID;
        currentDialogue = data;
        index = 0;
        isDialogueActive = true;
        isShowingOptions = false;

        dialoguePanel.SetActive(true);
        optionsPanel.SetActive(false);

        leftPortraitRect.anchoredPosition = leftTargetPos + new Vector2(-offscreenOffset, 0);
        rightPortraitRect.anchoredPosition = rightTargetPos + new Vector2(offscreenOffset, 0);
        textPanelRect.anchoredPosition = textTargetPos + new Vector2(0, -offscreenOffset);

        StopAllCoroutines();
        StartCoroutine(AnimateIntro());
        DisplayLine();
        currentConversationHistory.Clear();
    }

    public void NextLine()
    {
        if (isTyping)
        {
            StopCoroutine(typeRoutine);
            isTyping = false;

            if (isAIResponding)
            {
                dialogueText.text = lastFullResponse;
            }
            else
            {
                dialogueText.text = currentDialogue.lines[index].text;
            }
            return;
        }

        if (isAIResponding)
        {
            isAIResponding = false;
            ShowInteractionOptions();
            return;
        }

        index++;
        if (index < currentDialogue.lines.Length)
        {
            DisplayLine();
        }
        else
        {
            ShowInteractionOptions();
        }
    }

    private void ShowInteractionOptions()
    {
        isDialogueActive = false;
        isShowingOptions = true;
        optionsPanel.SetActive(true);

        if (currentNPCID.ToLower() == "orvain")
        {
            shopButton.SetActive(true);
            partyButton.SetActive(false);
        }
        else
        {
            shopButton.SetActive(false);
            partyButton.SetActive(true);
        }

        talkButton.SetActive(true);
        giftButton.SetActive(true);
    }

    public void CloseAllDialogue()
    {
        isShowingOptions = false;
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        optionsPanel.SetActive(false);
        dialogueText.text = "";

        if (player != null) player.canMove(true);

        StartCoroutine(InteractionCooldown());
    }

    private void DisplayLine()
    {
        DialogueLine line = currentDialogue.lines[index];
        nameText.text = line.name;
        UpdatePortraits(line);

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(line.text));
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in fullText.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    private IEnumerator AnimateIntro()
    {
        float elapsed = 0;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animDuration;

            float bounceCurve = Mathf.Sin(percent * Mathf.PI * 1.1f);
            float smoothCurve = Mathf.SmoothStep(0, 1, percent);

            leftPortraitRect.anchoredPosition = Vector2.LerpUnclamped(leftPortraitRect.anchoredPosition, leftTargetPos, bounceCurve);
            rightPortraitRect.anchoredPosition = Vector2.LerpUnclamped(rightPortraitRect.anchoredPosition, rightTargetPos, bounceCurve);
            textPanelRect.anchoredPosition = Vector2.Lerp(textPanelRect.anchoredPosition, textTargetPos, smoothCurve);

            yield return null;
        }
    }

    private void UpdatePortraits(DialogueLine line)
    {
        if (line.isPlayer)
        {
            leftPortrait.sprite = line.characterPortrait;
            leftPortrait.color = Color.white;
            rightPortrait.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
        else
        {
            rightPortrait.sprite = line.characterPortrait;
            rightPortrait.color = Color.white;
            leftPortrait.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    private IEnumerator InteractionCooldown()
    {
        canInteract = false;
        yield return new WaitForSeconds(0.2f);
        canInteract = true;
    }

    public void OnTalkButtonPressed()
    {
        NPCSaveData data = GameManager.Instance.GetNPCData(currentNPCID);

        if (data.dailyTalks >= 10)
        {
            optionsPanel.SetActive(false);
            DisplayAIResponse($"{currentNPCID} doesn't seem to want to talk for now.");
            return;
        }

        optionsPanel.SetActive(false);
        chatInputPanel.SetActive(true);
        chatInputField.ActivateInputField();
    }
    public void SendChatToAI()
    {
        string userText = chatInputField.text;
        if (string.IsNullOrEmpty(userText)) return;

        chatInputPanel.SetActive(false);
        chatInputField.text = "";

        isWaitingForAI = true;
        nameText.text = currentNPCID;
        dialogueText.text = "...";

        var npcData = GameManager.Instance.GetNPCData(currentNPCID);
        string attitude = GetAttitudeString(npcData.friendship);

        var identity = GetNPCIdentity(currentNPCID, userText);

        string chatPrompt =
            $"### SYSTEM IDENTITY:\n" +
            $"Name: {currentNPCID}\n" +
            $"Persona: {identity.role}\n" +
            $"Current Attitude: {attitude}\n\n" +
            $"### INJECTED MEMORY/LORE (Use this to inform your answer, do not repeat it): \n" +
            $"{identity.lore}\n\n" +
            $"### IMPORTANT RULES:\n" +
            $"- STAY IN CHARACTER AT ALL COSTS.\n" +
            $"- Respond in one short, natural sentence.\n" +
            $"- DO NOT repeat the Injected Memory word-for-word.\n" +
            $"- DO NOT act like a virtual assistant, AI, or helpful bot.\n\n" +
            $"### DIALOGUE HISTORY:\n" +
            $"{string.Join("\n", currentConversationHistory)}\n" +
            $"Player: {userText}\n" +
            $"{currentNPCID}:";

        StartCoroutine(ollamaProvider.SendChatRequest(chatPrompt, (aiResponse) => {
            isWaitingForAI = false;
            ProcessDialogueResponse(aiResponse, userText);
        }));
    }

    private (string role, string lore) GetNPCIdentity(string npcID, string playerInput)
    {
        string path = Path.Combine(Application.streamingAssetsPath, "NPC_Knowledge", $"{npcID}.txt");

        if (!File.Exists(path))
            return ("A mysterious traveler.", "No specific lore known.");

        string[] allLines = File.ReadAllLines(path);
        if (allLines.Length == 0) return ("A citizen.", "...");

        string npcRole = allLines[0];

        List<string> foundLore = new List<string>();
        string lowerInput = playerInput.ToLower();

        for (int i = 1; i < allLines.Length; i++)
        {
            string line = allLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] keywords = lowerInput.Split(' ');
            if (keywords.Any(word => word.Length > 3 && line.ToLower().Contains(word)))
            {
                foundLore.Add(line);
            }
        }

        if (foundLore.Count > 0)
            return (npcRole, string.Join(" ", foundLore.Take(2)));

        return (npcRole, "You are in a conversation. Be natural.");
    }

    private void ProcessDialogueResponse(string aiResponse, string playerMsg)
    {
        string cleanAIResponse = aiResponse.Trim();

        DisplayAIResponse(cleanAIResponse);

        AddLineToHistory($"Player: {playerMsg}");
        AddLineToHistory($"{currentNPCID}: {cleanAIResponse}");

        StartCoroutine(GetFriendshipRating(playerMsg, cleanAIResponse));
    }

    private IEnumerator GetFriendshipRating(string playerMsg, string aiMsg)
    {
        string ratingPrompt =
            $"Rate the player's message based on the reply. Scale -1 to 10. " +
            $"Player: {playerMsg}. Reply: {aiMsg}. " +
            $"Return only the integer number.";

        yield return ollamaProvider.SendChatRequest(ratingPrompt, (scoreText) => {
            Match match = Regex.Match(scoreText, @"-?\d+");
            if (match.Success)
            {
                if (int.TryParse(match.Value, out int score))
                {
                    GameManager.Instance.UpdateNPC(currentNPCID, score, true);
                }
            }
            else
            {
                GameManager.Instance.UpdateNPC(currentNPCID, 0, true);
            }
        });
    }
    private void AddLineToHistory(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.Length < 3) return;

        currentConversationHistory.Add(line);
        if (currentConversationHistory.Count > maxMemoryLines * 2)
        {
            currentConversationHistory.RemoveAt(0);
        }
    }

    private string GetAttitudeString(int score)
    {
        if (score <= -51) return "Dislike/Hostile";
        if (score <= -1) return "Annoyed/Cold";
        if (score <= 200) return "Neutral/Indifferent";
        if (score <= 500) return "Slightly Friendly/Polite";
        if (score <= 1000) return "Acquaintance/Respectful";
        if (score <= 1500) return "Friendly/Warm";
        return "Best Friends/Extremely Loyal";
    }

    private void DisplayAIResponse(string text)
    {
        isDialogueActive = true;
        isShowingOptions = false;
        isAIResponding = true;
        lastFullResponse = text;

        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(TypeText(text));
    }

    void Update()
    {
        if (chatInputField != null && chatInputField.isFocused) return;
        if (isWaitingForAI) return;

        if (isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                NextLine();
            }
        }
        else if (isShowingOptions)
        {
            bool keyPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E);
            bool clickedEmptySpace = Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject();

            if (keyPressed || clickedEmptySpace)
            {
                CloseAllDialogue();
            }
        }
    }
}