using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    private List<Character> companions;
    void Start()
    {
        HP = 100;
        speed = 5;
        companions = new List<Character>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
