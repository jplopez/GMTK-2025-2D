using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Dragging component for PlayableElement that handles drag behavior and constraints.
  /// This component provides pure drag functionality without any feedback - feedback should be handled by separate components.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Playable Dragging Component")]
  public class PlayableElementDragging : PlayableElementComponent {

    [Header("Drag Settings")]
    [Tooltip("If true, dragging will be constrained to the grid")]
    public bool SnapToGrid = true;
    [Tooltip("If true, element will snap to grid during drag, not just on drop")]
    public bool SnapDuringDrag = false;
    [Tooltip("Distance threshold for snapping during drag")]
    public float SnapThreshold = 0.5f;

    [Header("Drag Constraints")]
    [Tooltip("If true, dragging will be constrained to horizontal axis")]
    public bool ConstrainHorizontal = false;
    [Tooltip("If true, dragging will be constrained to vertical axis")]
    public bool ConstrainVertical = false;
    [Tooltip("Minimum world position for dragging")]
    public Vector2 MinPosition = new(-100, -100);
    [Tooltip("Maximum world position for dragging")]
    public Vector2 MaxPosition = new(100, 100);

    [Header("Drop Validation")]
    [Tooltip("If true, element will return to original position if dropped in invalid location")]
    public bool ValidateDropLocation = true;

    // Private state
    private Vector3 _originalPosition;
    private bool _isDragging = false;
    private Vector3 _dragOffset;


    protected override void Initialize() {
      // Override PlayableElement dragging capability
      _playableElement.Draggable = true;

      DebugLog("Initialized successfully");
    }

    protected override bool Validate() {
      return _playableElement != null;
    }

    protected override void HandleDragStart(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;

      _isDragging = true;
      _originalPosition = _playableElement.GetPosition();

      // Calculate drag offset (difference between pointer and element center)
      _dragOffset = _originalPosition - evt.WorldPosition;

      DebugLog($"Drag started on {_playableElement.name}");
    }

    protected override void HandleDragUpdate(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;
      if (evt.Handled) return; // Allow other components (like SnapDraggingFeedbackComponent) to override behavior

      Vector3 targetPosition = evt.WorldPosition + _dragOffset;

      // Apply constraints
      targetPosition = ApplyConstraints(targetPosition);

      // Apply grid snapping if enabled
      if (SnapToGrid && SnapDuringDrag && _levelGrid != null) {
        targetPosition = ApplyGridSnapping(targetPosition);
      }

      // Update position
      _playableElement.UpdatePosition(targetPosition);
    }

    protected override void HandleDragEnd(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;
      if (evt.Handled) return; // Allow other components to override behavior

      _isDragging = false;

      // Validate drop location
      if (ValidateDropLocation && !IsValidDropLocation(evt.WorldPosition)) {
        // Return to original position
        _playableElement.UpdatePosition(_originalPosition);
        DebugLog($"Invalid drop location, returned {_playableElement.name} to original position");
      }
      else if (SnapToGrid && _levelGrid != null) {
        // Snap to final grid position
        Vector3 snappedPosition = ApplyGridSnapping(_playableElement.GetPosition());
        _playableElement.UpdatePosition(snappedPosition);
      }

      DebugLog($"Drag ended on {_playableElement.name}");
    }

    private Vector3 ApplyConstraints(Vector3 position) {
      // Apply axis constraints
      if (ConstrainHorizontal) {
        position.x = _originalPosition.x;
      }
      if (ConstrainVertical) {
        position.y = _originalPosition.y;
      }

      // Apply boundary constraints
      position.x = Mathf.Clamp(position.x, MinPosition.x, MaxPosition.x);
      position.y = Mathf.Clamp(position.y, MinPosition.y, MaxPosition.y);

      return position;
    }

    private Vector3 ApplyGridSnapping(Vector3 worldPosition) {
      if (_levelGrid == null) return worldPosition;

      Vector2Int gridPos = _levelGrid.WorldToGrid(worldPosition);
      return _levelGrid.SnapToGrid(gridPos);
    }

    private bool IsValidDropLocation(Vector3 worldPosition) {
      if (_levelGrid == null) return true;

      // Check if position is inside playable area
      if (!_levelGrid.IsInsidePlayableArea(worldPosition)) {
        return false;
      }

      // Check if the position is available for placement
      Vector2Int gridPos = _levelGrid.WorldToGrid(worldPosition);
      return _levelGrid.CanPlace(_playableElement, gridPos);
    }

    // Public API
    public void SetDragConstraints(bool horizontal, bool vertical) {
      ConstrainHorizontal = horizontal;
      ConstrainVertical = vertical;
    }

    public void SetDragBounds(Vector2 min, Vector2 max) {
      MinPosition = min;
      MaxPosition = max;
    }

    public void SetSnapToGrid(bool snapToGrid, bool snapDuringDrag = false) {
      SnapToGrid = snapToGrid;
      SnapDuringDrag = snapDuringDrag;
    }

    public bool IsDragging => _isDragging;

    protected override void ResetComponent() {
      if (_isDragging) {
        _isDragging = false;
      }
    }

    // Legacy compatibility handlers
    protected override void HandleElementSelected(GridSnappableEventArgs evt) {
      // Handle legacy selection events for compatibility
    }

    protected override void HandleElementDropped(GridSnappableEventArgs evt) {
      // Handle legacy drop events for compatibility
    }

    protected override void HandleElementHovered(GridSnappableEventArgs evt) {
      // Handle legacy hover events for compatibility
    }

    protected override void HandleElementUnhovered(GridSnappableEventArgs evt) {
      // Handle legacy unhover events for compatibility
    }

    private void DebugLog(string message) {
      Debug.Log($"[PlayableElementDragging] {message}");
    }

    // Gizmos for debugging drag constraints
    void OnDrawGizmosSelected() {
      if (!ConstrainHorizontal && !ConstrainVertical) return;

      Vector3 pos = transform.position;
      Gizmos.color = Color.blue;

      if (ConstrainHorizontal) {
        Gizmos.DrawLine(new Vector3(MinPosition.x, pos.y, pos.z),
                       new Vector3(MaxPosition.x, pos.y, pos.z));
      }

      if (ConstrainVertical) {
        Gizmos.DrawLine(new Vector3(pos.x, MinPosition.y, pos.z),
                       new Vector3(pos.x, MaxPosition.y, pos.z));
      }

      // Draw bounds
      Gizmos.color = Color.cyan;
      Vector3 size = new Vector3(MaxPosition.x - MinPosition.x, MaxPosition.y - MinPosition.y, 0);
      Vector3 center = new Vector3((MinPosition.x + MaxPosition.x) * 0.5f, (MinPosition.y + MaxPosition.y) * 0.5f, pos.z);
      Gizmos.DrawWireCube(center, size);
    }
  }
}