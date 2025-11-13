using UnityEngine;
using UnityEngine.EventSystems;


public class ButtonHoverSFX : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        AudioManager.Instance.PlaySFX("ButtonHover");
    }
}
