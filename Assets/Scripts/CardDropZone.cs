using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drop target for <see cref="CardDragHandler"/>. Resolves the play via <see cref="DeckManager.RequestPlay"/>.
/// </summary>
[DisallowMultipleComponent]
public class CardDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        // Phase is gated on BeginDrag; do not re-check here — a false negative would skip NotifyDropEvaluated
        // and force an unnecessary snap-back after a valid drag.

        var drag = eventData.pointerDrag.GetComponent<CardDragHandler>();
        var view = eventData.pointerDrag.GetComponent<CardView>();
        if (drag == null || view == null)
            return;

        var deck = view.OwningDeck != null
            ? view.OwningDeck
            : FindAnyObjectByType<DeckManager>();
        if (deck == null)
        {
            drag.NotifyDropEvaluated(false);
            return;
        }

        bool ok = deck.RequestPlay(view);
        drag.NotifyDropEvaluated(ok);
    }
}
