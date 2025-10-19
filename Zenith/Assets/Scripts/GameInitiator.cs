using System;
using System.Threading.Tasks;
using UnityEngine;

public class GameInitiator : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private LoadingScreen _loadingScreen;
    private async void Start()
    {
        BindObjects();
        _loadingScreen.show(_mainCamera);
        await InitializePlayer();
        // await InitializeFirstScene();
    }

    private async Task InitializeFirstScene()
    {
        throw new NotImplementedException();
    }

    private async Task InitializePlayer()
    {
        _player.Initialize();
    }

    private void BindObjects()
    {
        _mainCamera = Instantiate(_mainCamera);
        _player = Instantiate(_player);
        _loadingScreen = Instantiate(_loadingScreen);
    }
}
