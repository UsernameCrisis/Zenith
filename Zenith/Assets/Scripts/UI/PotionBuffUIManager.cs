using UnityEngine;

public class PotionBuffUIManager : MonoBehaviour
{
    public GameObject BuffTimerPrefab;

    public void AddItem(DraggableItem item)
    {
        GameObject newObject = Instantiate(BuffTimerPrefab);

        newObject.GetComponent<BuffTimer>().SetVariables(item.item.UILogo, 300);

        newObject.transform.SetParent(this.transform);
    }
}
