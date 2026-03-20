using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Pointer event handler for individual cards inside a HandFanner.
/// Notifies the parent HandFanner on hover enter/exit and plays the card on click.
/// Auto-attached by HandFanner; can also be placed on a card prefab manually.
/// </summary>
public class CardTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private HandFanner _fanner;

    private void Start()
    {
        _fanner = GetComponentInParent<HandFanner>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_fanner == null) _fanner = GetComponentInParent<HandFanner>();
        _fanner?.SetHoveredCard(transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _fanner?.ClearHover();
    }

    private void OnDisable()
    {
        _fanner?.ClearHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var drag = GetComponent<CardDragHandler>();
        if (drag != null && drag.DidDrag)
            return;

        var cardView = GetComponent<CardView>();
        if (cardView == null) return;

        var deckManager = FindAnyObjectByType<DeckManager>();
        if (deckManager != null)
            deckManager.RequestPlay(cardView);
    }
}
