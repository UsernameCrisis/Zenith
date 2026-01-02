using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField] private TurnIconUI iconPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private float offsetX = -50, offsetY = -10;
    [SerializeField] private float iconSpacing = 90f;
    [SerializeField] private float animDuration = 0.25f;
    private List<TurnIconUI> icons = new();
    private List<CharacterObject> lastVisible = new();
    
    public void Refresh(List<CharacterObject> upcoming)
    {
        if (icons.Count == 0)
        {
            _buildIcon(upcoming);
            return;
        }
        
        bool isSlidingWindow = lastVisible.Count == upcoming.Count;

        if (isSlidingWindow)
        {
            for (int i = 0; i < upcoming.Count - 1; i++)
            {
                if (upcoming[i] != lastVisible[i + 1])
                {
                    isSlidingWindow = false;
                    break;
                }
            }
        }

        if (!isSlidingWindow)
        {
            Rebuild(upcoming);
            return;
        }

        // Shift keatas
        for (int i = 0; i < icons.Count; i++)
        {
            
            RectTransform rect = icons[i].GetComponent<RectTransform>();
            Vector2 start = rect.anchoredPosition;
            Vector2 targetPos = GetSlotPosition(i - 1);
            
            icons[i].SetBasePosition(targetPos);
            rect.anchoredPosition = start;

            rect.DOAnchorPos(targetPos, animDuration)
                .SetEase(Ease.OutCubic);
        }

        TurnIconUI recycled = icons[0];
        icons.RemoveAt(0);
        icons.Add(recycled);
        recycled.Set(upcoming[upcoming.Count - 1]);

        // Taruh offscreen
        RectTransform recycledRect = recycled.GetComponent<RectTransform>();
        recycled.SetBasePosition(GetSlotPosition(upcoming.Count - 1));

        // Animasi new icon
        recycled.AnimateIn(offscreenX: -300f, duration: animDuration);

        for (int i = 0; i < icons.Count; i++)
        {
            bool isCurrent = i == 0;
            icons[i].SetActiveTurn(isCurrent);
        }

        lastVisible.Clear();
        lastVisible.AddRange(upcoming);
    }

    private Vector2 GetSlotPosition(int index)
    {
        return new Vector2(offsetX, offsetY + -index * iconSpacing);
    }

    private void Rebuild(List<CharacterObject> upcoming)
    {
        foreach (var icon in icons)
            Destroy(icon.gameObject);

        icons.Clear();
        lastVisible.Clear();

        _buildIcon(upcoming);
    }

    private void _buildIcon(List<CharacterObject> upcoming)
    {
        for (int i = 0; i < upcoming.Count; i++)
            {
                var icon = Instantiate(iconPrefab, container);
                icon.Set(upcoming[i]);

                Vector2 slotPos = GetSlotPosition(i);
                icon.SetBasePosition(slotPos);

                icons.Add(icon);
            }

            for (int i = 0; i < icons.Count; i++)
            {
                icons[i].SetActiveTurn(i == 0);
            }

            lastVisible.AddRange(upcoming);
    }

    public void oldRefresh(List<CharacterObject> upcoming)
    {
        while (icons.Count < upcoming.Count)
        {
            var icon = Instantiate(iconPrefab, container);
            icon.gameObject.SetActive(false);
            icons.Add(icon);
        }

        for (int i = 0; i < icons.Count; i++)
        {
            if (i < upcoming.Count)
            {
                icons[i].gameObject.SetActive(true);
                icons[i].Set(upcoming[i]);
            }
            else
            {
                icons[i].gameObject.SetActive(false);
            }
        }

        Dictionary<TurnIconUI, Vector2> oldPositions = new();

        for (int i = 0; i < upcoming.Count; i++)
        {
            if (icons[i].gameObject.activeSelf)
                oldPositions[icons[i]] = icons[i].GetComponent<RectTransform>().anchoredPosition;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);

        foreach (var pair in oldPositions)
        {
            RectTransform rect = pair.Key.GetComponent<RectTransform>();
            Vector2 oldPos = pair.Value;
            Vector2 newPos = rect.anchoredPosition;
        
            if (oldPos != newPos)
            {
                rect.anchoredPosition = oldPos;
                rect.DOAnchorPos(newPos, 0.25f).SetEase(Ease.OutCubic);
            }
        }

        if (lastVisible.Count > 0 && upcoming.Count == lastVisible.Count)
        {
            bool isSlidingWindow = true;

            for (int i = 0; i < upcoming.Count - 1; i++)
            {
                if (upcoming[i] != lastVisible[i + 1])
                {
                    isSlidingWindow = false;
                    break;
                }
            }

            if (isSlidingWindow)
            {
                icons[upcoming.Count - 1].AnimateIn(-300f);
            }
        }
        lastVisible.Clear();
        lastVisible.AddRange(upcoming);
    }
}
