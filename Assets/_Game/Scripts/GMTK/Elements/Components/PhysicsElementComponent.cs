using UnityEngine;
using MoreMountains.Feedbacks;
using GMTK.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GMTK {

  public enum CollisionSourceFilter { Everything, MarbleOnly, ElementsOnly }

  [Flags]
  public enum TransformChangeSource { None = 0, OnCollision = 1, OnDrag = 2  }

  /// <summary>
  /// Physics component for PlayableElement that handles rotation, movement, and collision behavior.
  /// Auto Rotation, Gravity and Material properties are now handled by separate components.
  /// 
  /// <para>Implemented Lifecycle methods:</para>
  ///   1. Initialize<br/>
  ///   2. Validate<br/>
  ///   3. OnUpdate<br/>
  ///   4. ResetComponent<br/>
  ///   5. FinalizeComponent
  /// 
  /// <para>Implemented Event Listeners:</para>
  ///   1. OnDragStart - UnityEvent<br/>
  ///   2. OnDragEnd - UnityEvent<br/>
  ///   3. OnCollisionEnter2D - Physics<br/>
  ///   4. OnCollisionStay2D - Physics<br/>
  ///   5. OnCollisionExit2D - Physics<br/>
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Physics Element Component")]
  public class PhysicsElementComponent : PlayableElementComponent {

    #region Serialized Fields

    [Header("Collision Settings")]
    [Space]
    public bool DisableAllCollisions = false;
    [Space]
    [Tooltip("If true, the element will dispatch GameEvents on collisions. Otherwise, collisions won't be notified. Set it to false if the element will be static in the level.")]
    [MMFCondition("DisableAllCollisions", Hidden = true, Negative = true)]
    public bool TriggerCollisionEvents = true;

    [Space(10)]
    [Header("Movement Settings")]
    [Space]

    [Tooltip("Whether this element can change its position when colliding (default) with other element, while dragging when overlaping with other elements, both or none.")]
    [MMFCondition("DisableAllCollisions", Hidden = true, Negative = true)]
    public TransformChangeSource PositionChanges = TransformChangeSource.None;
    [Tooltip("Determines which objects can move this element on collisions")]
    [MMFCondition("DisableAllCollisions", Hidden = true, Negative = true)]
    public CollisionSourceFilter CanBeMovedBy = CollisionSourceFilter.Everything;
    [Tooltip("MMF Player for collision feedback")]
    public MMF_Player PositionChangeFeedback;

    [Header("Movement Constraints")]

    [MMFEnumCondition("PositionChanges", (int)TransformChangeSource.None)]
    [Help("Add a Position Change condition to enable movement constrains",UnityEditor.MessageType.None)]
    [DisplayWithoutEdit] public string NoMovementConstrains = string.Empty;

    [MMFEnumCondition("PositionChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("If true, the element's movement will be constrained vertically (Y axis)")]
    public bool ConstrainVerticalMovement = false;
    [MMFEnumCondition("PositionChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("If true, the element's movement will be constrained horizontally (X axis)")]
    public bool ConstrainHorizontalMovement = false;
    [MMFEnumCondition("PositionChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("Minimum position bounds for the element")]
    public Vector2Int MinPosition = new(-100, -100);
    [MMFEnumCondition("PositionChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("Maximum position bounds for the element")]
    public Vector2Int MaxPosition = new(100, 100);
    [MMFEnumCondition("PositionChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("If true, the element's movement will be constrained to a grid (eg: only move in increments of grid cell size)")]
    public bool SnapToGridOnMove = true;
    [MMFEnumCondition("PositionChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("If true, the element will not be able to move through other elements (eg: will collide and stop)")]
    public bool SolidOnCollision = true;
  
    [Space(10)]
    [Header("Rotation Settings")]
    [Space]

    [Tooltip("Whether this element can change its rotation when colliding (default) with other element, while dragging when overlaping with other elements, both or none.")]
    [MMFCondition("DisableAllCollisions", Hidden = true, Negative = true)]
    public TransformChangeSource RotationChanges = TransformChangeSource.None;
    [Tooltip("Determines which objects can rotate this element through collision")]
    [MMFCondition("DisableAllCollisions", Hidden = true, Negative = true)]
    public CollisionSourceFilter CanBeRotatedBy = CollisionSourceFilter.Everything;
    [Tooltip("MMF Player for rotation feedback")]
    public MMF_Player RotationChangeFeedback;
    
    [Header("Rotation Constrains")]

    [MMFEnumCondition("RotationChanges", (int)TransformChangeSource.None)]
    [Help("Add a Rotation Change condition to enable rotation constrains", UnityEditor.MessageType.None)]
    [DisplayWithoutEdit] public string NoRotationConstrains = string.Empty;

    [MMFEnumCondition("RotationChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("How many degrees the element will rotate every time is requested. Uses extension methods that handle flip state automatically.")]
    public float RotationStep = 90;
    [MMFEnumCondition("RotationChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("If true, you can limit the rotation angle using MinRotationAngle and MaxRotationAngle")]
    public bool LimitRotationAngle = false;
    [MMFEnumCondition("RotationChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Tooltip("The min and max angle the element can rotate")]
    [Range(-180f, 180f)]
    public float MinRotationAngle = -45f;
    [MMFEnumCondition("RotationChanges", (int)TransformChangeSource.OnDrag, (int)TransformChangeSource.OnCollision)]
    [Range(-180f, 180f)]
    public float MaxRotationAngle = 45f;

    [Space(10)]
    [Header("Feedbacks")]
    [Tooltip("Collision intensity calculator to use for this object. If null, no collision intensity will be calculated.")]
    public CollisionIntensityCalculator IntensityCalculator;
    [Space(5)]
    [Tooltip("MMF Player for constraint violation feedback")]
    public MMF_Player ConstraintViolationFeedback;

    [Space(10)]
    [Header("Debug")]
    [SerializeField, DisplayWithoutEdit] private Rigidbody2D _rigidbody2D;
    [SerializeField, DisplayWithoutEdit] private PolygonCollider2D _collider2D;
    [SerializeField, DisplayWithoutEdit] private float _currentRotation = 0f;
    [SerializeField, DisplayWithoutEdit] private bool _pendingPositionCollision = false;
    [SerializeField, DisplayWithoutEdit] private bool _pendingRotationCollision = false;
    [SerializeField, DisplayWithoutEdit] private Vector3 _pendingCollisionForce = Vector3.zero;
    [SerializeField, DisplayWithoutEdit] private float _pendingRotationForce = 0f;

    #endregion

    #region Private/Protected Fields

    protected Vector2 _initialPosition;
    protected Vector2 _initialScale;
    protected Quaternion _initialRotation;
    private Vector3 _lastValidPosition;
    private Vector3 _lastValidRotation;
    private bool _isValidatingMovement = false;
    //private bool _isDragOverride = false; // Track when dragging should override collision settings

    // Component references
    private GravityElementComponent _gravityComponent;
    private PhysicalMaterialsElementComponent _materialComponent;
    private AutoRotationElementComponent _autoRotationComponent;

    private CollisionState _currentCollisionState;

    #endregion

    #region Public fields

    /*
     * TODO
     * flag methods for rigidbody
     * test component
     * reduce boilerplate
     */

    // rigidbody constrains
    public bool RigidbodyCanRotate => _rigidbody2D != null && _rigidbody2D.bodyType != RigidbodyType2D.Static && !_rigidbody2D.constraints.HasFlag(RigidbodyConstraints2D.FreezeRotation);

    public bool RigidbodyCanMove => _rigidbody2D != null && _rigidbody2D.bodyType != RigidbodyType2D.Static && !_rigidbody2D.constraints.HasFlag(RigidbodyConstraints2D.FreezePosition);

    public bool RigidBodyCanMoveAndRotate => RigidbodyCanMove && RigidbodyCanRotate;

    // position change flags
    public bool AllowsPositionChanges => PositionChanges != TransformChangeSource.None;
    public bool PositionChangesDisabled => !AllowsPositionChanges;
    public bool AllowsPositionChangesOnCollision => AllowsPositionChanges && PositionChanges.HasFlag(TransformChangeSource.OnCollision);
    public bool AllowsPositionChangesWhileDragging => AllowsPositionChanges && PositionChanges.HasFlag(TransformChangeSource.OnDrag) && _playableElement.IsDraggable;

    // rotation change flags
    public bool AllowsRotationChanges => RotationChanges != TransformChangeSource.None && _playableElement.CanRotate;
    public bool RotationChangesDisabled => !AllowsRotationChanges;
    public bool AllowsRotationChangesOnCollision => AllowsRotationChanges && RotationChanges.HasFlag(TransformChangeSource.OnCollision);
    public bool AllowsRotationChangesWhileDragging => AllowsRotationChanges && RotationChanges.HasFlag(TransformChangeSource.OnDrag);
    // combo flags
    public bool AllowsPositionAndRotationChanges => AllowsPositionChanges && AllowsRotationChanges;
    public bool AllowsAllOnCollision => AllowsPositionChangesOnCollision && AllowsRotationChangesOnCollision;
    public bool AllowsAllWhileDragging => AllowsPositionChangesWhileDragging && AllowsRotationChangesWhileDragging;

    // all transform and rigidbody flags are disabbled
    public bool IsStaticElement => !AllowsPositionAndRotationChanges && !RigidBodyCanMoveAndRotate;
    #endregion

    #region Component Methods

    protected override void Initialize() {
      // Store initial transform to be able to reset object to initial state on LevelReset
      _initialPosition = _playableElement.SnapTransform.position;
      _initialScale = _playableElement.SnapTransform.localScale;
      _initialRotation = _playableElement.SnapTransform.rotation;
      _lastValidPosition = _initialPosition;
      _lastValidRotation = _initialRotation.eulerAngles;

      // Get or add Rigidbody2D
      _rigidbody2D = _playableElement.GetComponent<Rigidbody2D>();
      if (_rigidbody2D == null)
        _rigidbody2D = _playableElement.gameObject.AddComponent<Rigidbody2D>();

      // Get optional component references
      _gravityComponent = _playableElement.GetComponent<GravityElementComponent>();
      _materialComponent = _playableElement.GetComponent<PhysicalMaterialsElementComponent>();
      _autoRotationComponent = _playableElement.GetComponent<AutoRotationElementComponent>();

      // Initialize with default gravity (will be overridden by gravity component if present)
      if (_gravityComponent == null) {
        _rigidbody2D.gravityScale = 0f; // Default to no gravity if no gravity component
      }

      // Get or add PolygonCollider2D
      _collider2D = _playableElement.GetComponent<PolygonCollider2D>();
      if (_collider2D == null)
        _collider2D = _playableElement.gameObject.AddComponent<PolygonCollider2D>();

      // Configure collider for solid collision if needed
      if (SolidOnCollision) {
        _collider2D.isTrigger = false;
      }

      _currentRotation = _initialRotation.eulerAngles.z;

      //// Handle rotation override
      //InitRotationOverrides();

      // Apply initial movement control
      InitMovementControls();
    }

    protected override void ResetComponent() {
      _playableElement.SnapTransform.position = _initialPosition;
      _playableElement.SnapTransform.localScale = _initialScale;
      _playableElement.SnapTransform.rotation = _initialRotation;
      _currentRotation = _initialRotation.eulerAngles.z;
      if (_rigidbody2D.bodyType != RigidbodyType2D.Static) {
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
      }
      _lastValidPosition = _initialPosition;
      _lastValidRotation = _initialRotation.eulerAngles;
      //_isDragOverride = false;
      _currentCollisionState.Reset();
    }

    protected override bool Validate() => _rigidbody2D != null && _collider2D != null;

    protected override void OnUpdate() {
      _currentRotation = _rigidbody2D.rotation; // In degrees

      // Process any pending collisions first
      UpdatePendingCollisions();

      // Prime other components
      UpdateOtherComponents();

      // Apply all controls
      InitMovementControls();
      UpdateRotationControls();
      UpdateMovementConstraints();
      UpdateCollisionRules();

      // Additional validation for rotation limits during any movement
      UpdateRotationLimits();

      // Reset collision state for next frame
      _currentCollisionState.Reset();
    }

    protected override void FinalizeComponent() {
      // No cleanup needed
    }
    #endregion

    #region Initialize/OnUpdate Methods
    protected virtual void InitMovementControls() {
      // Determine what movements are allowed
      bool canMoveFromCollision = AllowsPositionChangesOnCollision && RigidbodyCanMove;
      bool canMoveFromDrag = AllowsPositionChangesWhileDragging;
      bool canRotateFromCollision = AllowsRotationChangesOnCollision && RigidbodyCanRotate;
      bool canRotateFromInput = _playableElement.CanRotate;
      bool needsGravity = _gravityComponent != null && _gravityComponent.IsAffectedByGravity();
      bool hasAutoRotation = _autoRotationComponent != null && _autoRotationComponent.IsCurrentlyRotating();

      // Determine if rigidbody should be dynamic
      bool shouldBeDynamic = canMoveFromCollision || canMoveFromDrag || canRotateFromCollision || canRotateFromInput || needsGravity || hasAutoRotation;

      _rigidbody2D.bodyType = shouldBeDynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Static;

      // Start with no constraints
      RigidbodyConstraints2D constraints = RigidbodyConstraints2D.None;

      // Apply position constraints
      if (!canMoveFromCollision && !canMoveFromDrag && !needsGravity) {
        constraints |= RigidbodyConstraints2D.FreezePosition;
      }
      else {
        // Apply axis-specific constraints
        if (ConstrainHorizontalMovement) {
          constraints |= RigidbodyConstraints2D.FreezePositionX;
        }
        if (ConstrainVerticalMovement) {
          constraints |= RigidbodyConstraints2D.FreezePositionY;
        }
      }

      // Apply rotation constraints
      if (!canRotateFromCollision && !canRotateFromInput && !hasAutoRotation) {
        constraints |= RigidbodyConstraints2D.FreezeRotation;
      }

      _rigidbody2D.constraints = constraints;

      // Special handling for immovable objects that need to remain kinematic for collision detection
      if (PositionChangesDisabled && !canRotateFromInput && RotationChangesDisabled && !hasAutoRotation) {
        // For truly static elements, we can make them kinematic to ensure they never move
        // but still participate in collision detection
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeAll;
      }
    }

    /// <summary>
    /// Process collisions added to the _currentCollisionState during the frame.
    /// </summary>
    private void UpdatePendingCollisions() {
      // Process position collision if valid
      if (_currentCollisionState.hasValidPositionCollision && AllowsPositionChanges) {
        ApplyPositionCollisionForce(_currentCollisionState.accumulatedForce);
        _pendingPositionCollision = true;
      }
      else {
        _pendingPositionCollision = false;
      }

      // Process rotation collision if valid
      if (_currentCollisionState.hasValidRotationCollision && AllowsRotationChangesOnCollision) {
        ApplyRotationCollisionForce(_currentCollisionState.accumulatedTorque);
        _pendingRotationCollision = true;
      }
      else {
        _pendingRotationCollision = false;
      }

      // Update debug values
      _pendingCollisionForce = _currentCollisionState.accumulatedForce;
      _pendingRotationForce = _currentCollisionState.accumulatedTorque;
    }


    /// <summary>
    /// TODO: Prime other components like gravity, material, auto-rotation if they exist.
    /// Not needed for now, this method is to make room for it in the future, or for extensions.
    /// </summary>
    protected virtual void UpdateOtherComponents() {
      // Prime gravity component
      if (_gravityComponent != null) {
        // Gravity component handles its own priming
      }

      // Prime material component
      if (_materialComponent != null) {
        // Material component handles its own priming
      }

      // Prime auto rotation component
      if (_autoRotationComponent != null) {
        // Auto rotation component handles its own priming
      }
    }

    protected virtual void UpdateRotationControls() {
      // Auto rotation is now handled by AutoRotationElementComponent
      // Only apply manual rotation limits here
      if (RotationChangesDisabled) return;

      // Apply rotation limits by stopping angular velocity at limits
      if (LimitRotationAngle) {
        if (_currentRotation < MinRotationAngle && _rigidbody2D.angularVelocity < 0f) {
          _rigidbody2D.angularVelocity = 0f;
        }
        else if (_currentRotation > MaxRotationAngle && _rigidbody2D.angularVelocity > 0f) {
          _rigidbody2D.angularVelocity = 0f;
        }
      }
    }

    protected virtual void UpdateMovementConstraints() {
      // Only apply constraints if movement is allowed or element is being dragged
      if (!AllowsPositionChanges) return;

      Vector3 currentPos = _playableElement.SnapTransform.position;
      Vector3 constrainedPos = ApplyPositionConstraints(currentPos);

      if (constrainedPos != currentPos && !_isValidatingMovement) {
        _isValidatingMovement = true;
        _playableElement.SnapTransform.position = constrainedPos;
        if (_rigidbody2D.bodyType == RigidbodyType2D.Dynamic) {
          _rigidbody2D.position = constrainedPos;
        }
        _lastValidPosition = constrainedPos;
        _isValidatingMovement = false;
      }
    }

    protected virtual void UpdateCollisionRules() {
      // Enforce position collision rules
      if (AllowsPositionChangesOnCollision) {
        Vector3 currentPos = _playableElement.SnapTransform.position;
        if (Vector3.Distance(currentPos, _lastValidPosition) > 0.01f) {
          RestoreLastValidPosition();
        }
      }
      else {
        // Update last valid position if movement is allowed
        _lastValidPosition = _playableElement.SnapTransform.position;
      }

      // Enforce rotation collision rules
      //bool rotationAllowed = AllowsRotationChangesOnCollision || AllowsRotationChangesWhileDragging; 
      if (RotationChangesDisabled) {
        // Check if rotation has changed when it shouldn't have
        float currentRotationZ = _rigidbody2D.rotation;
        float lastValidRotationZ = _lastValidRotation.z;

        if (Mathf.Abs(Mathf.DeltaAngle(currentRotationZ, lastValidRotationZ)) > 0.1f) {
          RestoreLastValidRotation();
        }
      }
      else {
        // Rotation is allowed, update our last valid rotation to current
        // This prevents UpdateRotationLimits from conflicting with allowed rotations
        _lastValidRotation = new Vector3(0, 0, _rigidbody2D.rotation);
      }
    }

    protected virtual void UpdateRotationLimits() {
      // Only validate if rotation limits are enabled and rotation is allowed in some form
      //bool rotationAllowed = AllowRotation || ChangeRotationOnCollision || _playableElement.IsDraggable; // ? why draggable is included
      if (!LimitRotationAngle || RotationChangesDisabled) return;

      // Get the current rotation from rigidbody (single source of truth)
      float currentRotation = _rigidbody2D.rotation;

      // Normalize angle to match our limit range (-180 to 180)
      if (currentRotation > 180f) {
        currentRotation -= 360f;
      }
      if (currentRotation < -180f) {
        currentRotation += 360f;
      }

      // Check if rotation exceeds limits and needs clamping
      if (currentRotation < MinRotationAngle || currentRotation > MaxRotationAngle) {
        // Feel integration - constraint violation feedback
        if (ConstraintViolationFeedback != null) {
          ConstraintViolationFeedback.PlayFeedbacks(transform.position, 1f);
        }

        float clampedRotation = Mathf.Clamp(currentRotation, MinRotationAngle, MaxRotationAngle);
        SetRotation(clampedRotation, true);
      }
    }

    /// <summary>
    /// Ensures PlayableElement and this component have consistent movement control settings.
    /// </summary>

    #endregion

    #region Event Handlers 
    public override void OnDragStart(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      if (evt.Handled) return;
      //_isDragOverride = true;
      InitMovementControls(); // Update rigidbody settings for dragging
    }

    public override void OnDragEnd(PlayableElementEventArgs evt) {
      if (evt.Element != _playableElement) return;
      if (evt.Handled) return;
      //_isDragOverride = false;
      InitMovementControls(); // Restore normal rigidbody settings
    }

    public override void OnRotate(PlayableElementEventArgs args) {
      if (args.Element != _playableElement) return;
      if (args.Handled) return;
      // this event responds to player input, so is only relevant to know 
      // if playableElement.CanRotate == true
      if (_playableElement.CanRotate) {
        bool clockwise = args.EventType == PlayableElementEventType.RotateCW;
        ApplyExtensionRotation(clockwise);
        args.Handled = true; // Prevent default rotation behavior
      }
    }

    // Enhanced collision handling - these methods now prime the component for processing
    protected virtual void OnCollisionEnter2D(Collision2D collision) {
      PrimeCollisionProcessing(collision);

      // Feel integration - play collision feedback based on force
      if (PositionChangeFeedback != null) {
        float intensity = CalculateCollisionIntensity(collision);
        PositionChangeFeedback.PlayFeedbacks(transform.position, intensity);
      }
      RaiseCollisionEvent(PlayableElementEventType.CollisionStart, collision);
    }

    protected virtual void OnCollisionExit2D(Collision2D collision) {
      PrimeCollisionProcessing(collision);
      RaiseCollisionEvent(PlayableElementEventType.CollisionEnd, collision);
    }

    protected virtual void OnCollisionStay2D(Collision2D collision) {
      PrimeCollisionProcessing(collision);

      // Continuously enforce collision rules during contact
      if (SolidOnCollision) {
        ValidateCurrentPosition();
      }
      RaiseCollisionEvent(PlayableElementEventType.Collisioning, collision);
    }

    private void RaiseCollisionEvent(PlayableElementEventType eventType, Collision2D collision) {
      // handle when collision events are disabled
      if (!TriggerCollisionEvents) return;

      List<ContactPoint2D> collisionPoints = new();
      if (collision.GetContacts(collisionPoints) > 0) {
        var otherObject = collision.gameObject;
        var collisionPoint = collisionPoints.First().point;
        RaisePlayableElementEvent(eventType, collisionPoint, otherObject);
      }
    }

    //protected void OnRotateCW(PlayableElementEventArgs evt) {
    //  if (AllowsRotationChanges) {
    //    ApplyExtensionRotation(true); // Use extension method for clockwise rotation
    //    evt.Handled = true; // Prevent default rotation behavior
    //  }
    //}
    //protected void OnRotateCCW(PlayableElementEventArgs evt) {
    //  if (AllowsRotationChanges) {
    //    ApplyExtensionRotation(false); // Use extension method for counter-clockwise rotation
    //    evt.Handled = true; // Prevent default rotation behavior
    //  }
    //}

    #endregion

    #region Collision

    private void ApplyPositionCollisionForce(Vector3 force) {
      if (!AllowsPositionChangesOnCollision) return;

      // Apply the accumulated force from collisions
      if (force.magnitude > 0.001f) {
        Vector3 currentPos = _playableElement.SnapTransform.position;
        Vector3 newPos = currentPos + force * Time.deltaTime;
        Vector3 constrainedPos = ApplyPositionConstraints(newPos);

        if (constrainedPos != currentPos) {
          _playableElement.SnapTransform.position = constrainedPos;
          _rigidbody2D.position = constrainedPos;
        }
      }
    }

    private void ApplyRotationCollisionForce(float torque) {
      if (!AllowsRotationChangesOnCollision) return;

      // Apply the accumulated torque from collisions
      if (Mathf.Abs(torque) > 0.001f) {
        // Use extension methods for collision-based rotation with RotationStep
        if (torque > 0) {
          ApplyExtensionRotation(true); // Clockwise
        }
        else {
          ApplyExtensionRotation(false); // Counter-clockwise
        }
      }
    }

    private bool IsCollisionSourceValid(GameObject collisionObject, CollisionSourceFilter filter) {
      if (collisionObject == null) return false;

      switch (filter) {
        case CollisionSourceFilter.Everything:
          return true;

        case CollisionSourceFilter.MarbleOnly:
          // Check if the colliding object is a marble
          // You might need to adjust this based on how you identify marbles in your game
          return collisionObject.CompareTag("Marble") || collisionObject.name.ToLower().Contains("marble");

        case CollisionSourceFilter.ElementsOnly:
          // Check if the colliding object is another playable element
          return collisionObject.GetComponent<PlayableElement>() != null ||
                 collisionObject.GetComponent<PhysicsElementComponent>() != null;

        default:
          return false;
      }
    }

    public virtual float CalculateCollisionIntensity(Collision2D collision) {
      if (IntensityCalculator != null) {
        if (IsCollisionSourceValid(collision.gameObject, CanBeMovedBy)
          || IsCollisionSourceValid(collision.gameObject, CanBeRotatedBy)) {
          return IntensityCalculator.CalculateCurrentIntensity();
        }
        else {
          return IntensityCalculator.DefaultIntensity;
        }
      }
      return 1f; // Default intensity
    }

    private void PrimeCollisionProcessing(Collision2D collision) {
      GameObject collisionObject = collision.gameObject;

      // Check if this collision should affect position
      if (AllowsPositionChangesOnCollision && IsCollisionSourceValid(collisionObject, CanBeMovedBy)) {
        _currentCollisionState.hasValidPositionCollision = true;

        // Calculate collision force based on contact points
        Vector3 collisionForce = CalculateCollisionForce(collision);
        _currentCollisionState.accumulatedForce += collisionForce;
      }

      // Check if this collision should affect rotation
      if (AllowsRotationChangesOnCollision && IsCollisionSourceValid(collisionObject, CanBeRotatedBy)) {
        _currentCollisionState.hasValidRotationCollision = true;

        // Calculate collision torque based on contact points
        float collisionTorque = CalculateCollisionTorque(collision);
        _currentCollisionState.accumulatedTorque += collisionTorque;
      }

      _currentCollisionState.collisionCount++;
    }

    private Vector3 CalculateCollisionForce(Collision2D collision) {
      Vector3 totalForce = Vector3.zero;

      // Use contact points to calculate realistic collision force
      for (int i = 0; i < collision.contactCount; i++) {
        ContactPoint2D contact = collision.contacts[i];
        Vector3 force = contact.normal * collision.relativeVelocity.magnitude * 0.1f; // Scale factor
        totalForce += force;
      }

      return totalForce;
    }

    private float CalculateCollisionTorque(Collision2D collision) {
      float totalTorque = 0f;
      Vector3 centerOfMass = _playableElement.SnapTransform.position;

      // Use contact points to calculate realistic collision torque
      for (int i = 0; i < collision.contactCount; i++) {
        ContactPoint2D contact = collision.contacts[i];
        Vector3 contactPoint = contact.point;
        Vector3 contactVector = contactPoint - centerOfMass;

        // Calculate torque using cross product (simplified for 2D)
        float torque = contactVector.x * contact.normal.y - contactVector.y * contact.normal.x;
        torque *= collision.relativeVelocity.magnitude * 0.1f; // Scale factor
        totalTorque += torque;
      }

      return totalTorque;
    }

    #endregion

    #region Position 
    private void RestoreLastValidPosition() {
      if (!_isValidatingMovement) {
        _isValidatingMovement = true;
        _playableElement.SnapTransform.position = _lastValidPosition;
        _rigidbody2D.position = _lastValidPosition;
        if(_rigidbody2D.bodyType != RigidbodyType2D.Static) _rigidbody2D.linearVelocity = Vector2.zero; // Stop any movement
        _isValidatingMovement = false;
        //this.Log($"RestoreLastValidPosition {_playableElement.name} : {_lastValidPosition}");
      }
    }

    private Vector3 ApplyPositionConstraints(Vector3 position) {
      Vector3 constrainedPos = position;

      // Apply movement axis constraints
      if (ConstrainHorizontalMovement) {
        constrainedPos.x = _initialPosition.x;
      }
      if (ConstrainVerticalMovement) {
        constrainedPos.y = _initialPosition.y;
      }

      // Apply boundary constraints
      constrainedPos.x = Mathf.Clamp(constrainedPos.x, MinPosition.x, MaxPosition.x);
      constrainedPos.y = Mathf.Clamp(constrainedPos.y, MinPosition.y, MaxPosition.y);

      // Apply grid snapping if enabled
      if (SnapToGridOnMove && _levelGrid != null) {
        //Vector2Int gridPos = _levelGrid.WorldToGrid(constrainedPos);
        constrainedPos = _levelGrid.SnapToGrid(constrainedPos);
      }

      return constrainedPos;
    }

    private void ValidateCurrentPosition() {
      Vector3 currentPos = _playableElement.SnapTransform.position;

      // Check if current position violates solid collision rules
      if (SolidOnCollision && IsPositionBlocked(currentPos)) {
        // Revert to last valid position
        RestoreLastValidPosition();
      }
      else {
        _lastValidPosition = currentPos;
      }
    }

    private bool IsPositionBlocked(Vector3 position) {
      // Perform a simple overlap check to see if position is blocked
      Collider2D[] overlapping = Physics2D.OverlapBoxAll(
        position,
        _collider2D.bounds.size,
        _playableElement.SnapTransform.rotation.eulerAngles.z
      );

      foreach (var collider in overlapping) {
        if (collider != _collider2D && collider.gameObject != _playableElement.gameObject) {
          // Check if the overlapping object also has solid collision
          var otherPhysics = collider.GetComponent<PhysicsElementComponent>();
          if (otherPhysics != null && otherPhysics.SolidOnCollision) {
            return true;
          }
        }
      }
      return false;
    }

    #endregion

    #region Rotation

    protected virtual void RestoreLastValidRotation() => SetRotation(_lastValidRotation.z, true); // Force restore the rotation

    /// <summary>
    /// Applies rotation using the TransformGameExtensions with flip-state awareness.
    /// This is the unified rotation method that handles all rotation scenarios.
    /// </summary>
    /// <param name="clockwise">If true, rotates clockwise; otherwise counter-clockwise</param>
    /// <param name="skipPermissionCheck">If true, applies rotation regardless of permissions</param>
    private void ApplyExtensionRotation(bool clockwise, bool skipPermissionCheck = false) {
      // apply them only if allows rotation or skipping permission check
      if (!skipPermissionCheck || (AllowsRotationChanges && _rigidbody2D.bodyType != RigidbodyType2D.Static)) {

        // Store current rotation for validation
        float previousRotation = _rigidbody2D.rotation;

        // Apply the rotation using extension methods with RotationStep parameter
        if (clockwise) {
          _playableElement.SnapTransform.RotateClockwise(RotationStep);
        }
        else {
          _playableElement.SnapTransform.RotateCounterClockwise(RotationStep);
        }

        // Get the new rotation after applying extension method
        float newRotation = _playableElement.SnapTransform.eulerAngles.z;

        // Apply rotation limits if enabled
        if (LimitRotationAngle) {
          // Normalize to -180 to 180 range for comparison
          float normalizedRotation = newRotation;
          if (normalizedRotation > 180f) normalizedRotation -= 360f;
          if (normalizedRotation < -180f) normalizedRotation += 360f;

          if (normalizedRotation < MinRotationAngle || normalizedRotation > MaxRotationAngle) {
            // Revert to previous rotation if it exceeds limits
            SetRotation(previousRotation, true);
            return;
          }
        }

        // Sync the rigidbody rotation with transform
        _rigidbody2D.MoveRotation(newRotation);
        _rigidbody2D.angularVelocity = 0f; // Stop any ongoing rotation

        // Update tracking variables
        _currentRotation = newRotation;
        _lastValidRotation = new Vector3(0, 0, newRotation);

        this.LogDebug($"Applied extension rotation {(clockwise ? "CW" : "CCW")} to {newRotation}° with step {RotationStep}° (flippedX: {_playableElement.SnapTransform.IsFlippedX()}, flippedY: {_playableElement.SnapTransform.IsFlippedY()})");

        // Feel integration - provide rotation feedback
        if (RotationChangeFeedback != null && newRotation != previousRotation) {
          RotationChangeFeedback.PlayFeedbacks(transform.position, 1f);
        }
      }
    }

    /// <summary>
    /// Centralized method to apply rotation changes with proper limit checking.
    /// This method is kept for backward compatibility but now delegates to extension methods when possible.
    /// </summary>
    /// <param name="degrees">Degrees to rotate by</param>
    /// <param name="skipPermissionCheck">If true, applies rotation regardless of permissions (used for corrections)</param>
    private void ApplyRotationChange(float degrees, bool skipPermissionCheck = false) {
      // apply them only if allows rotation or skipping permission check
      if (!skipPermissionCheck || (AllowsRotationChanges && _rigidbody2D.bodyType != RigidbodyType2D.Static)) {

        // If the rotation amount matches our RotationStep, use extension methods for flip-state awareness
        if (Mathf.Approximately(Mathf.Abs(degrees), RotationStep)) {
          ApplyExtensionRotation(degrees < 0, skipPermissionCheck);
          return;
        }

        // Fall back to traditional rotation for non-standard angles
        float currentAngle = _rigidbody2D.rotation;
        float targetAngle = currentAngle + degrees;

        this.LogDebug($"Applying rotation change: {degrees} degrees (current: {currentAngle}, target: {targetAngle})");

        // Apply limits if enabled
        if (LimitRotationAngle) {
          targetAngle = Mathf.Clamp(targetAngle, MinRotationAngle, MaxRotationAngle);

        }

        // Apply the rotation using centralized method
        SetRotation(targetAngle);
      }
    }

    /// <summary>
    /// Centralized method to set rotation to both rigidbody and transform
    /// This ensures both are always in sync
    /// </summary>
    /// <param name="angle">Target angle in degrees</param>
    /// <param name="force">If true, sets rotation regardless of body type</param>
    private void SetRotation(float angle, bool force = false) {
      if (!force && _rigidbody2D.bodyType == RigidbodyType2D.Static) return;

      // Apply to rigidbody first (primary source of truth for physics)
      _rigidbody2D.MoveRotation(angle);
      _rigidbody2D.angularVelocity = 0f; // Stop any ongoing rotation

      // Ensure transform is synchronized
      _playableElement.SnapTransform.rotation = Quaternion.Euler(0, 0, angle);

      // Update our tracking variables
      _currentRotation = angle;
      _lastValidRotation = new Vector3(0, 0, angle);

      this.LogDebug($"Set rotation to {angle} degrees");
    }

    #endregion

    // Public API methods - using extension-based rotation logic
    #region Public API

    public void RotateBy(float degrees) {
      // Use extension methods if the degrees match RotationStep for consistency
      if (Mathf.Approximately(Mathf.Abs(degrees), RotationStep)) {
        ApplyExtensionRotation(degrees < 0);
      }
      else {
        ApplyRotationChange(degrees);
      }
    }

    /// <summary>
    /// Rotates the element clockwise using the extension method with flip-state awareness.
    /// </summary>
    public void RotateClockwise() {
      ApplyExtensionRotation(true);
    }

    /// <summary>
    /// Rotates the element counter-clockwise using the extension method with flip-state awareness.
    /// </summary>
    public void RotateCounterClockwise() {
      ApplyExtensionRotation(false);
    }

    /// <summary>
    /// Rotates the element by a specific angle using extension methods.
    /// </summary>
    /// <param name="angle">The angle to rotate by</param>
    /// <param name="clockwise">If true, rotates clockwise; otherwise counter-clockwise</param>
    public void RotateByAngle(float angle, bool clockwise = true) {
      if (clockwise) {
        _playableElement.SnapTransform.RotateClockwise(angle);
      }
      else {
        _playableElement.SnapTransform.RotateCounterClockwise(angle);
      }

      // Sync with rigidbody
      float newRotation = _playableElement.SnapTransform.eulerAngles.z;
      _rigidbody2D.MoveRotation(newRotation);
      _currentRotation = newRotation;
      _lastValidRotation = new Vector3(0, 0, newRotation);
    }

    /// <summary>
    /// Snaps the current rotation to the nearest cardinal direction (0, 90, 180, 270).
    /// Useful for correcting rotation after free rotation or collision.
    /// </summary>
    public void SnapToCardinal() {
      int cardinalRotation = _playableElement.SnapTransform.GetCardinalRotation();
      SetRotation(cardinalRotation, true);
    }

    /// <summary>
    /// Gets the current cardinal rotation of the element (0, 90, 180, 270).
    /// </summary>
    /// <returns>The current rotation rounded to the nearest 90-degree increment</returns>
    public int GetCardinalRotation() {
      return _playableElement.SnapTransform.GetCardinalRotation();
    }

    /// <summary>
    /// Sets the element to a specific cardinal direction.
    /// </summary>
    /// <param name="degrees">Target cardinal direction (will be rounded to nearest 90-degree increment)</param>
    public void SetCardinalRotation(int degrees) {
      _playableElement.SnapTransform.SetCardinalRotation(degrees);
      // Sync rigidbody with the new transform rotation
      float newRotation = _playableElement.SnapTransform.eulerAngles.z;
      _rigidbody2D.MoveRotation(newRotation);
      _currentRotation = newRotation;
      _lastValidRotation = new Vector3(0, 0, newRotation);
    }

    // Integration methods for other components
    public void OnGravityChanged(bool hasGravity, float multiplier) {
      // Called by GravityElementComponent when gravity settings change
      InitMovementControls(); // Update rigidbody type if needed
    }

    #endregion


    #region Gizmos for Debugging
    void OnDrawGizmosSelected() {
      if (_playableElement == null) return;

      Vector3 pos = _playableElement.SnapTransform.position;

      // Show rotation limits
      //bool rotationAllowed = ChangeRotationOnCollision ? AllowRotation : _playableElement.CanRotate;
      if (AllowsRotationChanges && LimitRotationAngle) {
        float zRotation = _playableElement.SnapTransform.rotation.eulerAngles.z;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + Quaternion.Euler(0, 0, zRotation + MinRotationAngle) * Vector3.right * 2f);
        Gizmos.DrawLine(pos, pos + Quaternion.Euler(0, 0, zRotation + MaxRotationAngle) * Vector3.right * 2f);
      }

      // Show rotation step visualization

      //if (rotationAllowed || ChangeRotationOnCollision) {
      if (AllowsRotationChanges) { 
        Gizmos.color = Color.cyan;
        float currentRotation = _playableElement.SnapTransform.eulerAngles.z;
        // Show current rotation direction
        Gizmos.DrawLine(pos, pos + Quaternion.Euler(0, 0, currentRotation) * Vector3.right * 1.2f);
        // Show next rotation steps
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Gizmos.DrawLine(pos, pos + Quaternion.Euler(0, 0, currentRotation + RotationStep) * Vector3.right * 1f);
        Gizmos.DrawLine(pos, pos + Quaternion.Euler(0, 0, currentRotation - RotationStep) * Vector3.right * 1f);
      }

      // Show flip state indicators
      if (_playableElement.SnapTransform.IsFlippedX() || _playableElement.SnapTransform.IsFlippedY()) {
        Gizmos.color = Color.magenta;
        string flipState = $"{(_playableElement.SnapTransform.IsFlippedX() ? "X" : "")}{(_playableElement.SnapTransform.IsFlippedY() ? "Y" : "")}";
        // Draw a small indicator for flip state
        Gizmos.DrawWireCube(pos + Vector3.up * 1f, Vector3.one * 0.2f);
      }

      // Show position bounds
      if (AllowsPositionChanges) {
        Gizmos.color = Color.yellow;
        Vector3 boundsSize = new Vector3(MaxPosition.x - MinPosition.x, MaxPosition.y - MinPosition.y, 0);
        Vector3 boundsCenter = new Vector3(
          (MinPosition.x + MaxPosition.x) * 0.5f,
          (MinPosition.y + MaxPosition.y) * 0.5f,
          pos.z
        );
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
      }

      // Show movement constraints
      if (ConstrainHorizontalMovement) {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(pos.x, MinPosition.y, pos.z),
                       new Vector3(pos.x, MaxPosition.y, pos.z));
      }

      if (ConstrainVerticalMovement) {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(MinPosition.x, pos.y, pos.z),
                       new Vector3(MaxPosition.x, pos.y, pos.z));
      }

      // Show collision state
      if (_currentCollisionState.hasValidPositionCollision) {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, 0.5f);
      }

      if (_currentCollisionState.hasValidRotationCollision) {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.3f);
      }

      // Show immovable status
      if (PositionChangesDisabled && RotationChangesDisabled) {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(pos, _collider2D != null ? _collider2D.bounds.size : Vector3.one);
      }
    }

    #endregion
  }

  // Collision state tracking
  public struct CollisionState {
    public bool hasValidPositionCollision;
    public bool hasValidRotationCollision;
    public Vector3 accumulatedForce;
    public float accumulatedTorque;
    public int collisionCount;

    public void Reset() {
      hasValidPositionCollision = false;
      hasValidRotationCollision = false;
      accumulatedForce = Vector3.zero;
      accumulatedTorque = 0f;
      collisionCount = 0;
    }
  }
}