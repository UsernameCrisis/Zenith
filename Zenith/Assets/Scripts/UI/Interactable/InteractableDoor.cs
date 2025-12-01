using UnityEngine;
using System.Collections;

public class InteractableDoor : InteractableObject, Interactable
{
    public enum DoorType { Entry, Exit, Spawn }

    [Header("Door Setup")]
    public DoorType doorType;
    public string roomID;
    public InteractableDoor linkedDoor;
    public Transform exitPosition;

    [Header("Cooldown Settings")]
    public float cooldownDuration = 5f;
    private bool isOnCooldown = false;

    [Header("Screen Fade")]
    public CanvasGroup fadeOverlay;
    public float fadeOutDuration = 0.3f;
    public float fadeInDuration = 0.8f;

    private GameObject player;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement>();

        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    public override void OnInteract()
    {
        if (isOnCooldown || linkedDoor == null || player == null) return;

        StartCoroutine(StartCooldown());
        linkedDoor.StartCoroutine(linkedDoor.StartCooldown());
        StartCoroutine(TeleportWithFade());
    }

    private IEnumerator TeleportWithFade()
    {
        if (playerMovement != null)
            playerMovement.canMove(false);
        yield return Fade(1f, fadeOutDuration);
        player.transform.position = linkedDoor.exitPosition.position;
        if (playerMovement != null)
            playerMovement.canMove(true);
        yield return Fade(0f, fadeInDuration);
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;
        float startAlpha = fadeOverlay.alpha;
        float time = 0f;
        bool goingDark = targetAlpha > startAlpha;
        fadeOverlay.blocksRaycasts = goingDark;
        fadeOverlay.interactable = goingDark;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        fadeOverlay.alpha = targetAlpha;
        if (Mathf.Approximately(targetAlpha, 0f))
        {
            fadeOverlay.blocksRaycasts = false;
            fadeOverlay.interactable = false;
        }
    }

    private IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        isOnCooldown = false;
    }
}
