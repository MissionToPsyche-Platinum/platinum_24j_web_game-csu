using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Displays one card and handles click-to-play. Bind with CardData and DeckManager.
/// </summary>
public class CardView : MonoBehaviour, IPointerClickHandler
{
    [Header("Text Components")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text cost;

    [Header("Image Components")]
    [SerializeField] private Image cardArtImage;
    [SerializeField] private Image costBackgroundImage;

    [SerializeField] private GameObject wrapper;

    private DeckManager _deckManager;
    private CardData _cardData;

    /// <summary>Current card data (read-only).</summary>
    public CardData CardData => _cardData;

    /// <summary>Bind card data and deck manager. Call from DeckManager when spawning.</summary>
    public void Bind(CardData data, DeckManager deckManager)
    {
        _cardData = data;
        _deckManager = deckManager;
        if (title != null && data != null) title.text = data.cardName;
        if (description != null && data != null) description.text = data.description;
        if (cost != null && data != null) cost.text = data.CostString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        _deckManager?.RequestPlay(this);
    }
}
