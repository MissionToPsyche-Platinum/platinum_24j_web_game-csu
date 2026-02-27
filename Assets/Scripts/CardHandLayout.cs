#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simple hand layout that:
/// - Fans cards out around a center point
/// - Slightly rotates each card
/// - On hover, raises / scales the card and straightens its rotation
/// 
/// Usage:
/// - Add this component to a parent object that will contain card instances.
/// - Assign the existing card prefab in the inspector.
/// - Call RefreshLayout() after adding / removing cards, or enable autoLayoutOnStart.
/// </summary>
public class CardHandLayout : MonoBehaviour
{
    [Header("Card Setup")]
    [Tooltip("Existing card prefab you are already using.")]
    public GameObject cardPrefab;

    [Tooltip("Optional: parent for instantiated cards. Defaults to this transform.")]
    public Transform cardsParent = null!;

    [Header("Fan Layout")]
    [Tooltip("Total angle (in degrees) that the hand will span.")]
    public float fanAngle = 40f;

    [Tooltip("Radius from the center of the hand to the cards.")]
    public float radius = 200f;

    [Tooltip("Offset in local space to lift the middle of the fan (for UI, usually Y+).")]
    public Vector3 baseOffset = Vector3.zero;

    [Tooltip("If true, layout is calculated using RectTransform.localPosition. Otherwise Transform.localPosition.")]
    public bool useRectTransform = true;

    [Header("Hover Effect")]
    [Tooltip("How much to move the hovered card along its local Y axis.")]
    public float hoverLift = 40f;

    [Tooltip("How much to scale the hovered card.")]
    public float hoverScale = 1.2f;

    [Tooltip("Speed of hover transition.")]
    public float hoverLerpSpeed = 10f;

    [Tooltip("If true, hovered card will be straight (0°) instead of fanned.")]
    public bool straightenOnHover = true;

    [Header("Behaviour")]
    public bool autoLayoutOnStart = true;

    private readonly List<CardInstance> _cards = new List<CardInstance>();

    private void Awake()
    {
        if (cardsParent == null)
            cardsParent = transform;
    }

    private void Start()
    {
        if (autoLayoutOnStart)
        {
            CollectExistingCards();
            RefreshLayout();
        }
    }

    /// <summary>
    /// Finds existing card objects under the parent (useful if you already placed them in the scene).
    /// </summary>
    public void CollectExistingCards()
    {
        _cards.Clear();

        foreach (Transform child in cardsParent)
        {
            var card = child.gameObject;
            var hover = card.GetComponent<CardHover>();
            if (hover == null)
            {
                hover = card.AddComponent<CardHover>();
            }
            hover.Bind(this);

            _cards.Add(new CardInstance
            {
                transform = child,
                rectTransform = child as RectTransform
            });
        }
    }

