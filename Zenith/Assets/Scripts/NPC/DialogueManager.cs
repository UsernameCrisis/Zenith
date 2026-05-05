using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

    private void ProcessAIResponse(string rawText)
    {
        string cleanMessage = "";
        int pointsGained = 0;
        bool parsedSuccessfully = false;

        var match = Regex.Match(rawText, @"\{.*\}", RegexOptions.Singleline);
        if (match.Success)
        {
            try
            {
                AIStructuredResponse res = JsonUtility.FromJson<AIStructuredResponse>(match.Value);
                cleanMessage = res.response;
                pointsGained = res.score;
                parsedSuccessfully = true;
            }
            catch { }
        }

        if (!parsedSuccessfully)
        {
            Match scoreMatch = Regex.Match(rawText, @"""score"":\s*(-?\d+)");
            if (scoreMatch.Success)
            {
                int.TryParse(scoreMatch.Groups[1].Value, out pointsGained);
            }

            cleanMessage = Regex.Replace(rawText, @"\{.*\}", "").Trim();
            if (string.IsNullOrEmpty(cleanMessage)) cleanMessage = rawText;
        }

        pointsGained = Mathf.Clamp(pointsGained, -1, 10);

        GameManager.Instance.UpdateNPC(currentNPCID, pointsGained, true);
        DisplayAIResponse(cleanMessage);
    }

    public void SendChatToAI()
    {
        string userText = chatInputField.text;
        if (string.IsNullOrEmpty(userText)) return;

        chatInputPanel.SetActive(false);
        chatInputField.text = "";
        isWaitingForAI = true;

        var npcData = GameManager.Instance.GetNPCData(currentNPCID);
        string attitude = GetAttitudeString(npcData.friendship);

        string formattedPrompt =
            $"Task: Respond as {currentNPCID} ({attitude}).\n" +
            $"Scale: -1 (mean), 0 (neutral), 1-10 (friendly/helpful).\n" +
            $"Format Example: {{\"response\": \"Hello friend!\", \"score\": 5}}\n" +
            $"Player said: \"{userText}\"\n" +
            $"Response JSON:";

        StartCoroutine(ollamaProvider.SendChatRequest(formattedPrompt, (rawAIOutput) => {
            isWaitingForAI = false;
            ProcessAIResponse(rawAIOutput);
        }));
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