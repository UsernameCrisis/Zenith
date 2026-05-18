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
    public GameObject inventoryUIPanel;
    public GameObject shopUIPanel;

    [Header("UI Containers")]
    public GameObject dialoguePanel;
    public GameObject optionsPanel;
    public RectTransform leftPortraitRect;
    public RectTransform rightPortraitRect;
    public RectTransform textPanelRect;
    public FriendshipUI friendshipUI;

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
        if (friendshipUI != null) friendshipUI.DisplayFriendship(npcID);

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
        if (friendshipUI != null) friendshipUI.HideFriendship();
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

        if (data.dailyTalks >= 5)
        {
            optionsPanel.SetActive(false);
            DisplayAIResponse($"{currentNPCID} doesn't seem to want to talk for now.");
            return;
        }

        optionsPanel.SetActive(false);
        chatInputPanel.SetActive(true);
        chatInputField.ActivateInputField();
    }

    public void OnGiftButtonPressed()
    {
        optionsPanel.SetActive(false);
        InventoryManager.Instance.isGifting = true;
        inventoryUIPanel.SetActive(true);
    }

    public void OnPartyClick()
    {
        var npcData = GameManager.Instance.GetNPCData(currentNPCID);
        bool alreadyInParty = IsInParty(currentNPCID);

        string styleOpen = "<color=#AAAAAA><i>*";
        string styleClose = "*</i></color>";

        if (alreadyInParty)
        {
            SetPartyStatus(currentNPCID, false);
            dialogueText.text = $"{styleOpen}{currentNPCID} has left your party.{styleClose}";
        }
        else if (npcData.friendship >= 1000)
        {
            SetPartyStatus(currentNPCID, true);
            dialogueText.text = $"{styleOpen}{currentNPCID} has joined your party!{styleClose}";
        }
        else
        {
            dialogueText.text = GetRejectionText(currentNPCID);
        }

        isAIResponding = true;
        optionsPanel.SetActive(false);
    }

    public void OnShopButtonPressed()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.isGifting = false;
            InventoryManager.Instance.isShopping = true;
        }

        isShowingOptions = false;
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        optionsPanel.SetActive(false);

        shopUIPanel.SetActive(true);
        inventoryUIPanel.SetActive(true);
    }

    public void CloseShopUI()
    {
        if (shopUIPanel != null)
        {
            shopUIPanel.SetActive(false);
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.isShopping = false;
        }

        if (player != null)
        {
            player.canMove(true);
        }

        StartCoroutine(InteractionCooldown());
    }

    public void ReceiveGift(BaseItem item)
    {
        isWaitingForAI = true;
        isAIResponding = true;
        nameText.text = currentNPCID;
        dialogueText.text = "...";

        var npcData = GameManager.Instance.GetNPCData(currentNPCID);
        string attitude = GetAttitudeString(npcData.friendship);

        var identity = GetNPCIdentity(currentNPCID, item.itemName + " " + item.description);

        string giftPrompt =
            $"### SYSTEM IDENTITY:\n" +
            $"Name: {currentNPCID}\n" +
            $"Persona: {identity.role}\n" +
            $"Current Attitude: {attitude}\n\n" +
            $"### CONTEXT:\n" +
            $"The player has just gifted you: {item.itemName}.\n" +
            $"Item Description: {item.description}\n\n" +
            $"### INJECTED KNOWLEDGE (Likes/Dislikes):\n" +
            $"{identity.lore}\n\n" +
            $"### RULES:\n" +
            $"- React to the gift in one short, natural sentence.\n" +
            $"- Be honest: if the lore says you hate this, be annoyed. If you love it, be happy.\n" +
            $"- DO NOT mention 'stats', 'sell value', or 'items description' technicalities.\n" +
            $"- {currentNPCID}:";

        StartCoroutine(ollamaProvider.SendChatRequest(giftPrompt, (aiResponse) => {
            isWaitingForAI = false;
            ProcessGiftResponse(aiResponse, item, identity.lore);
        }));
    }

    private void ProcessGiftResponse(string aiResponse, BaseItem item, string lore)
    {
        string cleanAIResponse = aiResponse.Trim();
        DisplayAIResponse(cleanAIResponse);

        AddLineToHistory($"Player gave a {item.itemName}");
        AddLineToHistory($"{currentNPCID}: {cleanAIResponse}");

        StartCoroutine(GetGiftFriendshipRating(item, cleanAIResponse, lore));
    }

    private IEnumerator GetGiftFriendshipRating(BaseItem item, string aiMsg, string lore)
    {
        string ratingPrompt =
            $"### CONTEXT:\n" +
            $"NPC Lore: {lore}\n" +
            $"Gift: {item.itemName}\n" +
            $"Reaction given: '{aiMsg}'\n\n" +
            $"### TASK:\n" +
            $"Rate how much {currentNPCID} likes the gift based on the reaction and lore. " +
            $"Scale: -5 (hates it) to 50 (loves it).\n" +
            $"Return ONLY the integer. If no number is found or you are unsure, return 5.";

        yield return ollamaProvider.SendChatRequest(ratingPrompt, (scoreText) => {
            int baseScore = 5;

            Match match = Regex.Match(scoreText, @"-?\d+");
            if (match.Success && int.TryParse(match.Value, out int parsedScore))
            {
                baseScore = Mathf.Clamp(parsedScore, -5, 50);
            }
            else
            {
                Debug.LogWarning($"AI gave a 'drunk' response ({scoreText}). Failsafe triggered: Score set to 5.");
            }

            float multiplier = CalculateValueMultiplier(item.sellPrice);
            int finalScore = Mathf.RoundToInt(baseScore * multiplier);

            Debug.Log($"[GIFT SYSTEM] {item.itemName} | Base: {baseScore} | Multiplier: {multiplier:F2} | Final: {finalScore}");
            GameManager.Instance.UpdateNPC(currentNPCID, finalScore, false);
            if (friendshipUI != null) friendshipUI.DisplayFriendship(currentNPCID);
        });
    }

    private float CalculateValueMultiplier(int price)
    {
        if (price < 50) return 1.0f;
        if (price >= 500) return 5.0f;

        float normalizedValue = (price - 50f) / 450f;
        return 1f + (4f * Mathf.Pow(normalizedValue, 0.7f));
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
            if (friendshipUI != null) friendshipUI.DisplayFriendship(currentNPCID);
        });
    }

    private bool IsInParty(string npcID)
    {
        if (npcID == "Thorek") return GameManager.Instance.warriorInParty;
        if (npcID == "Iris") return GameManager.Instance.clericInParty;
        return false;
    }

    private void SetPartyStatus(string npcID, bool status)
    {
        Debug.Log($"<color=orange>[Party Check]</color> SetPartyStatus called for: '{npcID}' with status: {status}");

        bool matchFound = false;

        if (npcID == "Thorek")
        {
            GameManager.Instance.warriorInParty = status;
            Debug.Log("<color=green>[Party Success]</color> Thorek's warriorInParty bool updated!");
            matchFound = true;
        }

        if (npcID == "Iris")
        {
            GameManager.Instance.clericInParty = status;
            Debug.Log("<color=green>[Party Success]</color> Iris's clericInParty bool updated!");
            matchFound = true;
        }

        if (!matchFound)
        {
            Debug.LogError($"<color=red>[Party Error]</color> No ID match found for '{npcID}'. Check for typos or extra spaces!");
        }

        Debug.Log($"<color=cyan>[Party Final State]</color> {npcID} in party is now: {status}");
    }

    private string GetRejectionText(string npcID)
    {
        string styleOpen = "<color=#AAAAAA><i>*";
        string styleClose = "*</i></color>";

        string[] lines = {
            $"{npcID} doesn't seem to trust you enough to risk their life.",
            $"{npcID} politely declines; your bond isn't quite strong enough yet.",
            $"{npcID} watches you with skepticism. You'll need more influence to recruit them.",
            $"You haven't earned {npcID}'s loyalty yet. Keep talking and gifting!"
        };

        return styleOpen + lines[Random.Range(0, lines.Length)] + styleClose;
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

        // --- DEV CHEAT KEY BINDINGS ---
        // Only allow cheating when we have a valid current NPC conversation active
        if (!string.IsNullOrEmpty(currentNPCID))
        {
            // Press UP ARROW to add +25 friendship
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                GameManager.Instance.UpdateNPC(currentNPCID, 25, true);
                if (friendshipUI != null) friendshipUI.DisplayFriendship(currentNPCID);
                Debug.Log($"[CHEAT] Added 25 friendship to {currentNPCID}");
            }

            // Press DOWN ARROW to subtract -25 friendship
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                GameManager.Instance.UpdateNPC(currentNPCID, -25, true);
                if (friendshipUI != null) friendshipUI.DisplayFriendship(currentNPCID);
                Debug.Log($"[CHEAT] Subtracted 25 friendship from {currentNPCID}");
            }
        }
        // ------------------------------

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