using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public string description;

    public Sprite cardImage;

    public int cost;
    public int power;
}