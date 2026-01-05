using UnityEngine;

public class CharacterView : MonoBehaviour
{
    [SerializeField] private DamagePopup damagePopupPrefab;

    private CharacterObject character;

    public void Bind(CharacterObject character)
    {
        this.character = character;
        character.OnTakenDamage += OnTakenDamage;
    }

    private void OnDestroy()
    {
        if (character != null)
            character.OnTakenDamage -= OnTakenDamage;
    }

    private void OnTakenDamage(int totalDamage)
    {
        SpawnDamagePopup(totalDamage);
    }

    private void SpawnDamagePopup(int damage)
    {
        Vector3 spawnPos = transform.position + new Vector3(0.5f, 1f, 0.5f);

        DamagePopup popup = Instantiate(
            damagePopupPrefab,
            spawnPos,
            Quaternion.identity
        );

        popup.Setup(damage);
    }
}
