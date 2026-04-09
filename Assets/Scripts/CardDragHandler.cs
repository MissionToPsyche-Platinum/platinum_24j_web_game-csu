using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drags a hand card in screen space. On release, <see cref="CardDropZone"/> may accept the play;
/// otherwise the card snaps back to its saved hand slot (parent, sibling index, rect state).
/// Uses <see cref="CanvasGroup.blocksRaycasts"/> so the pointer can hit drop targets while dragging.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rect;
    private Canvas _rootCanvas;
    private CanvasGroup _canvasGroup;

    private Transform _originalParent;
    private int _originalSiblingIndex;
    private Vector2 _originalAnchoredPosition;
    private Vector2 _originalAnchorMin;
    private Vector2 _originalAnchorMax;
    private Vector2 _originalPivot;
    private Vector2 _originalSizeDelta;
    private Quaternion _originalLocalRotation;
    private Vector3 _originalLocalScale;
    private Vector2 _pointerOffsetLocal;

    private bool _dropEvaluated;
    private bool _playSucceeded;
    private bool _restored;

    /// <summary>True after a successful BeginDrag reparent (used to defer raycast restore).</summary>
    private bool _dragging;

    private Coroutine _endDragRoutine;

    private void Awake()
    {
        _rect = transform as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>Used by <see cref="CardTrigger"/> so a drag does not also fire click-to-play.</summary>
    public bool DidDrag { get; private set; }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!GamePhaseController.PlayerMayInteractWithCards)
            return;

        DidDrag = false;
        _dropEvaluated = false;
        _playSucceeded = false;
        _restored = false;

        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (_rootCanvas == null)
            return;

        _originalParent = _rect.parent;
        _originalSiblingIndex = _rect.GetSiblingIndex();
        SaveOriginalLayout();

        // Float above the hand while dragging
        _rect.SetParent(_rootCanvas.transform, true);
        _rect.SetAsLastSibling();

        // Let rays pass through this card so PlayZone (below) receives IDropHandler.
        // interactable=false avoids the group consuming clicks mid-drag.
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _dragging = true;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                eventData.position,
                _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
                out var localPoint))
        {
            localPoint = _rect.anchoredPosition;
        }

        _pointerOffsetLocal = _rect.anchoredPosition - localPoint;

        if (_endDragRoutine != null)
        {
            StopCoroutine(_endDragRoutine);
            _endDragRoutine = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _rootCanvas == null)
            return;

        if (eventData.delta.sqrMagnitude > 0.5f)
            DidDrag = true;

        var canvasRect = _rootCanvas.transform as RectTransform;
        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, cam, out var localPoint))
        {
            _rect.anchoredPosition = localPoint + _pointerOffsetLocal;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Do NOT restore blocksRaycasts here — OnDrop on the zone often runs in the same frame
        // after IEndDragHandler; turning raycasts back on immediately can block drop detection.
        if (!_dragging)
            return;

        if (_endDragRoutine != null)
            StopCoroutine(_endDragRoutine);
        _endDragRoutine = StartCoroutine(CoDeferredEndDrag());
    }

    /// <summary>Called from <see cref="CardDropZone"/> after play resolution (same frame as OnEndDrag, after this handler).</summary>
    public void NotifyDropEvaluated(bool playSucceeded)
    {
        _dropEvaluated = true;
        _playSucceeded = playSucceeded;
        if (!playSucceeded)
            RestoreToHand();
    }

    private IEnumerator CoDeferredEndDrag()
    {
        // Wait until after this frame's drop / pointer pipeline so PlayZone can receive OnDrop.
        yield return null;

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        _dragging = false;

        if (this == null || _rect == null)
            yield break;

        // No drop zone handled this drag — snap back.
        if (!_dropEvaluated)
            RestoreToHand();
        else if (!_playSucceeded)
            RestoreToHand();

        DidDrag = false;
        _endDragRoutine = null;
    }

    private void SaveOriginalLayout()
    {
        _originalAnchoredPosition = _rect.anchoredPosition;
        _originalAnchorMin = _rect.anchorMin;
        _originalAnchorMax = _rect.anchorMax;
        _originalPivot = _rect.pivot;
        _originalSizeDelta = _rect.sizeDelta;
        _originalLocalRotation = _rect.localRotation;
        _originalLocalScale = _rect.localScale;
    }

    private void RestoreToHand()
    {
        if (_restored || _originalParent == null)
            return;

        _restored = true;

        _rect.SetParent(_originalParent, false);
        _rect.SetSiblingIndex(Mathf.Clamp(_originalSiblingIndex, 0, _originalParent.childCount - 1));

        _rect.anchorMin = _originalAnchorMin;
        _rect.anchorMax = _originalAnchorMax;
        _rect.pivot = _originalPivot;
        _rect.sizeDelta = _originalSizeDelta;
        _rect.anchoredPosition = _originalAnchoredPosition;
        _rect.localRotation = _originalLocalRotation;
        _rect.localScale = _originalLocalScale;
    }

    private void OnDisable()
    {
        DidDrag = false;
        _dragging = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }
    }
}
