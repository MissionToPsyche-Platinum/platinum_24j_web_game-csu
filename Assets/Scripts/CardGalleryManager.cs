using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Card gallery: takes a list of CardData, instantiates the card prefab for each,
/// parents them under a UI GridLayoutGroup so they auto-arrange, and populates text/values.
/// Call BackToMenu() to load MainMenu scene.
///
/// SETUP: See instructions below for creating the CardGallery Canvas (Scroll View, Content with GridLayoutGroup, Back button).
/// </summary>
public class CardGalleryManager : MonoBehaviour
{
    [Header("Card collection")]
    [Tooltip("Assign CardData ScriptableObjects to display in the grid. Create via Assets > Create > Psyche > Card Data.")]
    public List<CardData> cardCollection = new List<CardData>();

    [Header("References")]
    [Tooltip("Prefab with CardView component (same as DeckManager uses).")]
    public GameObject cardPrefab;

    [Tooltip("The Content RectTransform that has the GridLayoutGroup. Cards will be instantiated as children here.")]
    public RectTransform gridContent;

    [Tooltip("Optional: if gridContent is null, will try to find a child named 'Content' under a Scroll View.")]
    public bool autoFindContent = true;

    [Header("Back button")]
    [Tooltip("Scene to load when BackToMenu() is called (e.g. MainMenu or SampleScene).")]
    public string menuSceneName = "MainMenu";

    private void Start()
    {
        if (gridContent == null && autoFindContent)
        {
            var scrollView = GetComponentInChildren<ScrollRect>(true);
            if (scrollView != null && scrollView.content != null)
                gridContent = scrollView.content;
        }

        if (gridContent == null)
        {
            Debug.LogWarning("CardGalleryManager: No gridContent assigned and could not auto-find. Assign the Content (with GridLayoutGroup) in the Inspector.");
            return;
        }

        if (cardPrefab == null)
        {
            Debug.LogWarning("CardGalleryManager: No cardPrefab assigned.");
            return;
        }

        BuildGrid();
    }

    /// <summary>Clears existing card instances and rebuilds the grid from cardCollection.</summary>
    public void BuildGrid()
    {
        if (gridContent == null || cardPrefab == null) return;

        // Clear existing card instances
        for (int i = gridContent.childCount - 1; i >= 0; i--)
        {
            var child = gridContent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        if (cardCollection == null || cardCollection.Count == 0) return;

        foreach (CardData data in cardCollection)
        {
            if (data == null) continue;

            GameObject instance = Instantiate(cardPrefab, gridContent);
            var view = instance.GetComponent<CardView>();
            if (view != null)
                view.BindForGallery(data);
            else
                PopulateCardManually(instance, data);
        }

        // Force layout rebuild so GridLayoutGroup positions children
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContent);
    }

    /// <summary>Fallback: if prefab has no CardView, set text by finding TMP_Text/Text by common names.</summary>
    private static void PopulateCardManually(GameObject cardInstance, CardData data)
    {
        var title = cardInstance.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (title != null)
            title.text = data.cardName;
        // If there are multiple TMP_Texts, you may need to assign them by name in a custom component
    }

public void BindForGallery(CardData data)
    {
        _data = data;
        _deckManager = null; // No manager needed in gallery mode
        UpdateUI();
    }

    /// <summary>Loads the menu scene (menuSceneName). Assign to the Back button's onClick in the Inspector.</summary>
    public void BackToMenu()
    {
        string scene = string.IsNullOrEmpty(menuSceneName) ? "MainMenu" : menuSceneName;
        SceneManager.LoadScene(scene);
    }
}
