using System;
using UnityEngine;
using UnityEssentials;
using MoreMountains.Feedbacks;

namespace GMTK {

  /// <summary>
  /// Dragging component for PlayableElement that handles drag behavior and constraints.
  /// This component provides pure drag functionality without any feedback - feedback should be handled by separate components.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Element Dragging Component")]
  public class ElementDraggingComponent : PlayableElementComponent {

    [Header("Drag Settings")]
    [Tooltip("Minimum dragged Distance for this component to act on the element")]
    public float DragMinMovement = 0.5f;
    [Space]
    [Tooltip("If true, dragging will be constrained to horizontal axis")]
    public bool ConstrainHorizontal = false;
    [Tooltip("If true, dragging will be constrained to vertical axis")]
    public bool ConstrainVertical = false;
    [Space]
    [Tooltip("If true, dragging will be restricted within defined world boundaries")]
    public bool RestrictPosition = false;
    [MMFCondition("RestrictPosition", true)]
    [Tooltip("Minimum world position for dragging")]
    public Vector2 MinPosition = new(-100, -100);
    [MMFCondition("RestrictPosition", true)]
    [Tooltip("Maximum world position for dragging")]
    public Vector2 MaxPosition = new(100, 100);

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

    [Foldout("Feedbacks (optional)")]
    [Header("Drag Feedbacks")]
    [Tooltip("feedback to play when drag starts")]
    public MMF_Player OnDragStartFeedback;
    [Tooltip("feedback to play when element is dropped")]
    public MMF_Player OnDragEndFeedback;
    [Space]
    [Tooltip("feedback to play while dragging")]
    public MMF_Player OnDragUpdateFeedback;
    [Tooltip("The waiting time en beetween OnDragUpdateFeedback plays")]
    public float DragUpdateWaitInterval = 0.2f;
    [Space]
    [Header("Placement Feedbacks")]
    [Tooltip("feedback to play when ghost placement is valid")]
    public MMF_Player OnValidPlacementFeedback;
    [Tooltip("feedback to play when ghost placement is invalid")]
    public MMF_Player OnInvalidPlacementFeedback;

    [Space(10)]

    [Foldout("Ghost Settings")]
    [Header("Ghost Settings")]
    [Tooltip("Enable ghost functionality during drag")]
    public bool EnableGhost = true;
    [MMFCondition("EnableGhost", true)]
    [Tooltip("Enable visual feedbacks for the ghost during drag")]
    public bool EnableGhostFeedbacks = true;
    [MMFCondition("EnableGhost", true)]
    [Tooltip("Alpha value for the dragged element (ghost)")]
    [Range(0.1f, 1f)]
    public float GhostAlpha = 0.7f;
    [MMFCondition("EnableGhost", true)]
    [Tooltip("Scale multiplier for the dragged element (ghost)")]
    [Range(0.5f, 2f)]
    public float GhostScale = 1.1f;
    [MMFCondition("EnableGhost", true)]
    [Tooltip("Color tint for the ghost when placement is valid")]
    public Color ValidGhostColor = Color.green;
    [MMFCondition("EnableGhost", true)]
    [Tooltip("Color tint for the ghost when placement is invalid")]
    public Color InvalidGhostColor = Color.red;
    [MMFCondition("EnableGhost", true)]
    [Tooltip("Color tint for the ghost when placement is neutral")]
    public Color NeutralGhostColor = Color.yellow;

    [Header("Ghost Feedbacks (optionals)")]
    [MMFCondition("EnableGhostFeedbacks", true)]
    [Tooltip("feedback to play when ghost mode starts")]
    public MMF_Player OnGhostModeStartFeedback;
    [MMFCondition("EnableGhostFeedbacks", true)]
    [Tooltip("feedback to play when ghost mode ends")]
    public MMF_Player OnGhostModeEndFeedback;

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


    #region PlayableElementComponent

