using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CardView : MonoBehaviour
{
    [Header("Text Components")]
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text cost;

    [Header("Image Components")]
    [SerializeField] private Image cardArtImage; 
    [SerializeField] private Image costBackgroundImage; 
    
    [SerializeField] private GameObject wrapper;
    // This is what DeckManager calls to link the card to the UI
    public CardData CardData { get; private set; }

    public void Bind(CardData data, DeckManager manager) 
    {
        this.CardData = data;
        
        // Update the UI elements
        if (title != null) title.text = data.cardName;
        if (description != null) description.text = data.description;
        if (cost != null) cost.text = $"{data.costPower}/{data.costBudget}/{data.costTime}";
        if (cardArtImage != null) cardArtImage.sprite = data.cardArt;
    }

    public void BindForGallery(CardData data) 
    {
        // Add specific gallery logic here, or just call Bind
        Bind(data, null);
    }
}
