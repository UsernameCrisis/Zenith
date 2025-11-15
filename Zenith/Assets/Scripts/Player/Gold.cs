using System;
using TMPro;
using UnityEngine;

public class Gold : MonoBehaviour
{
    private PlayerOverworldAttributes player;
    public TMP_Text text;
    void Start()
    {
        player = FindAnyObjectByType<PlayerOverworldAttributes>();
        if (text == null) return;
        text.text = FormatGold(player.gold);
    }

    void Update()
    {
        if (text == null) return;
        text.text = FormatGold(player.gold); //TODO move
    }

    public string FormatGold(int amount)
    {
        if (amount < 1000)
            return amount.ToString();

        if (amount < 1000000)
            return (amount / 1000f).ToString("0.#") + "K";

        if (amount < 1000000000)
            return (amount / 1000000f).ToString("0.#") + "M";

        return "999m";
    }
}