    protected override void Initialize() {
      // Override PlayableElement dragging capability if is active
      _playableElement.Draggable = IsActive;

      // Get the original renderer for ghost functionality
      if (EnableGhost) {
        _originalRenderer = _playableElement.Model.GetComponent<SpriteRenderer>();
        if (_originalRenderer == null) {
          Debug.LogError($"[ElementDraggingComponent] No SpriteRenderer found on {_playableElement.name} - Ghost functionality disabled");
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

    protected override void FinalizeComponent() => DestroyOriginalPlaceholder();

    private void OnDestroy() => DestroyOriginalPlaceholder();

    #endregion

    #region Event Handlers

    public override void OnDragStart(PlayableElementEventArgs evt) {
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
      PlayFeedback(OnDragStartFeedback);
      this.LogDebug($"Drag started on {_playableElement.name}");
    }

    public override void OnDragging(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;
      if (evt.Handled) return; // Allow other components to override behavior

      Vector3 targetPosition = evt.WorldPosition + _dragOffset;
      // Apply constraints
      targetPosition = ApplyConstraints(targetPosition);

      // Check min distance
      var distanceToLast = Vector3.Distance(targetPosition, _lastValidPosition);
      if (distanceToLast >= DragMinMovement) {
        this.LogDebug($"Dragging {_playableElement.name} to {targetPosition}");
        // Handle position updates during drag
        if (HasFlag(PositionChangeFlags.DuringDrag)) {
          ApplyPositionUpdate(targetPosition);
        }

        //If DragUpdateFeedback is not null and isn't currently playing, should play if:
        // time interval has passed since last play 
        // OR interval is set to zero, which means there is no wait time
        if (OnDragUpdateFeedback != null && !OnDragUpdateFeedback.IsPlaying) {
          if (DragUpdateWaitInterval == 0f
            || Time.time - _lastDragUpdateFeedbackTime >= DragUpdateWaitInterval) {
            PlayFeedback(OnDragUpdateFeedback);
            _lastDragUpdateFeedbackTime = Time.time;
          }
        }

        // Update ghost feedback if enabled
        if (EnableGhost && EnableGhostFeedbacks) {
          UpdateGhostFeedback();
        }
      }
    }

    public override void OnDragEnd(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;
      if (evt.Handled) return; // Allow other components to override behavior

      _isDragging = false;

      //stop Drag Update feedback in case is still running
      StopFeedback(OnDragUpdateFeedback);

      // Determine final target position applying axis constraints and min distance
      Vector3 finalTargetPosition = ApplyConstraints(evt.WorldPosition + _dragOffset);
      finalTargetPosition = Vector3.Distance(finalTargetPosition, _lastValidPosition) >= DragMinMovement ?
          finalTargetPosition : _lastValidPosition;

      bool isValidDrop = IsValidDropLocation(finalTargetPosition);

      // If drop is invalid, revert to last valid position
      finalTargetPosition = isValidDrop ? _lastValidPosition : finalTargetPosition;

      // Apply position if configured
      if (HasFlag(PositionChangeFlags.OnDrop)) {
        ApplyPositionUpdate(finalTargetPosition);
      }

      // End ghost mode
      if (EnableGhost && _isInGhostMode) {
        EndGhostMode();
        PlayFeedback(OnGhostModeEndFeedback);
      }
      PlayFeedback(OnDragEndFeedback);

      this.LogDebug($"{_playableElement.name} DragEnd at {finalTargetPosition}\t Valid? '{isValidDrop}'\t Update Position? '{HasFlag(PositionChangeFlags.OnDrop)}'");
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
      if (!_isInGhostMode) return; // || _levelGrid == null) return;

      bool isValidLocation = IsValidDropLocation(_playableElement.GetPosition());

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

    #region Position Methods

    /// <summary>
    /// Convenience method to check if a specific PositionChangeFlag is set.
    /// </summary>
    private bool HasFlag(PositionChangeFlags flag) => (ChangeUpdate & flag) != 0;

    /// <summary>
    /// Applies position update to the playable element if it has changed.
    /// </summary>
    /// <remarks>
    /// Triggers position change only if distance between last valid position and <c>targetPosition</c> is greater than <c>DragMinMovement</c>.
    /// Otherwise, respects last valid position.<br/>
    /// Triggers position change feedback if position is updated.<br/>
    /// This method assumes the <see cref="PositionChangeFlags"/> have been checked prior to calling.
    /// </remarks>
    /// <param name="targetPosition"></param>
    protected virtual void ApplyPositionUpdate(Vector3 targetPosition) {
      // Update position only if it has changed beyond DragMinMovement
      targetPosition = Vector3.Distance(targetPosition, _lastValidPosition) >= DragMinMovement ?
          targetPosition : _lastValidPosition;

      _playableElement.UpdatePosition(targetPosition);
      _lastValidPosition = targetPosition;

    }

    /// <summary>
    /// Apply the movement constraints to the given position.
    /// </summary>
    /// <remarks>
    /// Checks <c>ConstrainHorizontal</c> and <c>ConstrainVertical</c> fields 
    /// to apply axis and <c>RestrictPosition</c> field for boundary constraints.
    /// </remarks>
    /// <param name="position"></param>
    /// <returns></returns>
    protected virtual Vector3 ApplyConstraints(Vector3 position) {
      // Apply axis constraints
      if (ConstrainHorizontal) {
        position.x = _originalPosition.x;
      }
      if (ConstrainVertical) {
        position.y = _originalPosition.y;
      }

      // Apply boundary constraints
      if (RestrictPosition) {
        position.x = Mathf.Clamp(position.x, MinPosition.x, MaxPosition.x);
        position.y = Mathf.Clamp(position.y, MinPosition.y, MaxPosition.y);
      }
      return position;
    }

    private Vector3 ApplyGridSnapping(Vector3 worldPosition) {
      //      return (_levelGrid == null) ? worldPosition : _levelGrid.SnapToGrid(worldPosition);
      return SnapToGrid(worldPosition);
    }

    private bool IsValidDropLocation(Vector3 worldPosition) => _playableGrid != null && _playableGrid.CanPlaceElement(_playableElement, worldPosition);

    #endregion

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