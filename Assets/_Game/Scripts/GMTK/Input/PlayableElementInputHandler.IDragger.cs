using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// PlayableElementInputHandler partial class implementing IDragger interface functionality.
  /// Handles dragging logic and element movement.
  /// </summary>
  public partial class PlayableElementInputHandler : IDragger<PlayableElement> {

    [Header("Dragging Settings")]
    [SerializeField] private bool _canDrag = true;
    [SerializeField] private PlayableElement _draggedElement;
    private Vector3 _dragOffset;

    // IDragger<PlayableElement> implementation
    public bool CanDrag {
      get => _canDrag;
      set => _canDrag = value;
    }
    public bool IsDragging => _draggedElement != null;
    public PlayableElement DraggedElement => _draggedElement;

    #region IDragger<PlayableElement> Implementation

    public bool TryStartDrag(PlayableElement element) {
      if (!CanDrag || element == null) return false;

      // Check if element can be dragged
      if (!element.IsDraggable) return false;

      // Stop any current dragging operation
      if (IsDragging) {
        TryStopDrag();
      }

      // Start dragging the new element
      _draggedElement = element;
      _currentElement = element; // Update current element for compatibility
      IsMoving = true;

      // Calculate drag offset (difference between pointer and element center)
      _dragOffset = element.GetPosition() - _pointerWorldPos;

      // Notify the element
      element.OnDragStart();

      // Trigger events
      _eventsChannel.Raise(GameEventType.ElementSelected,
          new GridSnappableEventArgs(ConvertToGridSnappable(element), _pointerScreenPos, _pointerWorldPos));

      this.LogDebug($"Started dragging element: {element.name}");
      return true;
    }

    public bool TryStartDrag(Vector3 worldPosition, out PlayableElement element) {
      element = null;

      if (!CanDrag) return false;

      // Find element at world position
      Vector2 worldPos2D = new Vector2(worldPosition.x, worldPosition.y);
      RaycastHit2D hit = Physics2D.Raycast(worldPos2D, Vector2.zero);

      if (hit && hit.collider != null) {
        if (hit.collider.gameObject.TryGetComponent(out PlayableElement foundElement)) {
          element = foundElement;
          return TryStartDrag(foundElement);
        }
      }

      return false;
    }

    public bool TryStartDrag(Vector2 screenPosition, out PlayableElement element) {
      element = null;

      if (!CanDrag) return false;

      // Convert screen position to world position
      Camera camera = Camera.main;
      if (camera == null) {
        this.LogWarning("No main camera found for screen to world conversion");
        return false;
      }

      Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camera.nearClipPlane));
      return TryStartDrag(worldPos, out element);
    }

    public void UpdateDrag(Vector3 worldPosition) {
      if (!IsDragging || _draggedElement == null) return;

      // Calculate target position with offset
      Vector3 targetPosition = worldPosition + _dragOffset;

      // Update the dragged element
      _draggedElement.OnDragUpdate(targetPosition);
    }

    public bool TryStopDrag() {
      if (!IsDragging) return false;

      var elementToStop = _draggedElement;
      _draggedElement = null;
      IsMoving = false;

      // Clear current element if it was the dragged element
      if (_currentElement == elementToStop) {
        _currentElement = null;
      }

      // Notify the element
      elementToStop.OnDragEnd();

      // Trigger events
      _eventsChannel.Raise(GameEventType.ElementDropped,
          new GridSnappableEventArgs(ConvertToGridSnappable(elementToStop), _pointerScreenPos, _pointerWorldPos));

      this.LogDebug($"Stopped dragging element: {elementToStop.name}");
      return true;
    }

    #endregion

    #region Public Dragging API

    /// <summary>
    /// Programmatically start dragging an element
    /// </summary>
    public bool StartDragElement(PlayableElement element) {
      return TryStartDrag(element);
    }

    /// <summary>
    /// Programmatically stop dragging the current element
    /// </summary>
    public bool StopDragElement() {
      return TryStopDrag();
    }

    /// <summary>
    /// Check if an element is currently being dragged
    /// </summary>
    public bool IsElementDragged(PlayableElement element) {
      return _draggedElement == element;
    }

    #endregion
  }
}