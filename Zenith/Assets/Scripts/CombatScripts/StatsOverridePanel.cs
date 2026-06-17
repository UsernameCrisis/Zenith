using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StatOverridePanel : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private ObjectDatabaseSO database;

    [Header("UI Wiring")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform characterListParent;
    [SerializeField] private GameObject characterSectionPrefab; // prefab with a header label + a rows container
    [SerializeField] private StatSliderRow statRowPrefab;    // single slider+inputfield row, instantiated 4x per character
    [SerializeField] private Button editStatsToggleButton;
    [SerializeField] private Button resetAllButton;

    [Header("Slider Ranges")]
    [SerializeField] private int maxHP = 215;
    [SerializeField] private int maxDamage = 55;
    [SerializeField] private int maxSpeed = 20;
    [SerializeField] private int maxDefense = 20;

    private static readonly int[] AllCharacterIDs = { 0, 1, 2, 3, 4, 5 };

    private class CharacterRows
    {
        public int characterID;
        public ObjectData defaultData;
        public StatSliderRow hpRow;
        public StatSliderRow damageRow;
        public StatSliderRow speedRow;
        public StatSliderRow defenseRow;
    }

    private List<CharacterRows> allRows = new();
    private bool isPanelOpen = false;

    void Awake()
    {
        editStatsToggleButton.onClick.AddListener(TogglePanel);
        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllToDefaults);
    }

    void Start()
    {
        BuildAllCharacterRows();
        panelRoot.SetActive(false);
    }

    private void BuildAllCharacterRows()
    {
        foreach (int id in AllCharacterIDs)
        {
            ObjectData data = database.objectsData.Find(d => d.ID == id);
            if (data == null)
            {
                Debug.LogWarning($"[StatOverridePanel] No ObjectData found for ID {id} — skipping.");
                continue;
            }

            GameObject section = Instantiate(characterSectionPrefab, characterListParent);

            var header = section.GetComponentInChildren<TextMeshProUGUI>();
            if (header != null)
                header.text = data.Name;

            Transform rowsContainer = section.transform.Find("RowsContainer");
            if (rowsContainer == null)
            {
                Debug.LogError("[StatOverridePanel] characterSectionPrefab is missing a 'RowsContainer' child!");
                continue;
            }

            CharacterRows rows = new CharacterRows
            {
                characterID = id,
                defaultData = data,
                hpRow = CreateRow(rowsContainer, "HP", 0, maxHP, data.HP),
                damageRow = CreateRow(rowsContainer, "Damage", 0, maxDamage, data.Damage),
                speedRow = CreateRow(rowsContainer, "Speed", 0, maxSpeed, data.Speed),
                defenseRow = CreateRow(rowsContainer, "Defense", 0, maxDefense, data.Defense)
            };

            allRows.Add(rows);
        }
    }

    private StatSliderRow CreateRow(Transform parent, string label, int min, int max, int defaultValue)
    {
        StatSliderRow row = Instantiate(statRowPrefab, parent);
        row.Initialize(label, min, max, defaultValue);
        return row;
    }

    private void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        panelRoot.SetActive(isPanelOpen);
    }

    private void ResetAllToDefaults()
    {
        foreach (var rows in allRows)
        {
            rows.hpRow.ResetToDefault(rows.defaultData.HP);
            rows.damageRow.ResetToDefault(rows.defaultData.Damage);
            rows.speedRow.ResetToDefault(rows.defaultData.Speed);
            rows.defenseRow.ResetToDefault(rows.defaultData.Defense);
        }
    }

    public Dictionary<int, StatOverride> GetOverrides()
    {
        var result = new Dictionary<int, StatOverride>();

        foreach (var rows in allRows)
        {
            result[rows.characterID] = new StatOverride
            {
                hp = rows.hpRow.Value,
                damage = rows.damageRow.Value,
                speed = rows.speedRow.Value,
                defense = rows.defenseRow.Value
            };
        }

        return result;
    }
}

[System.Serializable]
public struct StatOverride
{
    public int hp;
    public int damage;
    public int speed;
    public int defense;
}
