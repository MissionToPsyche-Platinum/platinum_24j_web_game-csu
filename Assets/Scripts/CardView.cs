using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
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

    internal void BindForGallery(CardData data)
    {
        throw new NotImplementedException();
    }
}
