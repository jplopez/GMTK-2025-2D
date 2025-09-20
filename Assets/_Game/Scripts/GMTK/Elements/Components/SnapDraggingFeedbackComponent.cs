using UnityEngine;
using MoreMountains.Feedbacks;
using Ameba;

namespace GMTK {
  /// <summary>
  /// A Dragging Feedback component that provides ghost-based dragging feedback.<br/>
  /// The element stays in place while a ghost image is dragged around.<br/>
  /// Requires both <see cref="DraggingElementComponent"/> and optionally other grid components on the same GameObject.<br/>
  /// Integrates with <see cref="MMF_Player"/> (Feel) for visual and audio feedback.<br/>
  /// This component is the ONLY one that should apply feedbacks - the core components work without feedback dependencies.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Snap Dragging Feedback Component")]
  [RequireComponent(typeof(DraggingElementComponent))]
  public class SnapDraggingFeedbackComponent : PlayableElementComponent {

    [Header("Dependencies")]
    [Tooltip("The DraggingElementComponent component (auto-assigned)")]
    [SerializeField] private DraggingElementComponent _draggingComponent;

    [Header("Ghost Settings")]
    [Tooltip("Alpha value for the ghost image")]
    [Range(0.1f, 1f)]
    public float GhostAlpha = 0.7f;
    [Tooltip("Scale multiplier for the ghost image")]
    [Range(0.5f, 2f)]
    public float GhostScale = 1.1f;
    [Tooltip("Color tint for the ghost when placement is valid")]
    public Color ValidGhostColor = Color.green;
    [Tooltip("Color tint for the ghost when placement is invalid")]
    public Color InvalidGhostColor = Color.red;
    [Tooltip("Color tint for the ghost when placement is neutral")]
    public Color NeutralGhostColor = Color.yellow;

    [Header("Feel Feedbacks - Selection")]
    [Tooltip("Feedback to play when element is selected/hovered")]
    public MMF_Player OnElementSelectedFeedback;
    [Tooltip("Feedback to play when element is deselected/unhovered")]
    public MMF_Player OnElementDeselectedFeedback;

    [Header("Feel Feedbacks - Drag")]
    [Tooltip("Feedback to play when drag starts")]
    public MMF_Player OnDragStartFeedback;
    [Tooltip("Feedback to play during drag updates (valid placement)")]
    public MMF_Player OnValidPlacementFeedback;
    [Tooltip("Feedback to play during drag updates (invalid placement)")]
    public MMF_Player OnInvalidPlacementFeedback;

    [Header("Feel Feedbacks - Drop")]
    [Tooltip("Feedback to play when drop is successful")]
    public MMF_Player OnDropSuccessFeedback;
    [Tooltip("Feedback to play when drop is invalid")]
    public MMF_Player OnDropInvalidFeedback;

    [Header("Animation Settings")]
    [Tooltip("Speed of color transitions on the ghost")]
    public float ColorTransitionSpeed = 5f;
    [Tooltip("Enable pulse effect on the ghost")]
    public bool EnableGhostPulse = true;
    [Tooltip("Ghost pulse speed")]
    public float GhostPulseSpeed = 2f;
    [Tooltip("Ghost pulse intensity")]
    [Range(0f, 0.5f)]
    public float GhostPulseIntensity = 0.2f;

    // Private state
    private GameObject _ghostObject;
    private SpriteRenderer _ghostRenderer;
    private SpriteRenderer _originalRenderer;
    private Vector3 _originalPosition;
    private Color _originalColor;
    private Color _ghostTargetColor;
    private bool _isDragging = false;
    private bool _isValidDropLocation = false;
    private float _ghostPulseTimer = 0f;
    private Vector3 _ghostOffset;

    protected override void Initialize() {
      // Auto-assign required components
      _draggingComponent = GetComponent<DraggingElementComponent>();

      if (_draggingComponent == null) {
        Debug.LogError($"[SnapDraggingFeedbackComponent] DraggingElementComponent component not found on {gameObject.name}");
        return;
      }

      // Get the original renderer
      _originalRenderer = _playableElement.Model.GetComponent<SpriteRenderer>();
      if (_originalRenderer == null) {
        Debug.LogError($"[SnapDraggingFeedbackComponent] No SpriteRenderer found on {_playableElement.name}");
        return;
      }

      _originalColor = _originalRenderer.color;
      _ghostTargetColor = NeutralGhostColor;

      this.LogDebug("Initialized successfully");
    }

    protected override bool Validate() {
      return _draggingComponent != null && _originalRenderer != null && _playableElement != null;
    }

