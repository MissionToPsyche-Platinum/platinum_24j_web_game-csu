using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    public CardData cardData;

    public TMP_Text cardNameText;
    public TMP_Text descriptionText;
    public TMP_Text powerText;

    public Image cardImage;

    void Start()
    {
        cardNameText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        powerText.text = "Power: " + cardData.power;
        cardImage.sprite = cardData.cardImage;
    }
}