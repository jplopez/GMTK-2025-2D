using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Gravity component for PlayableElement that handles gravity effects.
  /// This component manages gravity scale and provides runtime control over gravitational forces.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Element Gravity Component")]
  public class ElementGravityComponent : PlayableElementComponent {

    [Header("Gravity Settings")]
    [Tooltip("If true, the element is affected by gravity")]
    public bool HasGravity = false;
    [Tooltip("A multiplier to increase or decrease the intensity of Gravity on the element")]
    [Range(0f, 10f)]
    public float GravityMultiplier = 1.0f;

    [Header("Gravity Direction")]
    [Tooltip("Direction of gravity force. Default is down (0, -1)")]
    public Vector2 GravityDirection = Vector2.down;
    [Tooltip("If true, uses world gravity direction instead of custom direction")]
    public bool UseWorldGravity = true;

    [Header("Debug")]
    [SerializeField, DisplayWithoutEdit] private Rigidbody2D _rigidbody2D;
    [SerializeField, DisplayWithoutEdit] private float _currentGravityScale;

    private ElementPhysicsComponent _physicsComponent;

    protected override void Initialize() {
      // Get required components
      _rigidbody2D = _playableElement.GetComponent<Rigidbody2D>();
      if (_rigidbody2D == null) {
        Debug.LogWarning($"[PlayableElementGravity] No Rigidbody2D found on {_playableElement.name}. Gravity component requires Rigidbody2D to function.");
        return;
      }

      // Try to get the physics component for integration
      _physicsComponent = _playableElement.GetComponent<ElementPhysicsComponent>();

      // Apply initial gravity settings
      UpdateGravity();
    }

    protected override bool Validate() => _rigidbody2D != null;

    protected override void OnUpdate() {
      // Update gravity if settings have changed
      float targetGravityScale = HasGravity ? GravityMultiplier : 0f;
      if (Mathf.Abs(_currentGravityScale - targetGravityScale) > 0.001f) {
        UpdateGravity();
      }

      // Apply custom gravity direction if not using world gravity
      if (HasGravity && !UseWorldGravity && _rigidbody2D.bodyType == RigidbodyType2D.Dynamic) {
        ApplyCustomGravity();
      }
    }

    private void UpdateGravity() {
      if (_rigidbody2D == null) return;

      _currentGravityScale = HasGravity ? GravityMultiplier : 0f;

      if (UseWorldGravity) {
        _rigidbody2D.gravityScale = _currentGravityScale;
      }
      else {
        // Disable world gravity when using custom direction
        _rigidbody2D.gravityScale = 0f;
      }

      // Notify physics component if present
      if (_physicsComponent != null) {
        _physicsComponent.OnGravityChanged(HasGravity, GravityMultiplier);
      }
    }

    private void ApplyCustomGravity() {
      if (_rigidbody2D == null || !HasGravity) return;

      // Apply custom gravity force
      Vector2 gravityForce = GravityDirection.normalized * (Physics2D.gravity.magnitude * GravityMultiplier);
      _rigidbody2D.AddForce(gravityForce * _rigidbody2D.mass, ForceMode2D.Force);
    }

    // Public API
    public void SetGravity(bool hasGravity, float multiplier = 1.0f) {
      HasGravity = hasGravity;
      GravityMultiplier = Mathf.Clamp(multiplier, 0f, 10f);
      UpdateGravity();
    }

    public void SetGravityDirection(Vector2 direction) {
      GravityDirection = direction.normalized;
    }

    public void SetUseWorldGravity(bool useWorld) {
      UseWorldGravity = useWorld;
      UpdateGravity();
    }

    public void ToggleGravity() {
      SetGravity(!HasGravity, GravityMultiplier);
    }

    public float GetCurrentGravityScale() => _currentGravityScale;

    public bool IsAffectedByGravity() => HasGravity && _currentGravityScale > 0f;

    protected override void ResetComponent() {
      // Reset to initial values would require storing them
      // For now, just ensure gravity is updated
      UpdateGravity();
    }

    protected override void FinalizeComponent() {
      // Reset gravity to default when component is destroyed
      if (_rigidbody2D != null) {
        _rigidbody2D.gravityScale = 1f; // Unity default
      }
    }

    // Gizmos for debugging gravity
    void OnDrawGizmosSelected() {
      if (_playableElement == null || !HasGravity) return;

      Vector3 pos = _playableElement.SnapTransform.position;

      // Draw gravity direction
      if (!UseWorldGravity) {
        Gizmos.color = Color.cyan;
        Vector3 gravityDir = new Vector3(GravityDirection.x, GravityDirection.y, 0) * 2f * GravityMultiplier;
        Gizmos.DrawLine(pos, pos + gravityDir);
        Gizmos.DrawSphere(pos + gravityDir, 0.1f);
      }
      else {
        // Show world gravity influence
        Gizmos.color = Color.blue;
        Vector3 worldGravityDir = new Vector3(0, -1, 0) * GravityMultiplier;
        Gizmos.DrawLine(pos, pos + worldGravityDir);
        Gizmos.DrawSphere(pos + worldGravityDir, 0.1f);
      }
    }
  }
}