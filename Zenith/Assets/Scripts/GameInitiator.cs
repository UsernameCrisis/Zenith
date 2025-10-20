using System;
using System.Threading.Tasks;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitiator : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private SceneSwapManager SceneSwapManager;
    [SerializeField] private LoadingScreen loadingSceneScript;
    [SerializeField] private SceneField FirstScene;
    [SerializeField] private SpawnsDamagePopups _spawnsDamagePopups;
    private Camera _mainCamera;

    private async void Start()
    {
        BindObjects();
        loadingSceneScript.show(_mainCamera);
        SceneSwapManager.SWAP_SCENE(FirstScene);
        loadingSceneScript.disable();
    }

    private void BindObjects()
    {
        _player = Instantiate(_player);
        _mainCamera = Player.Instance.GetComponentInChildren<Camera>();
        _mainCamera = Instantiate(_mainCamera);
        DontDestroyOnLoad(_mainCamera);
        _mainCamera.GetComponent<CameraMovement>().Initialize();
    }
}
