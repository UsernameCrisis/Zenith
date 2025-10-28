using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.VisualScripting;
public class SaveSystem
{
    public static SaveData _saveData = new SaveData();
    [System.Serializable]
    public struct SaveData
    {
        public PlayerSaveData PlayerSaveData;
    }

    public static string SaveFileName()
    {
        string saveFile = Application.persistentDataPath + "/save" + ".save";
        return saveFile;
    }

    public static void Save()
    {
        HandleSaveData();

        File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));
    }

    public static void HandleSaveData()
    {
        GameManager.Instance.Player.save(ref _saveData.PlayerSaveData);
    }

    public static void Load()
    {
        string saveContent = File.ReadAllText(SaveFileName());

        _saveData = JsonUtility.FromJson<SaveData>(saveContent);
        HandleLoadData();
    }
    
    public static void HandleLoadData()
    {
        GameManager.Instance.Player.load(_saveData.PlayerSaveData);
    }
}
