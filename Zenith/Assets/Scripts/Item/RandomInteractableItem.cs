using UnityEngine;
using TMPro;
using System.Collections;

public class RandomInteractableItem : InteractableObject
{
    public LootTable lootTable;
    private BaseItem rolledItem;

    [Header("Flying Box Setup")]
    public GameObject infoContainer;
    public SpriteRenderer backgroundSR;
    public SpriteRenderer itemIconSR;
    public SpriteRenderer goldIconSR;
    public SpriteRenderer weightIconSR;
    public TextMeshPro valueText;
    public TextMeshPro weightText;

    [Header("Animation Settings")]
    public float fadeSpeed = 5f;
    public float bobbleAmplitude = 0.1f;
    public float bobbleFrequency = 2f;

    [Header("Shake Settings")]
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 0.05f;

    private float currentAlpha = 0f;
    private bool isPlayerNearby = false;
    private bool isShaking = false;
    private Vector3 originalInfoPos;
    private Vector3 originalWeightIconPos;

    void Start()
    {
        int gold = GameManager.Instance != null ? GameManager.Instance.gold_spent : 0;
        rolledItem = lootTable.PickItem(gold);

        if (rolledItem == null)
        {
            Destroy(gameObject);
            return;
        }

        itemIconSR.sprite = rolledItem.icon;
        itemIconSR.transform.localScale = Vector3.one;
        Vector2 spriteSize = itemIconSR.sprite.bounds.size;
        float targetSize = 0.7f;
        float scaleFactor = targetSize / Mathf.Max(spriteSize.x, spriteSize.y);
        itemIconSR.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);

        valueText.text = rolledItem.sellPrice.ToString();
        weightText.text = rolledItem.weight.ToString("F1");

        originalInfoPos = infoContainer.transform.localPosition;
        originalWeightIconPos = weightIconSR.transform.localPosition;

        SetAlpha(0);
    }

    void Update()
    {
        HandleFading();
        HandleBobbling();
    }

    private void HandleFading()
    {
        float targetAlpha = isPlayerNearby ? 1f : 0f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        SetAlpha(currentAlpha);
    }

    private void HandleBobbling()
    {
        if (isShaking) return;

        float yOffset = Mathf.Sin(Time.time * bobbleFrequency) * bobbleAmplitude;
        infoContainer.transform.localPosition = new Vector3(originalInfoPos.x, originalInfoPos.y + yOffset, originalInfoPos.z);
    }

    public override void OnInteract()
    {
        if (rolledItem == null || isShaking) return;

        if (InventoryManager.Instance.AddItem(rolledItem, 1))
        {
            base.OnInteract();
            Destroy(gameObject);
        }
        else
        {
            if (!isShaking) StartCoroutine(ShakeWeightIcon());
        }
    }

    private void SetAlpha(float alpha)
    {
        backgroundSR.color = AdjustAlpha(backgroundSR.color, alpha);
        itemIconSR.color = AdjustAlpha(itemIconSR.color, alpha);
        goldIconSR.color = AdjustAlpha(goldIconSR.color, alpha);
        weightIconSR.color = AdjustAlpha(weightIconSR.color, alpha);
        valueText.alpha = alpha;
        weightText.alpha = alpha;
    }

    private Color AdjustAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    private IEnumerator ShakeWeightIcon()
    {
        isShaking = true;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            weightIconSR.transform.localPosition = originalWeightIconPos + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        weightIconSR.transform.localPosition = originalWeightIconPos;
        isShaking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isPlayerNearby = false;
    }
}