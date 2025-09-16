using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Dragging component for PlayableElement that handles drag behavior, constraints, and feedback.
  /// This component provides drag-specific functionality separate from physics.
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

    [Header("Drag Feedback")]
    [Tooltip("Scale multiplier when dragging starts")]
    [Range(0.5f, 2f)]
    public float DragScale = 1.1f;
    [Tooltip("Should the element move to a higher sorting layer when dragged?")]
    public bool MoveToFrontOnDrag = true;
    [Tooltip("Sorting layer offset when dragging")]
    public int DragSortingLayerOffset = 10;
    // Visual feedback component
    [Tooltip("The component that handles visual feedback during dragging. If not set, a default one will be created.")]
    [SerializeField] [DisplayWithoutEdit] protected PlayableElementDragFeedback _dragFeedbackComponent;

    [Header("Drop Validation")]
    [Tooltip("If true, element will return to original position if dropped in invalid location")]
    public bool ValidateDropLocation = true;

    // Private state
    private Vector3 _originalPosition;
    private Vector3 _originalScale;
    private int _originalSortingOrder;
    private SpriteRenderer _spriteRenderer;
    private bool _isDragging = false;
    private Vector3 _dragOffset;


    private void OnValidate() {
      //try get the _dragFeedbackComponent if not set
      EnsureDragFeedbackComponent(false); // false to avoid adding components in edit mode
    }

    protected override void Initialize() {
      _spriteRenderer = _playableElement.Model.GetComponent<SpriteRenderer>();
      if (_spriteRenderer != null) {
        _originalSortingOrder = _spriteRenderer.sortingOrder;
      }

      // Override PlayableElement dragging capability
      _playableElement.Draggable = true;

      // Auto-assign _dragFeedbackComponent if needed
      EnsureDragFeedbackComponent();
    }

    protected override bool Validate() {
      return _playableElement != null;
    }

    protected virtual void EnsureDragFeedbackComponent(bool addIfMissing=true) {
      if (_dragFeedbackComponent == null) {
        if (TryGetComponent<PlayableElementDragFeedback>(out var existing)) {
          _dragFeedbackComponent = existing;
        }
        // If still null and addIfMissing is true, add one
        // this flag is false during OnValidate to avoid adding components in edit mode
        else if (addIfMissing) {
          this.LogWarning($"[PlayableElementDragging] UseDraggingVisuals is true but no _dragFeedbackComponent found on {name}. Adding one automatically.");
          _dragFeedbackComponent = gameObject.AddComponent<PlayableElementDragFeedback>();
          _dragFeedbackComponent.transform.parent = transform;
          _dragFeedbackComponent.enabled = true;
          _dragFeedbackComponent.gameObject.SetActive(true);
        }
      }
    }

    protected override void HandleDragStart(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;

      _isDragging = true;
      _originalPosition = _playableElement.GetPosition();
      _originalScale = _playableElement.SnapTransform.localScale;

      // Calculate drag offset (difference between pointer and element center)
      _dragOffset = _originalPosition - evt.WorldPosition;

      // Apply drag visual effects
      ApplyDragStartEffects();

      DebugLog($"Drag started on {_playableElement.name}");
    }

    protected override void HandleDragUpdate(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;

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

      // Remove drag visual effects
      RemoveDragEffects();

      DebugLog($"Drag ended on {_playableElement.name}");
    }

    private void ApplyDragStartEffects() {
      // Scale effect
      if (DragScale != 1f) {
        _playableElement.SnapTransform.localScale = _originalScale * DragScale;
      }

      // Sorting layer effect
      if (MoveToFrontOnDrag && _spriteRenderer != null) {
        _spriteRenderer.sortingOrder = _originalSortingOrder + DragSortingLayerOffset;
      }
    }

    private void RemoveDragEffects() {
      // Reset scale
      _playableElement.SnapTransform.localScale = _originalScale;

      // Reset sorting order
      if (_spriteRenderer != null) {
        _spriteRenderer.sortingOrder = _originalSortingOrder;
      }
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

    public void SetDragScale(float scale) {
      DragScale = scale;
    }

    protected override void ResetComponent() {
      if (_isDragging) {
        _isDragging = false;
        RemoveDragEffects();
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