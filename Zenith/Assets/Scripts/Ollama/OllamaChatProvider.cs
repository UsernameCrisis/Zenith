using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OllamaChatProvider : MonoBehaviour
{
    [Header("Default Settings (Fallback)")]
    [SerializeField] private string defaultUrl = "http://localhost:11434/api/generate";
    [SerializeField] private string defaultConvModel = "phi:latest";
    [SerializeField] private string defaultCombatModel = "phi:latest";

    [Header("Current Runtime Settings")]
    public string activeUrl;
    public string conversationModel;
    public string combatModel;

    private void Awake()
    {
        LoadSettings();
    }

    public void LoadSettings()
    {
        activeUrl = PlayerPrefs.GetString("Ollama_URL", defaultUrl);
        conversationModel = PlayerPrefs.GetString("Ollama_ConvModel", defaultConvModel);
        combatModel = PlayerPrefs.GetString("Ollama_CombatModel", defaultCombatModel);
    }

    public void SaveSettings(string newUrl, string newConv, string newCombat)
    {
        PlayerPrefs.SetString("Ollama_URL", newUrl);
        PlayerPrefs.SetString("Ollama_ConvModel", newConv);
        PlayerPrefs.SetString("Ollama_CombatModel", newCombat);
        PlayerPrefs.Save();

        LoadSettings();
    }

    public IEnumerator SendChatRequest(string userPrompt, Action<string> onResponseReceived, bool isCombat = false)
    {
        string modelToUse = isCombat ? combatModel : conversationModel;

        OllamaRequest data = new OllamaRequest
        {
            model = modelToUse,
            prompt = userPrompt,
            stream = false
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(activeUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    OllamaResponse res = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
                    onResponseReceived?.Invoke(res.response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"JSON Parse Error: {e.Message}");
                    onResponseReceived?.Invoke("...My thoughts are messy.");
                }
            }
            else
            {
                Debug.LogError($"Ollama Error: {request.error}");
                onResponseReceived?.Invoke("Connection to the brain failed.");
            }
        }
    }

    [Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
        public Options options = new Options();
    }

    [Serializable]
    public class Options
    {
        public float temperature = 0.3f;
        public int num_predict = 100;
    }

    [Serializable]
    public class OllamaResponse { public string response; }

    [Serializable]
    public class AIStructuredResponse
    {
        public string response;
        public int score;
    }
}