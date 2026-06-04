using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Settings")]
    [SerializeField] private float dotCycleInterval = 0.2f;
    [SerializeField] private float completionPause = 0.3f;

    private Coroutine dotsCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        loadingCanvas.SetActive(false);
    }
    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        StartCoroutine(LoadRoutine(sceneName, mode));
    }

    public static void Load(string sceneName, 
                        LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (Instance != null)
        {
            Instance.LoadScene(sceneName, mode);
        }
        else
        {
            Debug.LogWarning("LoadingScreenManager: no instance found, " +
                            "falling back to direct scene load.");
            SceneManager.LoadScene(sceneName, mode);
        }
    }

    private IEnumerator LoadRoutine(string sceneName, LoadSceneMode mode)
    {
        loadingCanvas.SetActive(true);

        loadingText.text = "Loading";
        dotsCoroutine = StartCoroutine(AnimateDots());
        yield return new WaitForSecondsRealtime(0.1f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, mode);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }

        op.allowSceneActivation = true;

        yield return new WaitUntil(() => op.isDone);

        if (dotsCoroutine != null)
        {
            StopCoroutine(dotsCoroutine);
            dotsCoroutine = null;
        }

        loadingText.text = "Loading...";

        yield return new WaitForSeconds(completionPause);

        loadingCanvas.SetActive(false);
    }

    private IEnumerator AnimateDots()
    {
        int dotCount = 0;

        while (true)
        {
            string dots = new string('.', dotCount);
            loadingText.text = "Loading" + dots;

            dotCount = (dotCount + 1) % 4;

            yield return new WaitForSecondsRealtime(dotCycleInterval);
        }
    }
}
