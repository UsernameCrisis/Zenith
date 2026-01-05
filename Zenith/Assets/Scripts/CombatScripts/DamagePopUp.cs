using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float lifetime = 1f;

    private TextMeshPro text;
    private float timer;
    private Color startColor;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshPro>();
        startColor = text.color;
    }

    public void Setup(int damage)
    {
        text.text = damage.ToString();
        timer = lifetime;
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        timer -= Time.deltaTime;
        float t = timer / lifetime;
        text.color = new Color(startColor.r, startColor.g, startColor.b, t);

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}