    /// <summary>
    /// Instantiates a new card from the prefab and adds it to the hand.
    /// </summary>
    public GameObject AddCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("CardHandLayout: No cardPrefab assigned.");
            return null;
        }

        var go = Instantiate(cardPrefab, cardsParent);
        var hover = go.GetComponent<CardHover>();
        if (hover == null)
            hover = go.AddComponent<CardHover>();
        hover.Bind(this);

        _cards.Add(new CardInstance
        {
            transform = go.transform,
            rectTransform = go.transform as RectTransform
        });

        RefreshLayout();
        return go;
    }

    /// <summary>
    /// Recalculates positions and rotations for all cards.
    /// Call this after adding / removing / reordering.
    /// </summary>
    public void RefreshLayout()
    {
        if (_cards.Count == 0)
            return;

        float angleStep = _cards.Count > 1 ? fanAngle / (_cards.Count - 1) : 0f;
        float startAngle = -fanAngle * 0.5f;

        for (int i = 0; i < _cards.Count; i++)
        {
            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector3 localPos = new Vector3(
                Mathf.Sin(rad) * radius,
                baseOffset.y + (1f - Mathf.Cos(rad)) * radius * 0.1f,
                0f
            );

            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

            var c = _cards[i];
            c.defaultLocalPosition = localPos;
            c.defaultLocalRotation = rotation;
            c.defaultLocalScale = Vector3.one;

            if (useRectTransform && c.rectTransform != null)
            {
                c.rectTransform.anchoredPosition = new Vector2(localPos.x, localPos.y);
                c.rectTransform.localRotation = rotation;
                c.rectTransform.localScale = Vector3.one;
            }
            else
            {
                c.transform.localPosition = localPos;
                c.transform.localRotation = rotation;
                c.transform.localScale = Vector3.one;
            }
        }
    }

    /// <summary>
    /// Called by CardHover when pointer enters / exits.
    /// </summary>
    internal void SetHover(CardHover hover, bool isHovering)
    {
        if (hover == null)
            return;

        var t = hover.transform;
        var rect = t as RectTransform;

        var card = _cards.Find(c => c.transform == t);
        if (card.transform == null)
            return;

        // Target values
        Vector3 targetPos = card.defaultLocalPosition;
        Quaternion targetRot = card.defaultLocalRotation;
        Vector3 targetScale = card.defaultLocalScale;

        if (isHovering)
        {
            targetPos += Vector3.up * hoverLift;
            if (straightenOnHover)
                targetRot = Quaternion.identity;
            targetScale *= hoverScale;

            // Optionally bring to front
            t.SetAsLastSibling();
        }

        card.hoverCoroutine?.Stop();
        card.hoverCoroutine = new HoverAnimation(this, card, targetPos, targetRot, targetScale, rect);
    }

    private void Update()
    {
        // Tick simple "coroutines" that lerp hover transitions.
        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].hoverCoroutine?.Tick(Time.deltaTime, hoverLerpSpeed);
        }
    }

    #region Nested Types

    private struct CardInstance
    {
        public Transform transform;
        public RectTransform rectTransform;

        public Vector3 defaultLocalPosition;
        public Quaternion defaultLocalRotation;
        public Vector3 defaultLocalScale;

        public HoverAnimation? hoverCoroutine;
    }

    private class HoverAnimation
    {
        private readonly CardHandLayout _layout;
        private readonly Transform _transform;
        private readonly RectTransform _rectTransform;

        private Vector3 _startPos;
        private Quaternion _startRot;
        private Vector3 _startScale;

        private Vector3 _targetPos;
        private Quaternion _targetRot;
        private Vector3 _targetScale;

        private float _t;

        public HoverAnimation(CardHandLayout layout, CardInstance card, Vector3 targetPos, Quaternion targetRot, Vector3 targetScale, RectTransform rectTransform)
        {
            _layout = layout;
            _transform = card.transform;
            _rectTransform = rectTransform!;

            if (_layout.useRectTransform && _rectTransform != null)
            {
                var ap = _rectTransform.anchoredPosition;
                _startPos = new Vector3(ap.x, ap.y, 0f);
                _startRot = _rectTransform.localRotation;
                _startScale = _rectTransform.localScale;
            }
            else
            {
                _startPos = _transform.localPosition;
                _startRot = _transform.localRotation;
                _startScale = _transform.localScale;
            }

            _targetPos = targetPos;
            _targetRot = targetRot;
            _targetScale = targetScale;
            _t = 0f;
        }

        public void Tick(float deltaTime, float speed)
        {
            _t += deltaTime * speed;
            float t = Mathf.Clamp01(_t);

            Vector3 pos = Vector3.Lerp(_startPos, _targetPos, t);
            Quaternion rot = Quaternion.Slerp(_startRot, _targetRot, t);
            Vector3 scale = Vector3.Lerp(_startScale, _targetScale, t);

            if (_layout.useRectTransform && _rectTransform != null)
            {
                _rectTransform.anchoredPosition = new Vector2(pos.x, pos.y);
                _rectTransform.localRotation = rot;
                _rectTransform.localScale = scale;
            }
            else
            {
                _transform.localPosition = pos;
                _transform.localRotation = rot;
                _transform.localScale = scale;
            }
        }

        public void Stop()
        {
            // No-op; exists so we can null out reference on CardInstance if needed later.
        }
    }

    #endregion
}

/// <summary>
/// Handles pointer events on an individual card and notifies CardHandLayout.
/// Attach automatically via CardHandLayout or manually on your card prefab.
/// Also supports click-dragging a card while the left mouse button is held.
/// </summary>
public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CardHandLayout _layout;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private bool _isDragging;
    private Vector3 _dragOffset;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
    }

    internal void Bind(CardHandLayout layout)
    {
        _layout = layout;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _layout?.SetHover(this, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _layout?.SetHover(this, false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _isDragging = true;

        // Stop any hover animation and use the current position as drag start.
        _layout?.SetHover(this, false);
        transform.SetAsLastSibling();

        if (_rectTransform != null && _rectTransform.parent is RectTransform parentRect)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? _canvas.worldCamera
                        : null,
                    out var localPoint))
            {
                var delta = _rectTransform.anchoredPosition - localPoint;
                _dragOffset = new Vector3(delta.x, delta.y, 0f);
            }
        }
        else
        {
            var cam = _canvas != null ? _canvas.worldCamera : Camera.main;
            var worldPoint = cam != null
                ? cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, cam.nearClipPlane))
                : transform.position;
            _dragOffset = transform.position - worldPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        if (_rectTransform != null && _rectTransform.parent is RectTransform parentRect)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? _canvas.worldCamera
                        : null,
                    out var localPoint))
            {
                _rectTransform.anchoredPosition = (Vector2)localPoint + (Vector2)_dragOffset;
            }
        }
        else
        {
            var cam = _canvas != null ? _canvas.worldCamera : Camera.main;
            if (cam != null)
            {
                var worldPoint = cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, cam.nearClipPlane));
                transform.position = worldPoint + _dragOffset;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
            return;

        _isDragging = false;

        // When the drag ends, smoothly animate back into the hand layout.
        _layout?.SetHover(this, false);
    }
}

