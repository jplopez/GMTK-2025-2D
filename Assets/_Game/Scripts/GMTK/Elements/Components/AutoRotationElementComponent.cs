using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Auto rotation component for PlayableElement that handles automatic continuous rotation.
  /// This component provides independent automatic rotation functionality separate from manual rotation controls.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Auto Rotation Component")]
  public class AutoRotationElementComponent : PlayableElementComponent {

    // Move the enum here since it's only used by auto rotation
    public enum RotationDirections { Clockwise, Counterclockwise }

    [Header("Auto Rotation Settings")]
    [Tooltip("If true, the element will always rotate using the AutoRotationSpeed")]
    public bool EnableAutoRotation = false;
    [Tooltip("Degrees per second the element rotates")]
    public float AutoRotationSpeed = 90f; // degrees per second
    [Tooltip("If set to EnableAutoRotation, this property sets direction of the rotation")]
    public RotationDirections AutoRotationDirection = RotationDirections.Clockwise;

    [Header("Auto Rotation Constraints")]
    [Tooltip("If true, auto rotation will respect the rotation limits from PlayableElementPhysics")]
    public bool RespectRotationLimits = true;
    [Tooltip("If true, auto rotation will pause when the element is being dragged")]
    public bool PauseOnDrag = true;
    [Tooltip("If true, auto rotation will pause when the element is hovered")]
    public bool PauseOnHover = false;

    [Header("Debug")]
    [SerializeField, DisplayWithoutEdit] private Rigidbody2D _rigidbody2D;
    [SerializeField, DisplayWithoutEdit] private bool _isRotating = false;
    [SerializeField, DisplayWithoutEdit] private bool _isPaused = false;

    private PlayableElementPhysics _physicsComponent;
    private bool _isDragging = false;
    private bool _isHovering = false;

    protected override void Initialize() {
      // Get required components
      _rigidbody2D = _playableElement.GetComponent<Rigidbody2D>();
      if (_rigidbody2D == null) {
        Debug.LogWarning($"[AutoRotationElementComponent] No Rigidbody2D found on {_playableElement.name}. Auto rotation requires Rigidbody2D to function.");
        return;
      }

      // Try to get the physics component for integration
      _physicsComponent = _playableElement.GetComponent<PlayableElementPhysics>();

      // Initialize rotation state
      UpdateRotationState();
    }

    protected override bool Validate() => _rigidbody2D != null;

    protected override void OnUpdate() {
      if (!EnableAutoRotation) {
        _isRotating = false;
        return;
      }

      UpdateRotationState();

      if (_isRotating && !_isPaused) {
        ApplyAutoRotation();
      }
    }

    private void UpdateRotationState() {
      // Determine if we should be rotating
      _isRotating = EnableAutoRotation && _rigidbody2D.bodyType == RigidbodyType2D.Dynamic;

      // Determine if we should be paused
      _isPaused = (PauseOnDrag && _isDragging) || (PauseOnHover && _isHovering);
    }

    private void ApplyAutoRotation() {
      if (_physicsComponent == null) {
        // Direct rotation without physics component
        ApplyDirectRotation();
      }
      else {
        // Integrated rotation with physics component
        ApplyIntegratedRotation();
      }
    }

    private void ApplyDirectRotation() {
      float speedAndDirection = (AutoRotationDirection == RotationDirections.Clockwise) ?
                                -AutoRotationSpeed : AutoRotationSpeed;
      float rotationDelta = Time.deltaTime * speedAndDirection;

      float targetAngle = _rigidbody2D.rotation + rotationDelta;
      _rigidbody2D.MoveRotation(targetAngle);
    }

    private void ApplyIntegratedRotation() {
      // Use the physics component's rotation method if available and rotation limits should be respected
      if (RespectRotationLimits && _physicsComponent.AllowRotation) {
        float speedAndDirection = (AutoRotationDirection == RotationDirections.Clockwise) ?
                                  -AutoRotationSpeed : AutoRotationSpeed;
        _physicsComponent.RotateBy(Time.deltaTime * speedAndDirection);
      }
      else {
        // Apply rotation directly, bypassing limits
        ApplyDirectRotation();
      }
    }

    // Handle drag events
    protected override void HandleDragStart(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      _isDragging = true;
    }

    protected override void HandleDragEnd(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      _isDragging = false;
    }

    // Handle hover events
    protected override void HandlePointerOver(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      _isHovering = true;
    }

    protected override void HandlePointerOut(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      _isHovering = false;
    }

    // Public API
    public void SetAutoRotation(bool enabled) {
      EnableAutoRotation = enabled;
    }

    public void SetAutoRotationSpeed(float speed) {
      AutoRotationSpeed = speed;
    }

    public void SetAutoRotationDirection(RotationDirections direction) {
      AutoRotationDirection = direction;
    }

    public void SetRespectRotationLimits(bool respect) {
      RespectRotationLimits = respect;
    }

    public void SetPauseOnDrag(bool pause) {
      PauseOnDrag = pause;
    }

    public void SetPauseOnHover(bool pause) {
      PauseOnHover = pause;
    }

    public void ToggleAutoRotation() {
      EnableAutoRotation = !EnableAutoRotation;
    }

    public void ReverseDirection() {
      AutoRotationDirection = AutoRotationDirection == RotationDirections.Clockwise ?
                              RotationDirections.Counterclockwise : RotationDirections.Clockwise;
    }

    public bool IsCurrentlyRotating() => _isRotating && !_isPaused;

    public bool IsPaused() => _isPaused;

    public float GetEffectiveRotationSpeed() {
      if (!IsCurrentlyRotating()) return 0f;
      return AutoRotationDirection == RotationDirections.Clockwise ? -AutoRotationSpeed : AutoRotationSpeed;
    }

    // Rotation presets
    public void SetSlowRotation() {
      SetAutoRotationSpeed(30f);
    }

    public void SetMediumRotation() {
      SetAutoRotationSpeed(90f);
    }

    public void SetFastRotation() {
      SetAutoRotationSpeed(180f);
    }

    public void SetCustomRotation(float speed, RotationDirections direction) {
      SetAutoRotationSpeed(speed);
      SetAutoRotationDirection(direction);
    }

    protected override void ResetComponent() {
      // Reset to default values
      EnableAutoRotation = false;
      AutoRotationSpeed = 90f;
      AutoRotationDirection = RotationDirections.Clockwise;
      _isDragging = false;
      _isHovering = false;
    }

    protected override void FinalizeComponent() {
      // Stop any ongoing rotation
      EnableAutoRotation = false;
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

    // Gizmos for debugging auto rotation
    void OnDrawGizmosSelected() {
      if (_playableElement == null || !EnableAutoRotation) return;

      Vector3 pos = _playableElement.SnapTransform.position;

      // Show rotation direction and speed
      Gizmos.color = IsCurrentlyRotating() ? Color.green : (_isPaused ? Color.yellow : Color.red);

      // Draw rotation indicator
      float indicatorLength = Mathf.Clamp(AutoRotationSpeed / 90f, 0.5f, 3f);
      Vector3 direction = AutoRotationDirection == RotationDirections.Clockwise ?
                         Vector3.right : Vector3.left;

      // Draw arrow showing rotation direction
      Gizmos.DrawLine(pos, pos + direction * indicatorLength);
      Gizmos.DrawSphere(pos + direction * indicatorLength, 0.1f);

      // Draw curved arrow to indicate rotation
      for (int i = 0; i < 8; i++) {
        float angle1 = i * 45f;
        float angle2 = (i + 1) * 45f;
        if (AutoRotationDirection == RotationDirections.Counterclockwise) {
          angle1 = -angle1;
          angle2 = -angle2;
        }

        Vector3 p1 = pos + new Vector3(Mathf.Cos(angle1 * Mathf.Deg2Rad), Mathf.Sin(angle1 * Mathf.Deg2Rad), 0) * 0.7f;
        Vector3 p2 = pos + new Vector3(Mathf.Cos(angle2 * Mathf.Deg2Rad), Mathf.Sin(angle2 * Mathf.Deg2Rad), 0) * 0.7f;
        Gizmos.DrawLine(p1, p2);
      }

      // Show pause indicators
      if (_isPaused) {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, 1f);
      }
    }
  }
}