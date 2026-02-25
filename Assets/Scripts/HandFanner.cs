using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandFanner : MonoBehaviour
{
    [Header("Fan Settings")]
    public float cardSpacing = 100f;    // Distance between card centers
    public float arcHeight = 20f;       // How much the hand curves down
    public float maxRotation = 15f;     // Rotation of the furthest cards
    
    [Header("Hover Settings")]
    public float hoverScale = 1.5f;     // Size when hovered (1.5 = 150%)
    public float hoverHeight = 100f;    // How high it pops up
    public float moveSpeed = 10f;       // Smoothness of movement

    // Internal State
    private int hoveredIndex = -1;      // -1 means nothing is hovered
    private List<RectTransform> cards = new List<RectTransform>();

    void Update()
    {
        UpdateCardList();
        UpdatePositions();
    }

    void UpdateCardList()
    {
        // Rebuild list in case cards are added/removed
        cards.Clear();
        foreach (Transform child in transform)
        {
            cards.Add(child as RectTransform);
        }
    }

    void UpdatePositions()
    {
        int cardCount = cards.Count;
        if (cardCount == 0) return;

        // Find the center of the hand to measure distance from
        float centerIndex = (cardCount - 1) / 2f;

        for (int i = 0; i < cardCount; i++)
        {
            RectTransform card = cards[i];

            // 1. Calculate Standard Fan Position
            // "t" goes from -1 (left) to +1 (right)
            float t = (i - centerIndex); 
            
            // X Position: Spacing based on index
            float xPos = t * cardSpacing;

            // Y Position: Parabola (Arc) - moves edges down
            // Formula: -abs(distanceFromCenter) * intensity
            float yPos = -Mathf.Abs(t) * arcHeight; // Simple V-shape arc
            // Or use: -(t * t) * arcHeight for a smoother U-shape arc
            
            // Rotation: Rotate based on t
            float zRot = -t * maxRotation;
            float targetScale = 1f;

            // 2. Handle Hover Overrides
            // If this specific card is the one we are hovering...
            if (i == hoveredIndex)
            {
                yPos += hoverHeight;     // Move up
                zRot = 0;                // Straighten out
                targetScale = hoverScale;// Grow big
                // xPos unchanged for hovered card

                // Bring to front of rendering order so it overlaps neighbors
                card.SetAsLastSibling(); 
            }
            // If we are hovering ANY card, push the neighbors slightly?
            // (Optional complex logic usually goes here, skipping for simplicity)

            // 3. Apply the Math (Lerp for smoothness)
            Vector3 targetPos = new Vector3(xPos, yPos, 0);
            Quaternion targetRot = Quaternion.Euler(0, 0, zRot);
            Vector3 targetScaleVec = Vector3.one * targetScale;

            card.localPosition = Vector3.Lerp(card.localPosition, targetPos, Time.deltaTime * moveSpeed);
            card.localRotation = Quaternion.Lerp(card.localRotation, targetRot, Time.deltaTime * moveSpeed);
            card.localScale = Vector3.Lerp(card.localScale, targetScaleVec, Time.deltaTime * moveSpeed);
        }
    }

    // Called by the Card Trigger
    public void SetHoveredIndex(int index)
    {
        hoveredIndex = index;
    }
}