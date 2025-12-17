using UnityEngine;
using System.Collections.Generic;

public class TurnOrderUI : MonoBehaviour
{
    [SerializeField] private TurnIconUI iconPrefab;
    [SerializeField] private Transform container;
    private List<TurnIconUI> icons = new();

    public void Refresh(List<CharacterObject> upcoming)
    {
        while (icons.Count < upcoming.Count)
        {
            var icon = Instantiate(iconPrefab, container);
            icons.Add(icon);
        }

        // Update icons
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
    }
}
