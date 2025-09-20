using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Drag feedback component for PlayableElement that provides visual feedback during dragging.
  /// This component handles color changes, pulse effects, and drop zone validation feedback.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Dragging Feedback Element Component")]
  public class DraggingFeedbackElementComponent : PlayableElementComponent {

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

    [Header("Hover Feedback")]
    [Tooltip("Color tint when hovering (not dragging)")]
    public Color HoverColor = Color.white;
    [Tooltip("Alpha value when hovering")]
    [Range(0.1f, 1f)]
    public float HoverAlpha = 0.9f;

    // State tracking
    private bool _isDragging = false;
    //private bool _isHovering = false;
    //private bool _isValidDropZone = true;
    private Color _originalColor;
    private Color _targetColor;
    private SpriteRenderer _renderer;
    private float _pulseTimer = 0f;

    protected override void Initialize() {
      _renderer = _playableElement.Model.GetComponent<SpriteRenderer>();
      if (_renderer == null) {
        Debug.LogWarning($"[DraggingFeedbackElementComponent] No SpriteRenderer found on {_playableElement.name}");
        return;
      }

      _originalColor = _renderer.color;
      _targetColor = _originalColor;
    }

    protected override bool Validate() {
      return _renderer != null && _playableElement != null;
    }

    protected override void OnUpdate() {
      if (_isDragging) {
        UpdateDragFeedback();
      }

      // Apply color transitions
      _renderer.color = Color.Lerp(_renderer.color, _targetColor,
          Time.deltaTime * ColorTransitionSpeed);

      // Apply pulse effect
      if (_isDragging && EnablePulseEffect) {
        ApplyPulseEffect();
      }
    }

    protected void OnDragStart(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;

      _isDragging = true;
      //_isHovering = false;
      _pulseTimer = 0f;

      // Start with neutral dragging color
      SetNeutralDragging();

      DebugLog($"Started drag feedback for {_playableElement.name}");
    }

    protected  void OnDragUpdate(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || !_isDragging) return;

      // Feedback is updated in OnUpdate() method
    }

    protected void OnDragEnd(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;

      StopDragFeedback();
    }

    protected void OnPointerOver(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || _isDragging) return;

      //_isHovering = true;
      // Light highlight when hovering (not dragging)
      _targetColor = HoverColor;
      _targetColor.a = HoverAlpha;
    }

    protected void OnPointerOut(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement || _isDragging) return;

      //_isHovering = false;
      // Return to original color when not hovering
      _targetColor = _originalColor;
    }

    private void UpdateDragFeedback() {
      if (_levelGrid == null) return;

      // Check if current position is valid for dropping
      var currentGridPos = _levelGrid.WorldToGrid(_playableElement.transform.position);
      bool isInsideGrid = _levelGrid.IsInsidePlayableArea(_playableElement.transform.position);
      bool canPlace = isInsideGrid && _levelGrid.CanPlace(_playableElement, currentGridPos);

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
      //_isValidDropZone = isValid;
      _targetColor = isValid ? ValidDropColor : InvalidDropColor;
      _targetColor.a = DragAlpha;
    }

    private void SetNeutralDragging() {
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

    private void StopDragFeedback() {
      if (!_isDragging) return;

      _isDragging = false;
      _targetColor = _originalColor;
      _targetColor.a = 1f;

      DebugLog($"Stopped drag feedback for {_playableElement.name}");
    }

    // Public Interface
    public void SetDragColors(Color valid, Color invalid, Color neutral) {
      ValidDropColor = valid;
      InvalidDropColor = invalid;
      DraggingColor = neutral;
    }

    public void SetHoverColor(Color hover) {
      HoverColor = hover;
    }

    public void SetDragAlpha(float alpha) {
      DragAlpha = Mathf.Clamp01(alpha);
    }

    public void SetPulseSettings(bool enabled, float speed, float intensity) {
      EnablePulseEffect = enabled;
      PulseSpeed = speed;
      PulseIntensity = Mathf.Clamp01(intensity);
    }

    protected override void ResetComponent() {
      StopDragFeedback();
      if (_renderer != null) {
        _renderer.color = _originalColor;
      }
      //_isHovering = false;
    }

    // Legacy compatibility handlers
    protected override void HandleElementSelected(GridSnappableEventArgs evt) {
      // Legacy compatibility - could map to drag start if needed
    }

    protected override void HandleElementDropped(GridSnappableEventArgs evt) {
      // Legacy compatibility - could map to drag end if needed
    }

    protected override void HandleElementHovered(GridSnappableEventArgs evt) {
      // Legacy compatibility - could map to pointer over if needed
    }

    protected override void HandleElementUnhovered(GridSnappableEventArgs evt) {
      // Legacy compatibility - could map to pointer out if needed
    }

    private void DebugLog(string message) {
      Debug.Log($"[DraggingFeedbackElementComponent] {message}");
    }
  }

}