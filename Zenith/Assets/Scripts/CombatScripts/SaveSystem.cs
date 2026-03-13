using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string savePath = Application.persistentDataPath + "/grid_save.json";

    public static void Save(GridData grid)
    {
        GridSaveData saveData = grid.ToSaveData();
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Grid saved to {savePath}");
    }

    public static GridData Load(ObjectDatabaseSO database)
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("No save file found!");
            return new GridData();
        }

        string json = File.ReadAllText(savePath);
        GridSaveData saveData = JsonUtility.FromJson<GridSaveData>(json);
        GridData grid = GridData.FromSaveData(saveData, database);
        Debug.Log("✅ Grid loaded!");
        return grid;
    }

    public static bool HasSaveFile()
    {
        return File.Exists(savePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("🗑️ Save file deleted.");
        }
    }
}