using UnityEngine;

namespace GMTK {
  public class SnappablePhysics : SnappableComponent {
    public enum FrictionLevel { Low, Mid, High }
    public enum BouncinessLevel { Low, Mid, High }
    public enum RotationDirections { Clockwise, Counterclockwise }

    [Header("Position and Rotation Settings")]
    [Tooltip("whether this element can change from its initial position. This properties works in tandem with PositionChangeSource. If the element only source of movement is the player, a collision with another element will not move this element")]
    public bool AllowPositionChange = false;
    [Tooltip("Whether this element can rotate")]
    public bool AllowRotation = false;
    [Tooltip("How many degrees the element will rotate every time is requested. Recommendation: multiples of 360")]
    public float RotationStep = 90;
    [Tooltip("If true, you can limit the rotation angle using MinRotationAngle and MaxRotationAngle")]
    public bool LimitRotationAngle = false;
    [Tooltip("The min and max angle the element can rotate")]
    [Range(-180f, 180f)]
    public float MinRotationAngle = -45f;
    [Range(-180f, 180f)]
    public float MaxRotationAngle = 45f;

    [Header("Auto Rotation")]
    [Tooltip("If true, the element will always rotate using the AutoRotationSpeed")]
    public bool EnableAutoRotation = false;
    [Tooltip("Degrees per second the element rotates")]
    public float AutoRotationSpeed = 90f; // degrees per second
    [Tooltip("If set to EnableAutoRotation, this property sets direction of the rotation")]
    public RotationDirections AutoRotationDirection = RotationDirections.Clockwise;

    [Header("Gravity")]
    [Tooltip("if true, the element is affected by gravity ")]
    public bool HasGravity = false;
    [Tooltip("A multiplier to increase or decrease the intensity of Gravity on the element")]
    [Range(0f, 10f)]
    public float GravityMultiplier = 1.0f;

    [Header("Material Settings")]
    [Tooltip("Friction the element puts on the marble. High=slows down the marvel. Mid=keeps the current speed. Low=smooth surface, marble will gain speed")]
    public FrictionLevel Friction = FrictionLevel.Mid;
    [Tooltip("Bounce experienced by the marble when collisioning with this element. High=The marble bounces and gains more speed. Mid=regular bounce, no force added to the ball. Low=minimum bouncing, no force added to the ball ")]
    public BouncinessLevel Bounciness = BouncinessLevel.Mid;
    [Tooltip("Optional override for experimentation. If set, this will be used instead of auto-assigned material.")]
    public PhysicsMaterial2D OverrideMaterial;

    [Header("Debug")]
    [SerializeField, DisplayWithoutEdit] private Rigidbody2D _rigidbody2D;
    [SerializeField, DisplayWithoutEdit] private PolygonCollider2D _collider2D;
    [SerializeField, DisplayWithoutEdit] private PhysicsMaterial2D _assignedMaterial;
    [SerializeField, DisplayWithoutEdit] private float _currentRotation = 0f;

    protected Vector2 _initialPosition;
    protected Vector2 _initialScale;
    protected Quaternion _initialRotation;

    protected override void Initialize() {
      //safe initial transform to be able to reset object to initial state on LevelReset
      _initialPosition = transform.position;
      _initialScale = transform.localScale;
      _initialRotation = transform.rotation;

      _rigidbody2D = _snappable.GetComponent<Rigidbody2D>();
      if (_rigidbody2D == null)
        _rigidbody2D = _snappable.gameObject.AddComponent<Rigidbody2D>();

      _rigidbody2D.gravityScale = (HasGravity) ? 1f * GravityMultiplier : 0f;
      //_rigidbody2D.angularDamping = 0.05f;
      _rigidbody2D.freezeRotation = !AllowRotation;

      _collider2D = _snappable.GetComponent<PolygonCollider2D>();
      if (_collider2D == null)
        _collider2D = _snappable.gameObject.AddComponent<PolygonCollider2D>();

      //_assignedMaterial = (OverrideMaterial != null) ? OverrideMaterial : GetPredefinedMaterial(Friction, Bounciness);
      //_collider2D.sharedMaterial = _assignedMaterial;
      _currentRotation = _initialRotation.eulerAngles.z;
      UpdateMaterial();
    }

    protected override bool Validate() => _rigidbody2D != null && _collider2D != null;

    protected override void OnUpdate() {
      //ApplyMovementControl();
      //ApplyRotationControl();
    }

    private void FixedUpdate() {
      _currentRotation = _rigidbody2D.rotation; // In degrees
      ApplyMovementControl();
      ApplyRotationControl();
    }

