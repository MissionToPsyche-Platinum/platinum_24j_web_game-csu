using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slay the Spire–style card hand fan.
/// Cards sit on a gentle circular arc whose center is far below the hand anchor.
/// The hovered card lifts, straightens, and scales up; neighbors spread apart.
/// Attach to the parent RectTransform (e.g. HandViewAnchor) that holds card instances.
/// </summary>
public class HandFanner : MonoBehaviour
{
    [Header("Arc Shape")]
    [Tooltip("Radius of the virtual circle the arc sits on. Larger = flatter curve.")]
    public float arcRadius = 2200f;

    [Tooltip("Maximum angle (degrees) the full hand can span.")]
    public float maxArcAngle = 35f;

    [Tooltip("Angle (degrees) added per card to widen the arc.")]
    public float anglePerCard = 6f;

    [Header("Card Size")]
    [Tooltip("Base scale applied to all cards (increase for bigger cards).")]
    public float baseCardScale = 1.6f;

    [Tooltip("Vertical offset (px) to push cards up above the parent bottom edge.")]
    public float verticalOffset = 60f;

    [Header("Hover")]
    [Tooltip("Vertical lift (px) of the hovered card above its resting position.")]
    public float hoverLift = 200f;

    [Tooltip("Scale multiplier for the hovered card (applied on top of baseCardScale).")]
    public float hoverScale = 1.3f;

    [Tooltip("Extra X push (px) applied to immediate neighbors of the hovered card.")]
    public float neighborSpread = 60f;

    [Header("Animation")]
    [Tooltip("Lerp speed for all card transitions.")]
    public float lerpSpeed = 14f;

    private readonly List<RectTransform> _hand = new List<RectTransform>();
    private RectTransform _hovered;

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    public void SetHoveredCard(RectTransform card) => _hovered = card;
    public void ClearHover() => _hovered = null;

    public void SetHoveredIndex(int index)
    {
        _hovered = (index >= 0 && index < _hand.Count) ? _hand[index] : null;
    }

    // ------------------------------------------------------------------
    // Lifecycle
    // ------------------------------------------------------------------

    private void Update()
    {
        SyncCards();
        LayoutCards();
    }

    private void SyncCards()
    {
        for (int i = _hand.Count - 1; i >= 0; i--)
        {
            if (_hand[i] == null || _hand[i].parent != transform)
                _hand.RemoveAt(i);
        }

        foreach (Transform child in transform)
        {
            var rt = child as RectTransform;
            if (rt == null || _hand.Contains(rt)) continue;

            _hand.Add(rt);
            EnsureInteractable(rt);
        }
    }

    private static void EnsureInteractable(RectTransform rt)
    {
        // Pin card anchors to bottom-center of parent so arc positions from the bottom up.
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);

        if (rt.GetComponent<CardTrigger>() == null)
            rt.gameObject.AddComponent<CardTrigger>();

        if (rt.GetComponent<Graphic>() == null)
        {
            if (rt.GetComponent<CanvasRenderer>() == null)
                rt.gameObject.AddComponent<CanvasRenderer>();
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;
        }
    }

    // ------------------------------------------------------------------
    // Arc Layout
    // ------------------------------------------------------------------

    private void LayoutCards()
    {
        int count = _hand.Count;
        if (count == 0) return;

        int hovIdx = _hovered != null ? _hand.IndexOf(_hovered) : -1;

        float totalAngle = Mathf.Min(anglePerCard * Mathf.Max(0, count - 1), maxArcAngle);
        float step = count > 1 ? totalAngle / (count - 1) : 0f;
        float halfAngle = totalAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = -halfAngle + step * i;
            float rad = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(rad) * arcRadius;
            float y = (Mathf.Cos(rad) - 1f) * arcRadius + verticalOffset;
            float zRot = -angle;
            float scale = baseCardScale;

            if (i == hovIdx)
            {
                y += hoverLift;
                zRot = 0f;
                scale = baseCardScale * hoverScale;
            }
            else if (hovIdx >= 0)
            {
                int dist = i - hovIdx;
                if (dist != 0)
                {
                    float push = neighborSpread / Mathf.Abs(dist);
                    x += Mathf.Sign(dist) * push;
                }
            }

            RectTransform card = _hand[i];
            Vector2 tgtPos = new Vector2(x, y);
            Quaternion tgtRot = Quaternion.Euler(0f, 0f, zRot);
            Vector3 tgtScale = Vector3.one * scale;

            float t = Time.deltaTime * lerpSpeed;
            card.anchoredPosition = Vector2.Lerp(card.anchoredPosition, tgtPos, t);
            card.localRotation = Quaternion.Lerp(card.localRotation, tgtRot, t);
            card.localScale = Vector3.Lerp(card.localScale, tgtScale, t);
        }

        for (int i = 0; i < count; i++)
        {
            if (i != hovIdx)
                _hand[i].SetSiblingIndex(i);
        }
        if (hovIdx >= 0)
            _hand[hovIdx].SetAsLastSibling();
    }
}
