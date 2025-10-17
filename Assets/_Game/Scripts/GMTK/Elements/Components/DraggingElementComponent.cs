using MoreMountains.Feedbacks;
using System;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Dragging component for PlayableElement that handles drag behavior and constraints.
  /// This component provides pure drag functionality without any feedback - feedback should be handled by separate components.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Dragging Element Component")]
  public class DraggingElementComponent : PlayableElementComponent {

    [Header("Snapping")]
    [Tooltip("If true, position at drop will be constrained to the grid")]
    public bool SnapOnDrop = true;
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

    [Flags]
    public enum PositionChangeFlags {
      None = 0,
      OnDrop = 1,
      DuringDrag = 2,
      Event = 4
    }

    [Header("Position Change")]
    [Tooltip("Controls when position updates and events are triggered. OnDrop: updates position on drop, DuringDrag: updates during drag, Event: triggers events without updating the element's position")]
    public PositionChangeFlags ChangeUpdate = PositionChangeFlags.OnDrop;

    [Header("Ghost Settings")]
    [Tooltip("Enable ghost functionality during drag")]
    public bool EnableGhost = true;
    [Tooltip("Alpha value for the dragged element (ghost)")]
    [Range(0.1f, 1f)]
    public float GhostAlpha = 0.7f;
    [Tooltip("Scale multiplier for the dragged element (ghost)")]
    [Range(0.5f, 2f)]
    public float GhostScale = 1.1f;
    [Tooltip("Color tint for the ghost when placement is valid")]
    public Color ValidGhostColor = Color.green;
    [Tooltip("Color tint for the ghost when placement is invalid")]
    public Color InvalidGhostColor = Color.red;
    [Tooltip("Color tint for the ghost when placement is neutral")]
    public Color NeutralGhostColor = Color.yellow;

    [Header("Feedbacks (optionals)")]
    [Tooltip("feedback to play when drag starts")]
    public MMF_Player OnDragStartFeedback;
    [Tooltip("feedback to play while dragging")]
    public MMF_Player OnDragUpdateFeedback;
    [Tooltip("The waiting time en beetween OnDragUpdateFeedback plays")]
    public float DragUpdateWaitInterval = 0.2f;
    [Tooltip("feedback to play when element is dropped")]
    public MMF_Player OnDragEndFeedback;
    [Tooltip("feedback to play when the element changes its position")]
    public MMF_Player PositionChangeFeedback;
    [Tooltip("feedback to play when ghost mode starts")]
    public MMF_Player OnGhostModeStartFeedback;
    [Tooltip("feedback to play when ghost mode ends")]
    public MMF_Player OnGhostModeEndFeedback;
    [Tooltip("feedback to play when ghost placement is valid")]
    public MMF_Player OnValidPlacementFeedback;
    [Tooltip("feedback to play when ghost placement is invalid")]
    public MMF_Player OnInvalidPlacementFeedback;

    // Private state
    private Vector3 _originalPosition;
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    private Vector3 _lastValidPosition;
    private bool _lastPlacementWasValid = true;
    private float _lastDragUpdateFeedbackTime;

    // Ghost-related private fields
    private GameObject _originalPlaceholder;
    private SpriteRenderer _originalRenderer;
    private SpriteRenderer _placeholderRenderer;
    private Color _originalColor;
    private Vector3 _originalScale;
    private Color _ghostTargetColor;
    private bool _isValidDropLocation = false;
    private bool _isInGhostMode = false;

    protected override void Initialize() {
      // Override PlayableElement dragging capability
      _playableElement.Draggable = true;

      // Get the original renderer for ghost functionality
      if (EnableGhost) {
        _originalRenderer = _playableElement.Model.GetComponent<SpriteRenderer>();
        if (_originalRenderer == null) {
          Debug.LogError($"[DraggingElementComponent] No SpriteRenderer found on {_playableElement.name} - Ghost functionality disabled");
          EnableGhost = false;
        }
        else {
          _originalColor = _originalRenderer.color;
          _originalScale = _playableElement.transform.localScale;
          _ghostTargetColor = NeutralGhostColor;
        }
      }

      this.LogDebug("Initialized successfully");
    }

    protected override bool Validate() {
      return _playableElement != null;
    }

    protected override void OnUpdate() {
      if (!_isDragging || !EnableGhost || !_isInGhostMode) return;

      UpdateGhostFeedback();
      UpdateGhostVisuals();
    }

    #region New On* Event Handlers (using reflection-based system)

    protected virtual void OnDragStart(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;

      _isDragging = true;
      _originalPosition = _playableElement.GetPosition();
      _lastValidPosition = _originalPosition;

      // Calculate drag offset (difference between pointer and element center)
      _dragOffset = _originalPosition - evt.WorldPosition;

      // Enable ghost mode if configured
      if (EnableGhost) {
        StartGhostMode();
        PlayFeedback(OnGhostModeStartFeedback);
      }

      // Trigger event if Event flag is set
      if (HasFlag(PositionChangeFlags.Event)) {
        TriggerPositionChangeEvent(evt.WorldPosition, true, "DragStart");
      }
      PlayFeedback(OnDragStartFeedback);
      this.LogDebug($"Drag started on {_playableElement.name}");
    }

    protected virtual void OnDragUpdate(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;
      if (evt.Handled) return; // Allow other components to override behavior

      Vector3 targetPosition = evt.WorldPosition + _dragOffset;

      // Apply constraints
      targetPosition = ApplyConstraints(targetPosition);

      // Check snap threshold
      var distanceToLast = Vector3.Distance(targetPosition, _lastValidPosition);
      if (distanceToLast >= SnapThreshold) {
        // Apply grid snapping if enabled
        if (SnapOnDrop && SnapDuringDrag && _levelGrid != null) {
          targetPosition = ApplyGridSnapping(targetPosition);
        }

        // Handle position updates during drag
        if (HasFlag(PositionChangeFlags.DuringDrag)) {
          ApplyPositionUpdate(targetPosition);
        }

        // Trigger event if Event flag is set
        if (HasFlag(PositionChangeFlags.Event)) {
          TriggerPositionChangeEvent(targetPosition, true, "DragUpdate");
        }

        // TODO: while dragging, if the position doesnt change, we should avoid triggering feedbacks and events
        //Fires the feedback only if the time interval has passed and the feedback isn't playing
        if (OnDragUpdateFeedback != null && DragUpdateWaitInterval > 0f) {
          if (!OnDragUpdateFeedback.IsPlaying && Time.time - _lastDragUpdateFeedbackTime >= DragUpdateWaitInterval) {
            PlayFeedback(OnDragUpdateFeedback);
            _lastDragUpdateFeedbackTime = Time.time;
          }
        }
        else if (OnDragUpdateFeedback != null) {
          //If no interval is set, just play the feedback
          if (!OnDragUpdateFeedback.IsPlaying) {
            PlayFeedback(OnDragUpdateFeedback);
          }
        }
      }
      else {
        // Below threshold - do not update position or trigger events
        this.LogDebug($"Drag update ignored, below snap threshold ({distanceToLast} < {SnapThreshold})");
        return;
      }
    }

    protected virtual void OnDragEnd(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;
      if (evt.Handled) return; // Allow other components to override behavior

      _isDragging = false;
      //stop Drag Update feedback in case is still running
      StopFeedback(OnDragUpdateFeedback);

      Vector3 finalTargetPosition = evt.WorldPosition + _dragOffset;
      finalTargetPosition = ApplyConstraints(finalTargetPosition);
      if (SnapOnDrop && _levelGrid != null) {
        finalTargetPosition = ApplyGridSnapping(finalTargetPosition);
      }

      bool isValidDrop = !ValidateDropLocation || IsValidDropLocation(finalTargetPosition);
      Vector3 finalPosition = finalTargetPosition;

      if (_lastValidPosition == finalTargetPosition) {
        //it means the element never moved, so we'll skip the drop logic, to prevent unnecessary events
        this.LogDebug($"Drag ended without movement on {_playableElement.name}");

        // Still end ghost mode
        if (EnableGhost && _isInGhostMode) {
          EndGhostMode();
          PlayFeedback(OnGhostModeEndFeedback);
        }
        return;
      }

      // Handle position updates on drop
      if (HasFlag(PositionChangeFlags.OnDrop)) {
        if (isValidDrop) {
          // Update position on valid drop
          ApplyPositionUpdate(finalTargetPosition);
        }
        else {
          // Return to original position on invalid drop
          finalPosition = _originalPosition;
          ApplyPositionUpdate(_originalPosition);
        }
      }
      else if (!HasFlag(PositionChangeFlags.DuringDrag)) {
        // If neither OnDrop nor DuringDrag is set, but position needs to be determined for events
        if (!isValidDrop) {
          finalPosition = _originalPosition; // Would return to original
        }
      }
      else if (HasFlag(PositionChangeFlags.DuringDrag)) {
        // Position was already updated during drag, but handle invalid drops
        if (!isValidDrop) {
          finalPosition = _originalPosition;
          _playableElement.UpdatePosition(_originalPosition);
        }
      }

      // End ghost mode
      if (EnableGhost && _isInGhostMode) {
        EndGhostMode();
        PlayFeedback(OnGhostModeEndFeedback);
      }

      // Trigger event if Event flag is set
      if (HasFlag(PositionChangeFlags.Event)) {
        TriggerPositionChangeEvent(finalPosition, false, "DragEnd");
      }

      // Always trigger drop success/failure events
      if (isValidDrop) {
        TriggerDropSuccessEvent(finalPosition);
        this.LogDebug($"Valid drop at {finalPosition}");
      }
      else {
        TriggerDropInvalidEvent(evt.WorldPosition);
        this.LogDebug($"Invalid drop location, element at {finalPosition}");
      }

      PlayFeedback(OnDragEndFeedback);

      this.LogDebug($"Drag ended on {_playableElement.name}");
    }

    #endregion

    #region Ghost Management

    private void StartGhostMode() {
      if (!EnableGhost || _originalRenderer == null || _isInGhostMode) return;

      _isInGhostMode = true;

      // Create placeholder at original position
      CreateOriginalPlaceholder();

      // Apply ghost settings to the original element (which will be dragged)
      ApplyGhostSettingsToOriginal();
    }

    private void EndGhostMode() {
      if (!_isInGhostMode) return;

      _isInGhostMode = false;

      // Restore original element appearance
      RestoreOriginalSettings();

      // Destroy placeholder
      DestroyOriginalPlaceholder();
    }

    private void CreateOriginalPlaceholder() {
      if (_originalPlaceholder != null) {
        DestroyOriginalPlaceholder();
      }

      // Create placeholder GameObject at original position
      _originalPlaceholder = new GameObject($"{_playableElement.name}_Placeholder");
      _originalPlaceholder.transform.SetPositionAndRotation(_originalPosition, _playableElement.transform.rotation);
      _originalPlaceholder.transform.localScale = _originalScale;

      // Copy sprite renderer
      _placeholderRenderer = _originalPlaceholder.AddComponent<SpriteRenderer>();
      _placeholderRenderer.sprite = _originalRenderer.sprite;
      _placeholderRenderer.sortingLayerName = _originalRenderer.sortingLayerName;
      _placeholderRenderer.sortingOrder = _originalRenderer.sortingOrder - 1; // Below original

      // Apply ghost settings to placeholder (static, no feedback)
      Color placeholderColor = NeutralGhostColor;
      placeholderColor.a = GhostAlpha;
      _placeholderRenderer.color = placeholderColor;
    }

    private void DestroyOriginalPlaceholder() {
      if (_originalPlaceholder != null) {
        DestroyImmediate(_originalPlaceholder);
        _originalPlaceholder = null;
        _placeholderRenderer = null;
      }
    }

    private void ApplyGhostSettingsToOriginal() {
      if (_originalRenderer == null) return;

      // Apply scale
      _playableElement.transform.localScale = _originalScale * GhostScale;

      // Apply initial ghost color
      Color ghostColor = NeutralGhostColor;
      ghostColor.a = GhostAlpha;
      _originalRenderer.color = ghostColor;
      _ghostTargetColor = ghostColor;
    }

    private void RestoreOriginalSettings() {
      if (_originalRenderer == null) return;

      // Restore original color and scale
      _originalRenderer.color = _originalColor;
      _playableElement.transform.localScale = _originalScale;
    }

    private void UpdateGhostFeedback() {
      if (!_isInGhostMode || _levelGrid == null) return;

      Vector3 currentPosition = _playableElement.GetPosition();
      bool isValidLocation = IsValidDropLocation(currentPosition);

      // Update visual feedback
      if (isValidLocation != _isValidDropLocation) {
        _isValidDropLocation = isValidLocation;

        // Update ghost color
        Color newColor = isValidLocation ? ValidGhostColor : InvalidGhostColor;
        newColor.a = GhostAlpha;
        _ghostTargetColor = newColor;

        // Play appropriate feedback
        if (isValidLocation) {
          PlayFeedback(OnValidPlacementFeedback);
        }
        else {
          PlayFeedback(OnInvalidPlacementFeedback);
        }
      }
    }

    private void UpdateGhostVisuals() {
      if (_originalRenderer == null || !_isInGhostMode) return;

      // Apply color transitions to the original element (ghost)
      _originalRenderer.color = Color.Lerp(_originalRenderer.color, _ghostTargetColor,
                                          Time.deltaTime * 5f); // Color transition speed
    }

    #endregion

    #region Helper Methods

    private bool HasFlag(PositionChangeFlags flag) {
      return (ChangeUpdate & flag) != 0;
    }

    private void ApplyPositionUpdate(Vector3 targetPosition) {
      // Update position only if it has changed
      if (!targetPosition.Equals(_lastValidPosition)) {
        _playableElement.UpdatePosition(targetPosition);
        _lastValidPosition = targetPosition;
        PlayFeedback(PositionChangeFeedback);
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
      var ret = _levelGrid.SnapToGrid(gridPos);

      return ret;
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

    #endregion

    #region Event Triggering

    private void TriggerPositionChangeEvent(Vector3 position, bool isDuringDrag, string eventContext) {
      // Check if current position is valid for contextual events
      bool isValidPosition = IsValidDropLocation(position);

      // Track validation state changes
      if (isValidPosition != _lastPlacementWasValid || isDuringDrag) {
        _lastPlacementWasValid = isValidPosition;

        this.LogDebug($"Position change event [{eventContext}]: {position}, Valid: {isValidPosition}, Durante Drag: {isDuringDrag}");

        // Here you could trigger custom PlayableElementEventType events if needed
        // For example: TriggerCustomPositionChangeEvent(position, isValidPosition, isDuringDrag, eventContext);
      }
    }

    private void TriggerDropSuccessEvent(Vector3 position) {
      var successArgs = new PlayableElementEventArgs(_playableElement, position, PlayableElementEventType.DropSuccess);
      _gameEventChannel.Raise(GameEventType.PlayableElementEvent, successArgs);
    }

    private void TriggerDropInvalidEvent(Vector3 position) {
      var invalidArgs = new PlayableElementEventArgs(_playableElement, position, PlayableElementEventType.DropInvalid);
      _gameEventChannel.Raise(GameEventType.PlayableElementEvent, invalidArgs);
    }

    #endregion

    #region Public API

    public void SetDragConstraints(bool horizontal, bool vertical) {
      ConstrainHorizontal = horizontal;
      ConstrainVertical = vertical;
    }

    public void SetDragBounds(Vector2 min, Vector2 max) {
      MinPosition = min;
      MaxPosition = max;
    }

    public void SetSnapToGrid(bool snapToGrid, bool snapDuringDrag = false) {
      SnapOnDrop = snapToGrid;
      SnapDuringDrag = snapDuringDrag;
    }

    public void SetPositionChangeSettings(PositionChangeFlags changeUpdate) {
      ChangeUpdate = changeUpdate;
    }

    /// <summary>
    /// Set the ghost appearance settings
    /// </summary>
    public void SetGhostSettings(bool enabled, float alpha, float scale, Color validColor, Color invalidColor, Color neutralColor) {
      EnableGhost = enabled;
      GhostAlpha = Mathf.Clamp01(alpha);
      GhostScale = scale;
      ValidGhostColor = validColor;
      InvalidGhostColor = invalidColor;
      NeutralGhostColor = neutralColor;
    }

    /// <summary>
    /// Set the feel feedback players for ghost functionality
    /// </summary>
    public void SetGhostFeedbacks(MMF_Player ghostModeStart, MMF_Player ghostModeEnd, MMF_Player validPlacement, MMF_Player invalidPlacement) {
      OnGhostModeStartFeedback = ghostModeStart;
      OnGhostModeEndFeedback = ghostModeEnd;
      OnValidPlacementFeedback = validPlacement;
      OnInvalidPlacementFeedback = invalidPlacement;
    }

    /// <summary>
    /// Add a flag to the current PositionChangeFlags
    /// </summary>
    public void AddFlag(PositionChangeFlags flag) {
      ChangeUpdate |= flag;
    }

    /// <summary>
    /// Remove a flag from the current PositionChangeFlags
    /// </summary>
    public void RemoveFlag(PositionChangeFlags flag) {
      ChangeUpdate &= ~flag;
    }

    /// <summary>
    /// Check if a specific flag is set
    /// </summary>
    public bool HasPositionChangeFlag(PositionChangeFlags flag) {
      return HasFlag(flag);
    }

    public bool IsDragging => _isDragging;

    /// <summary>
    /// Get the current valid position (useful when ChangeUpdate includes Event)
    /// </summary>
    public Vector3 GetLastValidPosition() => _lastValidPosition;

    /// <summary>
    /// Check if a specific world position would be a valid drop location
    /// </summary>
    public bool IsValidPosition(Vector3 worldPosition) => IsValidDropLocation(worldPosition);

    /// <summary>
    /// Check if ghost mode is currently active
    /// </summary>
    public bool IsInGhostMode => _isInGhostMode;

    #endregion

    protected override void ResetComponent() {
      if (_isDragging) {
        _isDragging = false;
      }

      // End ghost mode if active
      if (_isInGhostMode) {
        EndGhostMode();
      }

      _lastPlacementWasValid = true;
    }

    protected override void FinalizeComponent() {
      DestroyOriginalPlaceholder();
    }

    private void OnDestroy() {
      DestroyOriginalPlaceholder();
    }

    // Legacy compatibility handlers - still required by abstract base class
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