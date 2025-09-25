using UnityEngine;

public class Enemy : Character
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HP = 50;
        speed = 3;
    }

    // Update is called once per frame
    void Update()
    {
    }
    protected override void die()
    {
        throw new System.NotImplementedException();
    }
}
