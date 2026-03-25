using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Slay the Spire-style card hand fan.
/// If a SplineContainer is assigned (handSpline), cards follow that spline.
/// Otherwise a symmetric quadratic Bezier arc is computed from arcWidth / arcHeight.
/// New cards snap to position instantly; only hover/re-layout uses smooth lerp.
/// Attach to the parent RectTransform (e.g. HandViewAnchor) that holds card instances.
/// </summary>
public class HandFanner : MonoBehaviour
{
    [Header("Spline Layout (optional)")]
    [Tooltip("Enable this only when you intentionally want spline-driven placement.")]
    public bool useSplineLayout = false;
    [Tooltip("Assign a SplineContainer to drive card positions when Use Spline Layout is enabled.")]
    public SplineContainer handSpline;

    [Header("Fan Shape (STS-style)")]
    [Tooltip("Desired spacing between adjacent cards before clamping to max width.")]
    public float cardSpacing = 140f;
    [Tooltip("Maximum width the hand fan can occupy.")]
    public float maxFanWidth = 760f;
    [Tooltip("How much higher the center card sits than the edges.")]
    public float arcHeight = 56f;
    [Tooltip("Maximum Z rotation at the outermost cards.")]
    public float maxRotation = 12f;
    [Tooltip("Horizontal offset for nudging the full hand left/right.")]
    public float horizontalOffset = 0f;

    [Header("Card Size")]
    [Tooltip("Base scale applied to all cards.")]
    public float baseCardScale = 1.1f;
    [Tooltip("Extra vertical offset added to every card position.")]
    public float verticalOffset = 0f;

    [Header("Hover")]
    [Tooltip("Vertical lift (px) of the hovered card above its arc position.")]
    public float hoverLift = 140f;
    [Tooltip("Scale multiplier for the hovered card (on top of baseCardScale).")]
    public float hoverScale = 1.25f;
    [Tooltip("Extra X push (px) applied to immediate neighbors of the hovered card.")]
    public float neighborSpread = 40f;

    [Header("Animation")]
    [Tooltip("Lerp speed for all card transitions.")]
    public float lerpSpeed = 14f;

    private readonly List<RectTransform> _hand = new List<RectTransform>();
    private int _lastHandCount;
    private RectTransform _hovered;
    private Canvas _canvas;

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

    private void Start()
    {
        if (handSpline == null)
            handSpline = GetComponentInChildren<SplineContainer>();
        _canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        SyncCards();
        RefreshHoverFromPointer();
        LayoutCards();
    }

    // ------------------------------------------------------------------
    // Card sync
    // ------------------------------------------------------------------

    private void SyncCards()
    {
        for (int i = _hand.Count - 1; i >= 0; i--)
        {
            if (_hand[i] == null || _hand[i].parent != transform)
                _hand.RemoveAt(i);
        }

        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf) continue;

            var rt = child as RectTransform;
            if (rt == null || _hand.Contains(rt)) continue;

            if (child.GetComponent<SplineContainer>() != null) continue;