    public override void OnSnappableEvent(GridSnappableEventArgs eventArgs) {
      if(eventArgs.ComponentEventType == SnappableComponentEventType.RotateCW) {
        RotateBy(RotationStep);
      }
      if(eventArgs.ComponentEventType == SnappableComponentEventType.RotateCW) {
        RotateBy(-RotationStep);
      }
    }



    private void ApplyMovementControl() {
      _rigidbody2D.bodyType = (AllowPositionChange || AllowRotation) ?
            RigidbodyType2D.Dynamic : RigidbodyType2D.Static;
      //_rigidbody2D.bodyType = RigidbodyType2D.Static;

      //Apply RigidBody constraints using the 'Allow' flags
      //these should be applied regardless of the bodyType, 
      //to support bodyType changes in runtime
      if (AllowPositionChange && AllowRotation) {
        _rigidbody2D.constraints = RigidbodyConstraints2D.None;
      }
      else if (AllowPositionChange && !AllowRotation) {
        _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
      }
      else if (!AllowPositionChange && AllowRotation) {
        _rigidbody2D.constraints = RigidbodyConstraints2D.FreezePosition;
      }
      else if (!AllowPositionChange && !AllowRotation) {
        _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;
      }
    }
    private void ApplyRotationControl() {
      if (!AllowRotation) return;

      if (LimitRotationAngle) {
        if (_currentRotation < MinRotationAngle && _rigidbody2D.angularVelocity < 0f) {
          _rigidbody2D.angularVelocity = 0f;
        }
        else if (_currentRotation > MaxRotationAngle && _rigidbody2D.angularVelocity > 0f) {
          _rigidbody2D.angularVelocity = 0f;
        }
      }
    }

    public void RotateBy(float degrees) {
      if (!AllowRotation || _rigidbody2D.bodyType != RigidbodyType2D.Dynamic)
        return;
      float targetAngle = _rigidbody2D.rotation + degrees;
      if (LimitRotationAngle) {
        targetAngle = Mathf.Clamp(
           targetAngle,
           MinRotationAngle,
           MaxRotationAngle
       );
      }
      _rigidbody2D.MoveRotation(targetAngle);
    }

    public void CancelRotation() {
      _currentRotation = 0f;
      _snappable.transform.rotation = Quaternion.identity;
    }

    // Public API

    public void EnableRotation(bool enabled) {
      AllowRotation = enabled;
      if (_rigidbody2D != null)
        _rigidbody2D.freezeRotation = !enabled;
    }

    public void SetRotationSpeed(float speed) => AutoRotationSpeed = speed;
    public void SetRotationRange(float minAngle, float maxAngle) {
      MinRotationAngle = minAngle;
      MaxRotationAngle = maxAngle;
    }

    public void SetFrictionLevel(FrictionLevel level) {
      Friction = level;
      UpdateMaterial();
    }

    public void SetBouncinessLevel(BouncinessLevel level) {
      Bounciness = level;
      UpdateMaterial();
    }

    private void UpdateMaterial() {
      if (OverrideMaterial != null) return;

      _assignedMaterial = GetPredefinedMaterial(Friction, Bounciness);
      if (_collider2D != null)
        _collider2D.sharedMaterial = _assignedMaterial;
      if (_rigidbody2D != null)
        _rigidbody2D.sharedMaterial = _assignedMaterial;
    }

    private PhysicsMaterial2D GetPredefinedMaterial(FrictionLevel friction, BouncinessLevel bounce) {
      string name = $"Physics/F{friction}_B{bounce}_Material"; // e.g., FLow_BHigh_Material
      return Resources.Load<PhysicsMaterial2D>(name);
    }

    protected override void FinalizeComponent() {
      // No cleanup needed for shared materials
    }

    protected override void HandleElementSelected(object sender, GridSnappableEventArgs evt) {
    }

    protected override void HandleElementDropped(object sender, GridSnappableEventArgs evt) {
    }

    protected override void HandleElementHovered(object sender, GridSnappableEventArgs evt) {
    }

    protected override void HandleElementUnhovered(object sender, GridSnappableEventArgs evt) {
    }


    void OnDrawGizmosSelected() {
      //shows the min/max angles, if the snappable is using limits
      if (AllowRotation && LimitRotationAngle) {
        Vector3 pos = transform.position;
        float zRotation = transform.rotation.eulerAngles.z;
        Gizmos.color = Color.orangeRed;
        Gizmos.DrawLine(pos, pos + Quaternion.Euler(0, 0, zRotation + MinRotationAngle) * Vector3.right * 2f);
        Gizmos.DrawLine(pos, pos + Quaternion.Euler(0, 0, zRotation + MaxRotationAngle) * Vector3.right * 2f);
      }
    }
  }
}