using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int playerHP = 90;
    public int playerMaxHP = 100;
    public int gold = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
