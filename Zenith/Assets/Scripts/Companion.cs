using UnityEngine;

public class Companion : Character
{
    protected override void die()
    {
        throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speed = 4;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
