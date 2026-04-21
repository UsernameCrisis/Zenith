using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static OllamaInspectorTester;

public class OllamaChatProvider : MonoBehaviour
{
    [Header("Server Settings")]
    public string url = "http://localhost:11434/api/generate";
    public string model = "phi:latest";

    public IEnumerator SendChatRequest(string userPrompt, Action<string> onResponseReceived)
    {
        OllamaRequest data = new OllamaRequest
        {
            model = this.model,
            prompt = userPrompt,
            stream = false
        };

        string json = JsonUtility.ToJson(data);

        Debug.Log("Sending JSON: " + json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawResponse = request.downloadHandler.text;
                Debug.Log($"Raw JSON from Ollama: {rawResponse}");

                try
                {
                    OllamaResponse res = JsonUtility.FromJson<OllamaResponse>(rawResponse);
                    onResponseReceived?.Invoke(res.response);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse JSON: {e.Message}");
                    onResponseReceived?.Invoke("...I understood you, but my thoughts are messy.");
                }
            }
        }
    }

    [Serializable]
    public class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }
}