    protected override void OnUpdate() {
      if (!_isDragging || _ghostRenderer == null) return;

      UpdateGhostFeedback();
      UpdateGhostVisuals();
    }

    #region New On* Event Handlers (using reflection-based system)

    protected virtual void OnSelected(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      PlayFeedback(OnElementSelectedFeedback);
      this.LogDebug($"Element selected");
    }

    protected virtual void OnDeselected(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      PlayFeedback(OnElementDeselectedFeedback);
      this.LogDebug($"Element deselected");
    }

    protected virtual void OnPointerOver(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      PlayFeedback(OnElementSelectedFeedback);
      this.LogDebug($"Pointer over element");
    }

    protected virtual void OnPointerOut(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      PlayFeedback(OnElementDeselectedFeedback);
      this.LogDebug($"Pointer out of element");
    }

    protected virtual void OnDragStart(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;

      _isDragging = true;
      _originalPosition = _playableElement.GetPosition();
      _ghostOffset = _originalPosition - evt.WorldPosition;

      // Create ghost object
      CreateGhost();

      // Play drag start feedback
      PlayFeedback(OnDragStartFeedback);

      // Mark event as handled to prevent default dragging behavior
      evt.Handled = true;

      this.LogDebug($"Drag started - ghost created at {evt.WorldPosition}");
    }

    protected virtual void OnDragUpdate(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;

      // Update ghost position
      if (_ghostObject != null) {
        Vector3 targetPosition = evt.WorldPosition + _ghostOffset;

        // Apply snapping if enabled and levelGrid is available
        if (_levelGrid != null) {
          targetPosition = _levelGrid.SnapToGrid(targetPosition);
        }

        _ghostObject.transform.position = targetPosition;
      }

      // Mark event as handled to prevent default dragging behavior
      evt.Handled = true;
    }

    protected virtual void OnDragEnd(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;

      _isDragging = false;

      // Check if drop location is valid
      bool isValidDrop = IsValidDropLocation(evt.WorldPosition);

      if (isValidDrop) {
        // Update element position and trigger success event
        Vector3 finalPosition = evt.WorldPosition + _ghostOffset;
        if (_levelGrid != null) {
          finalPosition = _levelGrid.SnapToGrid(finalPosition);
        }

        _playableElement.UpdatePosition(finalPosition);

        // Trigger drop success event
        var successArgs = new PlayableElementEventArgs(_playableElement, finalPosition, PlayableElementEventType.DropSuccess);
        TriggerDropEvent(successArgs);

        PlayFeedback(OnDropSuccessFeedback);
        this.LogDebug($"Drop successful at {finalPosition}");
      }
      else {
        // Trigger drop invalid event
        var invalidArgs = new PlayableElementEventArgs(_playableElement, evt.WorldPosition, PlayableElementEventType.DropInvalid);
        TriggerDropEvent(invalidArgs);

        PlayFeedback(OnDropInvalidFeedback);
        this.LogDebug($"Drop invalid at {evt.WorldPosition}");
      }

      // Clean up ghost
      DestroyGhost();

      // Mark event as handled
      evt.Handled = true;
    }

    protected virtual void OnDropSuccess(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      PlayFeedback(OnDropSuccessFeedback);
      this.LogDebug($"Drop success feedback triggered");
    }

    protected virtual void OnDropInvalid(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      PlayFeedback(OnDropInvalidFeedback);
      this.LogDebug($"Drop invalid feedback triggered");
    }

    #endregion

    #region Ghost Management

    private void CreateGhost() {
      if (_ghostObject != null) {
        DestroyGhost();
      }

      // Create ghost GameObject
      _ghostObject = new GameObject($"{_playableElement.name}_Ghost");
      _ghostObject.transform.SetPositionAndRotation(_originalPosition, _playableElement.transform.rotation);
      _ghostObject.transform.localScale = _playableElement.transform.localScale * GhostScale;

      // Copy sprite renderer
      _ghostRenderer = _ghostObject.AddComponent<SpriteRenderer>();
      _ghostRenderer.sprite = _originalRenderer.sprite;
      _ghostRenderer.sortingLayerName = _originalRenderer.sortingLayerName;
      _ghostRenderer.sortingOrder = _originalRenderer.sortingOrder + 10; // Above original

      // Set initial ghost appearance
      Color ghostColor = NeutralGhostColor;
      ghostColor.a = GhostAlpha;
      _ghostRenderer.color = ghostColor;
      _ghostTargetColor = ghostColor;
    }

