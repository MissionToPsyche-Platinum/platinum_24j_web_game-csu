using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [Header("Full Card Image (displays the complete card art)")]
    [SerializeField] private Image fullCardImage;

    [Header("Legacy Components (hidden when full card art is available)")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text cost;
    [SerializeField] private Image cardArtImage; 
    [SerializeField] private Image costBackgroundImage; 
    [SerializeField] private GameObject wrapper;

    public CardData CardData { get; private set; }

    /// <summary>Deck that spawned this card (avoids wrong <see cref="DeckManager"/> when AI deck exists).</summary>
    public DeckManager OwningDeck { get; private set; }

    public void Bind(CardData data, DeckManager manager) 
    {
        this.CardData = data;
        OwningDeck = manager;

        if (data.cardArt != null)
        {
            SetupFullCardImage(data.cardArt);
            if (wrapper != null) wrapper.SetActive(false);
        }
        else
        {
            if (wrapper != null) wrapper.SetActive(true);
            if (fullCardImage != null) fullCardImage.enabled = false;
            if (title != null) title.text = data.cardName;
            if (description != null) description.text = data.description;
            if (cost != null) cost.text = $"{data.costPower}/{data.costBudget}/{data.costTime}";
            if (cardArtImage != null) cardArtImage.sprite = null;
        }
    }

    public void BindForGallery(CardData data) 
    {
        Bind(data, null);
    }

    private void SetupFullCardImage(Sprite art)
    {
        if (fullCardImage == null)
        {
            fullCardImage = GetComponent<Image>();
            if (fullCardImage == null)
                fullCardImage = gameObject.AddComponent<Image>();
        }

        fullCardImage.sprite = art;
        fullCardImage.color = Color.white;
        fullCardImage.preserveAspect = false;
        fullCardImage.raycastTarget = true;
        fullCardImage.enabled = true;

        var rt = GetComponent<RectTransform>();
        if (rt != null && rt.sizeDelta.x < 1f)
        {
            const float cardWidth = 140f;
            const float cardHeight = 196f; // 5:7 aspect ratio
            rt.sizeDelta = new Vector2(cardWidth, cardHeight);
        }
    }
}