            _hand.Add(rt);
            EnsureInteractable(rt);
        }
    }

    private static void EnsureInteractable(RectTransform rt)
    {
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

    private void RefreshHoverFromPointer()
    {
        if (CardRewardUI.IsRewardPanelOpen)
        {
            _hovered = null;
            return;
        }

        if (_hand.Count == 0)
        {
            _hovered = null;
            return;
        }

        if (!TryGetPointerPosition(out Vector2 pointer))
        {
            _hovered = null;
            return;
        }

        Camera uiCamera = null;
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = _canvas.worldCamera;
        RectTransform best = null;
        int bestSibling = int.MinValue;

        for (int i = 0; i < _hand.Count; i++)
        {
            RectTransform card = _hand[i];
            if (card == null || !card.gameObject.activeInHierarchy) continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(card, pointer, uiCamera))
                continue;

            int sibling = card.GetSiblingIndex();
            if (sibling >= bestSibling)
            {
                bestSibling = sibling;
                best = card;
            }
        }

        _hovered = best;
    }

    private static bool TryGetPointerPosition(out Vector2 pointer)
    {
        if (Pointer.current != null)
        {
            pointer = Pointer.current.position.ReadValue();
            return true;
        }

        pointer = default;
        return false;
    }

    // ------------------------------------------------------------------
    // Arc evaluation — STS-style deterministic fan (inverted U / dome)
    // ------------------------------------------------------------------

    private void EvaluateBySpline(float t, out Vector2 position, out Vector2 tangent)
    {
        handSpline.Evaluate(t, out float3 p, out float3 tng, out float3 up);
        Vector3 local = transform.InverseTransformPoint(new Vector3(p.x, p.y, p.z));
        position = new Vector2(local.x, local.y);
        Vector3 localTan = transform.InverseTransformDirection(new Vector3(tng.x, tng.y, tng.z));
        tangent = new Vector2(localTan.x, localTan.y);
    }

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private void LayoutCards()
    {
        int count = _hand.Count;
        if (count == 0) return;

        if (_hovered != null && (!_hovered.gameObject.activeInHierarchy || _hovered.parent != transform))
            _hovered = null;

        bool countChanged = count != _lastHandCount;
        _lastHandCount = count;

        bool canUseSpline = useSplineLayout
                            && handSpline != null
                            && handSpline.Splines != null
                            && handSpline.Splines.Count > 0
                            && handSpline.Splines[0].Count >= 2;

        int hovIdx = _hovered != null ? _hand.IndexOf(_hovered) : -1;
        float span = count <= 1 ? 0f : Mathf.Min(maxFanWidth, cardSpacing * (count - 1));
        float halfSpan = span * 0.5f;
        float xStep = count <= 1 ? 0f : span / (count - 1);
        float mid = (count - 1) * 0.5f;
        float maxDistance = Mathf.Max(1f, mid);
        bool hasHover = hovIdx >= 0;

        for (int i = 0; i < count; i++)
        {
            float x;
            float y;
            float cardRot;

            if (canUseSpline)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                EvaluateBySpline(t, out Vector2 pos, out Vector2 tan);
                x = pos.x + horizontalOffset;
                y = pos.y + verticalOffset;
                cardRot = tan.sqrMagnitude > 0.001f ? -Mathf.Atan2(tan.y, tan.x) * Mathf.Rad2Deg : 0f;
            }
            else
            {
                // Deterministic fan formula:
                // x goes left->right linearly, y uses center-distance tiers for perfect pair symmetry.
                float rawX = count <= 1 ? 0f : (i - mid) * xStep;
                float norm = halfSpan > 0.001f ? rawX / halfSpan : 0f; // [-1, 1]
                float distance01 = Mathf.Abs(i - mid) / maxDistance; // center=0, edges=1
                float dome = 1f - (distance01 * distance01);         // center=1, edges=0

                x = rawX + horizontalOffset;
                y = verticalOffset + (arcHeight * dome);
                cardRot = -norm * maxRotation;
            }

            float scale = baseCardScale;

            if (i == hovIdx)
            {
                y += hoverLift;
                cardRot = 0f;
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
            Quaternion tgtRot = Quaternion.Euler(0f, 0f, cardRot);
            Vector3 tgtScale = Vector3.one * scale;

            if (countChanged)
            {
                card.anchoredPosition = tgtPos;
                card.localRotation = tgtRot;
                card.localScale = tgtScale;
            }
            else
            {
                // Keep non-hover layout perfectly locked so pair heights stay visually identical.
                if (!hasHover)
                {
                    card.anchoredPosition = tgtPos;
                    card.localRotation = tgtRot;
                    card.localScale = tgtScale;
                }
                else
                {
                    float lt = Time.deltaTime * lerpSpeed;
                    card.anchoredPosition = Vector2.Lerp(card.anchoredPosition, tgtPos, lt);
                    card.localRotation = Quaternion.Lerp(card.localRotation, tgtRot, lt);
                    card.localScale = Vector3.Lerp(card.localScale, tgtScale, lt);
                }
            }
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
