using UnityEngine;

public abstract class PlacedObject
{
    public string Name { get; protected set; }
    public Vector3Int Position { get; set; }

    // Optional: what type of object this is (for filtering)
    public ObjectType ObjectType { get; protected set; }

    // Common behavior
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

[System.Serializable]
public class CharacterObject : PlacedObject
{
    public int HP { get; private set; }
    public int MaxHp { get; private set; }
    public int Damage { get; private set; }
    public int Defense { get; private set; }
    public bool IsPlayer { get; private set; }

    public CharacterObject(string name, int hp, int damage, int defense, bool isPlayer)
    {
        Name = name;
        HP = hp;
        MaxHp = hp;
        Damage = damage;
        Defense = defense;
        IsPlayer = isPlayer;
        ObjectType = ObjectType.Character;
    }

    public void TakeDamage(int amount)
    {
        int finalDamage = Mathf.Max(amount - Defense, 1);
        HP -= finalDamage;
        HP = Mathf.Max(HP, 0);

        Debug.Log($"{Name} took {finalDamage} damage! (Raw: {amount}, Defense: {Defense}) Remaining HP: {HP}");

        if (HP <= 0)
            OnDeath();
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
        // OnDeath logic
    }
}