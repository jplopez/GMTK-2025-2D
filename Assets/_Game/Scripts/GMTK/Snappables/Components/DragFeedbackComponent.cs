using UnityEngine;

namespace GMTK {
  public class DragFeedbackComponent : SnappableComponent {

    [Header("Drag Feedback Settings")]
    [Tooltip("Color when the element can be placed at current position")]
    public Color ValidDropColor = Color.green;

    [Tooltip("Color when the element cannot be placed at current position")]
    public Color InvalidDropColor = Color.red;

    [Tooltip("Color when the element is being dragged but position is neutral")]
    public Color DraggingColor = Color.yellow;

    [Tooltip("Alpha value during dragging")]
    [Range(0.1f, 1f)]
    public float DragAlpha = 0.7f;

    [Header("Animation Settings")]
    [Tooltip("Speed of color transitions")]
    public float ColorTransitionSpeed = 5f;

    [Tooltip("Pulse effect during dragging")]
    public bool EnablePulseEffect = true;

    [Tooltip("Pulse speed")]
    public float PulseSpeed = 2f;

    [Tooltip("Pulse intensity")]
    [Range(0f, 0.5f)]
    public float PulseIntensity = 0.2f;

    // State tracking
    private bool _isDragging = false;
    private bool _isValidDropZone = true;
    private Color _originalColor;
    private Color _targetColor;
    private SpriteRenderer _renderer;
    private float _pulseTimer = 0f;
    private Vector3 _lastPosition;

    protected override void Initialize() {
      _renderer = _snappable.Model.GetComponent<SpriteRenderer>();
      if (_renderer == null) {
        Debug.LogWarning($"[DragFeedbackComponent] No SpriteRenderer found on {_snappable.name}");
        return;
      }

      _originalColor = _renderer.color;
      _targetColor = _originalColor;
    }

    protected override bool Validate() {
      return _renderer != null && _snappable != null;
    }

    protected override void OnUpdate() {
      if (!_isDragging) return;

      // Check if the element has moved since last frame
      Vector3 currentPosition = _snappable.transform.position;
      bool hasMovedThisFrame = currentPosition != _lastPosition;

      // Only update feedback if the element is actually moving
      if (hasMovedThisFrame) {
        UpdateDragFeedback();
        _lastPosition = currentPosition;
      }

      // Apply color transitions
      _renderer.color = Color.Lerp(_renderer.color, _targetColor,
          Time.deltaTime * ColorTransitionSpeed);

      // Apply pulse effect only when moving
      if (EnablePulseEffect && hasMovedThisFrame) {
        ApplyPulseEffect();
      }
    }

    private void UpdateDragFeedback() {
      //Debug.Log("UpdateDragFeedback");
      if (_levelGrid == null) return;

      // Check if current position is valid for dropping
      var currentGridPos = _levelGrid.WorldToGrid(_snappable.transform.position);
      bool isInsideGrid = _levelGrid.IsInsidePlayableArea(_snappable.transform.position);
      bool canPlace = isInsideGrid && _levelGrid.CanPlace(_snappable, currentGridPos);

      // Update feedback based on drop validity
      if (isInsideGrid && canPlace) {
        SetValidDropZone(true);
      }
      else if (isInsideGrid && !canPlace) {
        SetValidDropZone(false);
      }
      else {
        // Outside grid - neutral dragging color
        SetNeutralDragging();
      }
    }

    private void SetValidDropZone(bool isValid) {
      //Debug.Log("SetValidDropZone");

      _isValidDropZone = isValid;
      _targetColor = isValid ? ValidDropColor : InvalidDropColor;
      _targetColor.a = DragAlpha;
    }

    private void SetNeutralDragging() {
      //Debug.Log("SetNeutralDragging");
      _targetColor = DraggingColor;
      _targetColor.a = DragAlpha;
    }

    private void ApplyPulseEffect() {
      _pulseTimer += Time.deltaTime * PulseSpeed;
      float pulseValue = Mathf.Sin(_pulseTimer) * PulseIntensity;

      Color currentColor = _renderer.color;
      currentColor.a = _targetColor.a + pulseValue;
      currentColor.a = Mathf.Clamp01(currentColor.a);
      _renderer.color = currentColor;
    }

    #region Input Event Handlers

    protected override void HandleElementSelected(GridSnappableEventArgs evt) {
      if (evt.Element != _snappable) return;

      // Element is being picked up for potential dragging
      // We'll start visual feedback in the LevelGrid's Update when IsMoving becomes true
    }

    protected override void HandleElementDropped(GridSnappableEventArgs evt) {
      if (evt.Element != _snappable) return;

      StopDragFeedback();
    }

    protected override void HandleElementHovered( GridSnappableEventArgs evt) {
      if (evt.Element != _snappable || _isDragging) return;

      // Light highlight when hovering (not dragging)
      _targetColor = _originalColor;
      _targetColor.a = 0.9f;
    }

    protected override void HandleElementUnhovered(GridSnappableEventArgs evt) {
      if (evt.Element != _snappable || _isDragging) return;

      // Return to original color when not hovering
      _targetColor = _originalColor;
    }

    #endregion

    #region Public Interface (called by LevelGrid)

    public void StartDragFeedback() {
      if (_isDragging) return;

      _isDragging = true;
      _pulseTimer = 0f;
      _lastPosition = _snappable.transform.position;

      // Start with neutral dragging color
      SetNeutralDragging();

      //Debug.Log($"[DragFeedbackComponent] Started drag feedback for {_snappable.name}");
    }

    public void StopDragFeedback() {
      if (!_isDragging) return;

      _isDragging = false;
      _targetColor = _originalColor;
      _targetColor.a = 1f;

      //Debug.Log($"[DragFeedbackComponent] Stopped drag feedback for {_snappable.name}");
    }

    #endregion

    protected override void ResetComponent() {
      StopDragFeedback();
      if (_renderer != null) {
        _renderer.color = _originalColor;
      }
    }
  }
}
