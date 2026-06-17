using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ObjectDatabaseSO : ScriptableObject
{
    public List<ObjectData> objectsData;
}

[Serializable]
public class ObjectData
{
    [field: SerializeField]
    public string Name { get; private set; }
    [field: SerializeField]
    public int ID { get; private set; }
    [field: SerializeField]
    public int HP { get; private set; }
    [field: SerializeField]
    public int Damage { get; private set; }
    [field: SerializeField]
    public int Speed { get; private set; }
    [field: SerializeField]
    public int Defense { get; private set; }
    [field: SerializeField]
    public float currentATB { get; private set; }
    [field: SerializeField]
    public ObjectType Type { get; private set; }
    [field: SerializeField]
    public bool IsPlayer { get; private set; }
    [field: SerializeField]
    public int Team { get; private set; }
    [field: SerializeField]
    public int AtkRange { get; private set; }
    [field: SerializeField]
    public GameObject Prefab { get; private set; }
    [field: SerializeField]
    public Sprite Portrait { get; private set; }
    public void setDamage(int damage)
    {
        Damage = damage;
    }

    public void setDefense(int def)
    {
        Defense = def;
    }

    public void setHP(int hp)
    {
        HP = hp;
    }

    public void setSpeed (int speed)
    {
        Speed = speed;
    }
} 