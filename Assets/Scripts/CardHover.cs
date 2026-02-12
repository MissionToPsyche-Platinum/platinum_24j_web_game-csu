using UnityEngine;
using UnityEngine.EventSystems;

public class CardTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private HandFanner fanner;

    void Start()
    {
        // Find the HandFanner in the parent object
        fanner = GetComponentInParent<HandFanner>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Tell the fanner "I am the active card"
        if(fanner != null)
            fanner.SetHoveredIndex(transform.GetSiblingIndex());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Tell the fanner "Nobody is active"
        if(fanner != null)
            fanner.SetHoveredIndex(-1);
    }
}