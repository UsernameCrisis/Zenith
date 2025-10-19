using System;
using System.Threading.Tasks;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitiator : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private SceneSwapManager SceneSwapManager;
    [SerializeField] private LoadingScreen loadingSceneScript;
    [SerializeField] private SceneField FirstScene;
    private async void Start()
    {
        BindObjects();
        loadingSceneScript.show(_mainCamera);
        await InitializePlayer();
        SceneSwapManager.SWAP_SCENE(FirstScene);
        loadingSceneScript.disable();
    }

    private async Task InitializePlayer()
    {
        _player.Initialize();
    }

    private void BindObjects()
    {
        _mainCamera = Instantiate(_mainCamera);
        _player = Instantiate(_player);
        SceneSwapManager = Instantiate(SceneSwapManager);
    }
}
