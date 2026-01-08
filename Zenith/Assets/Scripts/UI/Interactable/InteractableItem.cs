using System.Collections;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InteractableItem : InteractableObject, Interactable
{

    public Item item;
    public DraggableItem DraggableItemPrefab;
    public GameObject Object;
    private Inventory inventory;
    private bool up = true;

    private void Start() {
        inventory = FindAnyObjectByType<OverworldUI>().inventory;

        GetComponent<SpriteRenderer>().sprite = item.sprite;

        StartCoroutine(anim());
    }

    public override void OnInteract()
    {
        DraggableItem newDraggableItem = Instantiate(DraggableItemPrefab);
        newDraggableItem.item = item;
        newDraggableItem.InitializeItemValues();
        if (!inventory.CanInsertToInventory(newDraggableItem)) {Destroy(newDraggableItem); return;}
        inventory.AddItem(newDraggableItem);
        newDraggableItem.gameObject.GetComponent<Image>().sprite = item.sprite;
        Kill();
        base.OnInteract();
    }

    private IEnumerator anim()
    {
        while (true)
        {
            if (transform != null) {DOTween.Kill(transform); yield return null;}

            Vector3 newPos = new Vector3(
                transform.position.x, 
                transform.position.y + (up ? 0.3f : -0.3f),
                transform.position.z);

            transform.DOMove(newPos, 1f)
                .SetEase(Ease.InOutSine);
                
            up = !up;
            yield return new WaitForSeconds(1f);
            
        }
    }

    private void Kill()
    {
        DOTween.Kill(transform);
        StopCoroutine(anim());
        Destroy(this.gameObject);
    }
}
