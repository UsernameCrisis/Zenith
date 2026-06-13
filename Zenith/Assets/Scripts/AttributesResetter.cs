using UnityEngine;

public class AttributeResetter : MonoBehaviour
{
    void Start()
    {
        ResetGameManager();
        ResetPlayerOverworld();
    }

    private void ResetGameManager()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) { Debug.LogWarning("[AttributeResetter] GameManager instance not found."); return; }

        gm.playerHP = gm.playerMaxHP;
        gm.clericHP = gm.clericMaxHP;
        gm.warriorHP = gm.warriorMaxHP;
    }

    private void ResetPlayerOverworld()
    {
        PlayerOverworldAttributes player = FindAnyObjectByType<PlayerOverworldAttributes>();
        if (player == null) { Debug.LogWarning("[AttributeResetter] PlayerOverworldAttributes not found."); return; }

        player.currentHP = player.maxHP;
        player.RefreshHealthUI();
    }
}