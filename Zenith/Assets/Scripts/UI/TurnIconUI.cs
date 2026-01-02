using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TurnIconUI : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 basePosition;
    [SerializeField] private Image portrait;
    [SerializeField] private Image frame;
    public bool WasAnimatedThisFrame { get; private set; }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Set(CharacterObject character)
    {
        WasAnimatedThisFrame = false;
        portrait.sprite = character.Portrait;
        // frame.color = character.IsPlayer ? Color.cyan : Color.red;
    }

    public void SetBasePosition(Vector2 pos)
    {
        basePosition = pos;
        rect.anchoredPosition = pos;
    }

    public void AnimateIn(float offscreenX, float duration = 0.3f)
    {
        if (rect == null) return;
        rect.DOKill();
        Vector2 finalPos = rect.anchoredPosition;

        rect.anchoredPosition = new Vector2(offscreenX, finalPos.y);

        rect
            .DOAnchorPos(finalPos, duration)
            .SetEase(Ease.OutCubic).SetDelay(0.05f)
            .OnComplete(() => WasAnimatedThisFrame = true);
    }

    public void SetActiveTurn(bool isActive, float duration = 0.2f)
    {
        if (rect == null) return;
        rect.DOKill();

        float targetScale = isActive ? 1.15f : 1f;
        float targetXOffset = isActive ? 20f : 0f;
        float targetYOffset = isActive ? 20f : 0f;

        rect.DOScale(targetScale, duration).SetEase(Ease.OutCubic);

        rect.DOAnchorPos(
            basePosition + new Vector2(targetXOffset, targetYOffset),
            duration
        ).SetEase(Ease.OutCubic);
    }

    private void OnDisable()
    {
        rect.DOKill();
    }

    private void OnDestroy()
    {
        if (rect != null)
            rect.DOKill();
    }
}