    private void DestroyGhost() {
      if (_ghostObject != null) {
        DestroyImmediate(_ghostObject);
        _ghostObject = null;
        _ghostRenderer = null;
      }
    }

    private void UpdateGhostVisuals() {
      if (_ghostRenderer == null) return;

      // Apply color transitions
      _ghostRenderer.color = Color.Lerp(_ghostRenderer.color, _ghostTargetColor,
                                       Time.deltaTime * ColorTransitionSpeed);

      // Apply pulse effect
      if (EnableGhostPulse) {
        _ghostPulseTimer += Time.deltaTime * GhostPulseSpeed;
        float pulseValue = Mathf.Sin(_ghostPulseTimer) * GhostPulseIntensity;

        Color currentColor = _ghostRenderer.color;
        currentColor.a = _ghostTargetColor.a + pulseValue;
        currentColor.a = Mathf.Clamp01(currentColor.a);
        _ghostRenderer.color = currentColor;
      }
    }

    #endregion

    #region Drop Validation & Feedback

    private void UpdateGhostFeedback() {
      if (_ghostObject == null || _levelGrid == null) return;

      Vector3 ghostPosition = _ghostObject.transform.position;
      bool isValidLocation = IsValidDropLocation(ghostPosition);

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

    private bool IsValidDropLocation(Vector3 worldPosition) {
      if (_levelGrid == null) return false;

      // Check if position is inside playable area
      if (!_levelGrid.IsInsidePlayableArea(worldPosition)) {
        return false;
      }

      // Check if the position is available for placement
      Vector2Int gridPos = _levelGrid.WorldToGrid(worldPosition);
      return _levelGrid.CanPlace(_playableElement, gridPos);
    }

    private void TriggerDropEvent(PlayableElementEventArgs args) {
      // Trigger the event through the PlayableElement's event system
      _playableElement.GetType()
        .GetMethod("OnPlayableElementEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
        .Invoke(_playableElement, new object[] { args });
    }

    #endregion
    #region Feel Feedback Integration

    #endregion

    #region Component Lifecycle

    protected override void ResetComponent() {
      _isDragging = false;
      DestroyGhost();

      if (_originalRenderer != null) {
        _originalRenderer.color = _originalColor;
      }
    }

    protected override void FinalizeComponent() {
      DestroyGhost();
    }

    private void OnDestroy() {
      DestroyGhost();
    }

    #endregion

    // Legacy compatibility handlers - still required by abstract base class
    protected override void HandleElementSelected(GridSnappableEventArgs evt) {
      // Legacy compatibility - handle through new event system
    }

    protected override void HandleElementDropped(GridSnappableEventArgs evt) {
      // Legacy compatibility - handle through new event system
    }

    protected override void HandleElementHovered(GridSnappableEventArgs evt) {
      // Legacy compatibility - handle through new event system
    }

    protected override void HandleElementUnhovered(GridSnappableEventArgs evt) {
      // Legacy compatibility - handle through new event system
    }

    #region Public API

    /// <summary>
    /// Set the ghost appearance settings
    /// </summary>
    public void SetGhostSettings(float alpha, float scale, Color validColor, Color invalidColor, Color neutralColor) {
      GhostAlpha = Mathf.Clamp01(alpha);
      GhostScale = scale;
      ValidGhostColor = validColor;
      InvalidGhostColor = invalidColor;
      NeutralGhostColor = neutralColor;
    }

    /// <summary>
    /// Set the feel feedback players
    /// </summary>
    public void SetFeedbacks(MMF_Player elementSelected, MMF_Player elementDeselected, MMF_Player dragStart, 
                           MMF_Player validPlacement, MMF_Player invalidPlacement,
                           MMF_Player dropSuccess, MMF_Player dropInvalid) {
      OnElementSelectedFeedback = elementSelected;
      OnElementDeselectedFeedback = elementDeselected;
      OnDragStartFeedback = dragStart;
      OnValidPlacementFeedback = validPlacement;
      OnInvalidPlacementFeedback = invalidPlacement;
      OnDropSuccessFeedback = dropSuccess;
      OnDropInvalidFeedback = dropInvalid;
    }

    /// <summary>
    /// Enable or disable ghost pulse effect
    /// </summary>
    public void SetGhostPulse(bool enabled, float speed = 2f, float intensity = 0.2f) {
      EnableGhostPulse = enabled;
      GhostPulseSpeed = speed;
      GhostPulseIntensity = Mathf.Clamp01(intensity);
    }

    #endregion
  }
}