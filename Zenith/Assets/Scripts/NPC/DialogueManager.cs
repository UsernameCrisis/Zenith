using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

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
    private int index;
    private bool isTyping;
    private bool isDialogueActive;
    private bool isShowingOptions;
    private bool canInteract = true;
    private Coroutine typeRoutine;

    private Vector2 leftTargetPos;
    private Vector2 rightTargetPos;
    private Vector2 textTargetPos;

    private void Awake()
    {
        leftTargetPos = leftPortraitRect.anchoredPosition;
        rightTargetPos = rightPortraitRect.anchoredPosition;
        textTargetPos = textPanelRect.anchoredPosition;
    }

    private void Start()
    {
        dialoguePanel.SetActive(false);
        optionsPanel.SetActive(false);
        isDialogueActive = false;
        isShowingOptions = false;
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
            dialogueText.text = currentDialogue.lines[index].text;
            isTyping = false;
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

        if (currentNPCID.ToLower() == "merchant")
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

    void Update()
    {
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