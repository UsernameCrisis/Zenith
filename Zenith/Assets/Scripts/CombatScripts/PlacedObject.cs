using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class PlacedObject
{
    public string Name { get; protected set; }
    public Vector3Int Position { get; set; }
    public ObjectType ObjectType { get; protected set; }

    public virtual void OnPlaced(Vector3Int pos)
    {
        Position = pos;
    }

    public virtual void OnRemoved() { }
    // Do something when the placed object is removed
}

public enum ObjectType
{
    Static,
    RandomProp,
    Character
}

public class StaticObject : PlacedObject
{
    public StaticObject(string name)
    {
        Name = name;
        ObjectType = ObjectType.Static;
    }
}

public class RandomObject : PlacedObject
{
    public RandomObject(string name)
    {
        Name = name;
        ObjectType = ObjectType.RandomProp;
    }
}

[Serializable]
public class CharacterObject : PlacedObject
{
    public int HP { get; private set; }
    public int ID { get; private set; }
    public int MaxHp { get; private set; }
    public int Damage { get; private set; }
    public int Defense { get; private set; }
    public int Speed { get; private set; }
    public float CurrentATB { get; private set; }
    public Sprite Portrait { get; private set; }
    public int Team { get; private set; }
    public bool IsPlayer { get; private set; }
    public int MaxMoveRange { get; private set; } = 3;
    public int RemainingMoveRange { get; private set; }
    public int AtkRange { get; private set; }
    public GameObject GameObject { get; private set; }
    public event Action<int, int> OnHPChanged;
    public event Action<int> OnTakenDamage;
    public event Action<CharacterObject> OnDied;
    private bool canAttack = true;

    public CharacterObject(string name, int id, int hp, int damage, int defense, int speed, 
                            float currentATB, Sprite portrait, int team, int atkRange, bool isPlayer)
    {
        Name = name;
        ID = id;
        HP = hp;
        MaxHp = hp;
        Damage = damage;
        Defense = defense;
        CurrentATB = currentATB;
        Speed = speed;
        Team = team;
        IsPlayer = isPlayer;
        ObjectType = ObjectType.Character;
        RemainingMoveRange = MaxMoveRange;
        Portrait = portrait;
        AtkRange = atkRange;
    }

    public void AddATB(float addition) => CurrentATB += addition;
    public void SubATB(float subtraction) => CurrentATB -= subtraction;

    // Call at the start of each turn
    public void ResetMovement() => RemainingMoveRange = MaxMoveRange;

    public void ResetState()
    {
        HP = MaxHp;
        CurrentATB = 0f;
        RemainingMoveRange = MaxMoveRange;
        canAttack = true;
    }

    public void UseMovement(int distance)
    {
        RemainingMoveRange = Mathf.Max(0, RemainingMoveRange - distance);
    }

    public bool CanStillMove => RemainingMoveRange > 0;

    public void EnableAttack() => canAttack = true;
    public void DisableAttack() => canAttack = false;
    public bool CanStillAttack() => canAttack;
    public void BindGameObject(GameObject go) => GameObject = go;
    public UnitView View => GameObject != null ? GameObject.GetComponentInChildren<UnitView>() : null;

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(amount - Defense, 1);
        HP -= finalDamage;
        HP = Mathf.Max(HP, 0);

        OnHPChanged?.Invoke(HP, MaxHp);
        OnTakenDamage?.Invoke(finalDamage);

        Debug.Log($"{Name} took {finalDamage} damage! (Raw: {amount}, Defense: {Defense}) Remaining HP: {HP}");

        if (HP <= 0)
            OnDeath();
    }

    public void Heal(int amount)
    {
        HP = Mathf.Clamp(HP + amount, 0, MaxHp);
        OnHPChanged?.Invoke(HP, MaxHp);
    }
    
    public void Attack(CharacterObject target)
    {
        Debug.Log($"{Name} attacks {target.Name} for {Damage} damage!");
        target.TakeDamage(Damage);
    }

    public override void OnPlaced(Vector3Int pos)
    {
        base.OnPlaced(pos);
        Debug.Log($"{Name} placed at {pos}");
    }
    protected virtual void OnDeath()
    {
        Debug.Log($"{Name} has died.");

        OnDied?.Invoke(this);
    }
}