using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class OllamaInspectorTester : MonoBehaviour
{
    [Header("Ollama Server Settings")]
    [SerializeField] private string url = "http://localhost:11434/api/generate";
    [SerializeField] private string model = "phi:latest";

    [Header("Generation Settings")]
    [SerializeField, TextArea(3, 10)] private string prompt = "Describe a slime enemy in one sentance";
    [SerializeField] private bool stream = false;
    [SerializeField] private string format = "";

    [ContextMenu("Send Test Prompt")]
    public void TestRequest()
    {
        StartCoroutine(SendPrompt(prompt));
    }

    [System.Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
        public string format;
    }

    [System.Serializable]
    public class OllamaResponse
    {
        public string response;
    }

    public IEnumerator SendPrompt(string userPrompt)
    {
        Debug.Log($"Sending request to {model}...");

        OllamaRequest data = new OllamaRequest
        {
            model = this.model,
            prompt = userPrompt,
            stream = this.stream,
            format = this.format
        };

        string json = JsonUtility.ToJson(data);
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OllamaResponse res = JsonUtility.FromJson<OllamaResponse>(request.downloadHandler.text);
                Debug.Log($"<color=green><b>{model} Response:</b></color> {res.response}");
            }
            else
            {
                Debug.LogError($"Error: {request.error} | Response: {request.downloadHandler.text}");
            }
        }
    }
}