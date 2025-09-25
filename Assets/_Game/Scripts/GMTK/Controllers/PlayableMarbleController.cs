using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using Ameba;

namespace GMTK {
  public class PlayableMarbleController : MonoBehaviour {

    [Header("Model")]
    public GameObject Model;

    [Header("Physics")]
    public float Mass = 15f;
    public float GravityScale = 3f;
    public float AngularDamping = 0.05f;
    public Vector2 InitialForce = Vector2.zero;
    [Tooltip("The minimum distance the Marble's position has to change between Update calls, to consider is moving")]
    [Min(0.001f)]
    public float MinimalMovementThreshold = 0.01f;

    [Header("Spawn")]
    [Tooltip("Where should the Marble spawned")]
    public Transform SpawnTransform;
    [Tooltip("The LayerMask the Marble should collide. Level and Interactables are the most common")]
    public LayerMask GroundedMask;

    [Header("Collision Detection & Feedback")]
    [Tooltip("Minimum collision velocity to trigger feedback")]
    [Min(0.1f)]
    public float MinCollisionVelocity = 1f;
    [Tooltip("Maximum collision velocity for intensity calculation")]
    [Min(1f)]
    public float MaxCollisionVelocity = 20f;
    [Tooltip("Minimum fall distance to trigger enhanced feedback")]
    [Min(0.1f)]
    public float MinFallDistance = 2f;
    [Tooltip("Maximum fall distance for intensity calculation")]
    [Min(1f)]
    public float MaxFallDistance = 10f;
    [Tooltip("Multiplier for collision angle effect (0 = no effect, 1 = full effect)")]
    [Range(0f, 1f)]
    public float AngleIntensityMultiplier = 0.3f;

    [Header("Feedback Settings")]
    public float FeedbackCooldown = 0.1f;

    [Header("Feedback Players")]
    [Tooltip("Light collision feedback (low intensity)")]
    public MMF_Player LightCollisionFeedback;
    [Tooltip("Medium collision feedback (medium intensity)")]
    public MMF_Player MediumCollisionFeedback;
    [Tooltip("Heavy collision feedback (high intensity)")]
    public MMF_Player HeavyCollisionFeedback;
    [Tooltip("Boundary collision feedback (hitting level bounds)")]
    public MMF_Player BoundaryCollisionFeedback;

    [Header("Material-Based Feedback")]
    [Tooltip("Feedback for low friction materials")]
    public MMF_Player LowFrictionFeedback;
    [Tooltip("Feedback for medium friction materials")]
    public MMF_Player MediumFrictionFeedback;
    [Tooltip("Feedback for high friction materials")]
    public MMF_Player HighFrictionFeedback;
    [Tooltip("Feedback for low bounciness materials")]
    public MMF_Player LowBouncinessFeedback;
    [Tooltip("Feedback for medium bounciness materials")]
    public MMF_Player MediumBouncinessFeedback;
    [Tooltip("Feedback for high bounciness materials")]
    public MMF_Player HighBouncinessFeedback;

    /// <summary>
    /// Whether the Marble is currently colliding with an object is considered ground (ex: wall, platform)
    /// </summary>
    public bool Grounded { get => IsGrounded(); }

    /// <summary>
    /// Whether the Marble has moved since the last Update call
    /// </summary>
    public bool IsMoving => _timeSinceLastMove > 0f;

    /// <summary>
    /// Current fall distance since last ground contact
    /// </summary>
    public float CurrentFallDistance => _fallDistance;

    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;
    protected GameEventChannel _eventChannel;
    protected Vector2 _lastMarblePosition = Vector2.zero;
    protected float _timeSinceLastMove = 0f;

    // Collision tracking
    protected Vector3 _lastGroundedPosition;
    protected float _fallDistance = 0f;
    protected bool _wasGrounded = true;
    protected Vector2 _lastVelocity = Vector2.zero;
    protected List<Collision2D> _activeCollisions = new();

    // Feedback cooldown to prevent spam
    protected float _lastFeedbackTime = 0f;

    //constants for intensity, bounciness and friction levels
    //TODO make them configurable at the game level from ScriptableObjects

    private const float INTENSITY_LIGHT = 0.3f;
    private const float INTENSITY_MID = 0.7f;

    private const float INTENSITY_TO_TYPE_FACTOR = 0.8f;
    private const float INTENSITY_TO_MATERIAL_FACTOR = 0.6f;


    #region MonoBehaviour methods
    private void Awake() {

      if (ServiceLocator.TryGet<GameEventChannel>(out var eventChannel)) {
        _eventChannel = eventChannel;
      } else {
        this.LogWarning($"No GameEventChannel found in ServiceLocator. Attempting to load from Resources.");
        _eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }

      if (Model == null) { Model = this.gameObject; }
      //at the start we make the last position equal to its current position.
      _lastMarblePosition = Model.transform.position;
      _lastGroundedPosition = Model.transform.position;

      _rb = Model.GetComponent<Rigidbody2D>();
      _sr = Model.GetComponent<SpriteRenderer>();
      Spawn();
    }

    void Update() {
      //if rigidBody isn't loaded, we do nothing and wait until next frame
      if (_rb == null) return;
      if (!_rb.mass.Equals(Mass)) _rb.mass = Mass;
      if (!_rb.gravityScale.Equals(GravityScale)) _rb.gravityScale = GravityScale;
      if (!_rb.angularDamping.Equals(AngularDamping)) _rb.angularDamping = AngularDamping;

      //calculate if Marble has moved since last update
      Vector2 currentMarblePosition = Model.transform.position;
      if (Vector2.Distance(currentMarblePosition, _lastMarblePosition) <= MinimalMovementThreshold) {
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0;
      }

      // Track fall distance
      UpdateFallDistance();

      // Store last velocity for collision calculations
      _lastVelocity = _rb.linearVelocity;
    }

    private void LateUpdate() {
      //last position is updated last to prevent overridings
      _lastMarblePosition = Model.transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
      this.Log($"Collision entered with {collision.gameObject.name}");
      if (!_activeCollisions.Contains(collision)) {
        _activeCollisions.Add(collision);
      }

      // Calculate collision intensity and play appropriate feedback
      HandleCollisionFeedback(collision);
    }

    private void OnCollisionExit2D(Collision2D collision) {
      this.Log($"Collision exited with {collision.gameObject.name}");
      _activeCollisions.Remove(collision);
    }

    #endregion

    protected virtual bool IsGrounded() {
      RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, GroundedMask);
      return hit.collider != null;
    }

    #region Collision Detection & Feedback

    /// <summary>
    /// Updates fall distance tracking for intensity calculations
    /// </summary>
    private void UpdateFallDistance() {
      bool currentlyGrounded = Grounded;
      
      if (currentlyGrounded && !_wasGrounded) {
        // Just landed - reset fall distance
        _fallDistance = 0f;
        _lastGroundedPosition = transform.position;
      }
      else if (!currentlyGrounded && _wasGrounded) {
        // Just started falling - record starting position
        _lastGroundedPosition = transform.position;
        _fallDistance = 0f;
      }
      else if (!currentlyGrounded) {
        // Currently falling - update fall distance
        _fallDistance = Vector3.Distance(_lastGroundedPosition, transform.position);
      }

      _wasGrounded = currentlyGrounded;
    }

    /// <summary>
    /// Handles collision feedback based on intensity factors
    /// </summary>
    protected virtual void HandleCollisionFeedback(Collision2D collision) {
      // Cooldown check to prevent feedback spam
      if (Time.time - _lastFeedbackTime < FeedbackCooldown) return;

      // Get collision context
      CollisionContext context = GetCollisionContext(collision);

      // Calculate collision intensity
      float intensity = CalculateCollisionIntensity(context);

      // Play appropriate feedback
      PlayCollisionFeedback(intensity, context);
      
      _lastFeedbackTime = Time.time;

      // Log collision for debugging
      this.Log($"Collision with {collision.gameObject.name} - Intensity: {intensity:F2}, " +
                   $"Speed: {context.Speed:F2}, Fall: {context.FallDistance:F2}, " +
                   $"Angle: {context.CollisionAngle:F2}, Type: {context.CollisionType}");
    }


    /// <summary>
    /// Gets collision context information
    /// </summary>
    protected CollisionContext GetCollisionContext(Collision2D collision) {
      var context = new CollisionContext {
        // Speed at collision
        Speed = _lastVelocity.magnitude,

        // Fall distance
        FallDistance = _fallDistance
      };

      // Collision angle (relative to surface normal)
      if (collision.contacts.Length > 0) {
        Vector2 collisionNormal = collision.contacts[0].normal;
        Vector2 velocityDirection = _lastVelocity.normalized;
        context.CollisionAngle = Vector2.Angle(velocityDirection, -collisionNormal);
      }
      
      // Collision type and material
      context.CollisionType = DetermineCollisionType(collision);
      context.Material = GetCollisionMaterial(collision);

      return context;
    }

    private float CalculateCollisionIntensity(Collision2D collision) => CalculateCollisionIntensity(GetCollisionContext(collision));

    /// <summary>
    /// Calculates collision intensity based on speed, fall distance, angle, and material
    /// </summary>
    private float CalculateCollisionIntensity(CollisionContext context) {
      
      // Base intensity from speed (0-1)
      float speedIntensity = Mathf.Clamp01((context.Speed - MinCollisionVelocity) / 
                                          (MaxCollisionVelocity - MinCollisionVelocity));
      
      // Fall distance multiplier (1-2)
      float fallMultiplier = 1f + Mathf.Clamp01(context.FallDistance / MaxFallDistance);
      
      // Angle multiplier (perpendicular collisions are more intense)
      float angleMultiplier = 1f + (1f - Mathf.Abs(context.CollisionAngle) / 90f) * AngleIntensityMultiplier;
      
      // Material multiplier based on bounciness
      float materialMultiplier = context.MaterialIntensityMultiplier;
      
      // Combined intensity
      float intensity = speedIntensity * fallMultiplier * angleMultiplier * materialMultiplier;
      
      return Mathf.Clamp01(intensity);
    }

    /// <summary>
    /// Determines the type of collision (PlayableElement, LevelGrid bounds, etc.)
    /// </summary>
    protected CollisionType DetermineCollisionType(Collision2D collision) {
      // Check for PlayableElement
      if (collision.gameObject.GetComponent<PlayableElement>() != null) {
        return CollisionType.PlayableElement;
      }
      
      // Check for LevelGrid bounds
      if (IsLevelGridBound(collision.gameObject)) {
        return CollisionType.LevelGridBound;
      }
      
      // Check for other specific types
      if (collision.gameObject.GetComponent<Checkpoint>() != null) {
        return CollisionType.Checkpoint;
      }
      
      return CollisionType.Generic;
    }

    /// <summary>
    /// Checks if the collided object is a LevelGrid boundary
    /// </summary>
    private bool IsLevelGridBound(GameObject obj) {
      // Check if the object has an EdgeCollider2D (typical for grid bounds)
      if (obj.GetComponent<EdgeCollider2D>() != null) {
        // Check if it's on the Level layer
        if (obj.layer == LayerMask.NameToLayer("Level")) {
          return true;
        }
        
        // Additional checks based on naming convention or tags
        string objName = obj.name.ToLower();
        if (objName.Contains("bound") || objName.Contains("wall") || objName.Contains("grid")) {
          return true;
        }
      }
      
      return false;
    }

    /// <summary>
    /// Gets material properties from the collided object
    /// </summary>
    protected MaterialProperties GetCollisionMaterial(Collision2D collision) {
      var properties = new MaterialProperties();
      
      // Check for PlayableElement with material properties
      if (collision.gameObject.TryGetComponent<PlayableElement>(out var playableElement)) {
        if (playableElement.TryGetComponent<PhysicalMaterialsElementComponent>(out var physicalMaterial)) {
          properties.Friction = physicalMaterial.Friction;
          properties.Bounciness = physicalMaterial.Bounciness;
          properties.HasCustomProperties = true;
        }
      }
      
      // Fallback to PhysicsMaterial2D if available
      if (!properties.HasCustomProperties) {
        var collider = collision.collider;
        if (collider.sharedMaterial != null) {
          properties.PhysicsMaterial = collider.sharedMaterial;
          properties.HasCustomProperties = true;
        }
      }
      
      return properties;
    }

    /// <summary>
    /// Plays appropriate feedback based on intensity and context
    /// </summary>
    protected void PlayCollisionFeedback(float intensity, CollisionContext context) {
      // Play intensity-based feedback
      MMF_Player intensityFeedback = intensity switch {
        < INTENSITY_LIGHT => LightCollisionFeedback,
        < INTENSITY_MID => MediumCollisionFeedback,
        _ => HeavyCollisionFeedback
      };
      
      // Play collision type specific feedback
      MMF_Player typeFeedback = context.CollisionType switch {
        CollisionType.LevelGridBound => BoundaryCollisionFeedback,
        _ => null
      };
      
      // Play material-based feedback
      MMF_Player materialFeedback = GetMaterialFeedback(context.Material);
      
      // Execute feedbacks (with intensity scaling)
      if (intensityFeedback != null) {
        intensityFeedback.FeedbacksIntensity = intensity;
        intensityFeedback.PlayFeedbacks();
      }
      
      if (typeFeedback != null) {
        typeFeedback.FeedbacksIntensity = intensity * INTENSITY_TO_TYPE_FACTOR; // Slightly reduce for type feedback
        typeFeedback.PlayFeedbacks();
      }
      
      if (materialFeedback != null) {
        materialFeedback.FeedbacksIntensity = intensity * INTENSITY_TO_MATERIAL_FACTOR; // Reduce for material feedback
        materialFeedback.PlayFeedbacks();
      }
    }

    /// <summary>
    /// Gets material-specific feedback player
    /// </summary>
    protected MMF_Player GetMaterialFeedback(MaterialProperties material) {
      if (!material.HasCustomProperties) return null;
      
      // Prioritize friction feedback over bounciness
      return material.Friction switch {
        FrictionLevel.Low => LowFrictionFeedback,
        FrictionLevel.Mid => MediumFrictionFeedback,
        FrictionLevel.High => HighFrictionFeedback,
        _ => material.Bounciness switch {
          BouncinessLevel.Low => LowBouncinessFeedback,
          BouncinessLevel.Mid => MediumBouncinessFeedback,
          BouncinessLevel.High => HighBouncinessFeedback,
          _ => null
        }
      };
    }

    #endregion

    #region Public API

    public void Spawn() {
      if (SpawnTransform != null) {
        Model.transform.position = SpawnTransform.position;
      }
      else { Model.transform.position = Vector3.zero; }
      
      // Reset collision tracking
      _lastGroundedPosition = Model.transform.position;
      _fallDistance = 0f;
      _wasGrounded = true;
      _activeCollisions.Clear();
      
      Model.SetActive(true);
      StopMarble();
    }

    public void StopMarble() {
      GravityScale = 0f;
      ResetForces();
    }

    public void Launch() {
      ResetForces();
      GravityScale = 1f;
      Model.SetActive(true);
      _rb.AddForce(InitialForce, ForceMode2D.Impulse);
    }

    public void ResetForces() {
      if (_rb == null) return;
      _rb.linearVelocity = Vector2.zero;
      _rb.angularVelocity = 0f;
      _rb.rotation = 0f;
    }

    public void ApplyForce(Vector2 force) {
      if(_rb != null) _rb.AddForce(force, ForceMode2D.Impulse);
    }

    /// <summary>
    /// Gets the current collision intensity for external systems
    /// </summary>
    public float GetCurrentCollisionIntensity() {
      if (_activeCollisions.Count == 0) return 0f;
      
      float maxIntensity = 0f;
      foreach (var collision in _activeCollisions) {
        float intensity = CalculateCollisionIntensity(collision);
        if (intensity > maxIntensity) {
          maxIntensity = intensity;
        }
      }
      
      return maxIntensity;
    }

    #endregion

    #region Helper Structures

    /// <summary>
    /// Context information for a collision
    /// </summary>
    protected struct CollisionContext {
      public float Speed;
      public float FallDistance;
      public float CollisionAngle;
      public CollisionType CollisionType;
      public MaterialProperties Material;

      public readonly float MaterialIntensityMultiplier {
        get {
          if (!Material.HasCustomProperties) return 1f;
          return Material.Friction.NormalizedValue() * Material.Bounciness.NormalizedValue();
        }
      }
    }

    /// <summary>
    /// Material properties for collision feedback
    /// </summary>
    protected struct MaterialProperties {
      public FrictionLevel Friction;
      public BouncinessLevel Bounciness;
      public PhysicsMaterial2D PhysicsMaterial;
      public bool HasCustomProperties;
    }

    /// <summary>
    /// Types of collisions for specialized feedback
    /// </summary>
    protected enum CollisionType {
      Generic,
      PlayableElement,
      LevelGridBound,
      Checkpoint
    }

    #endregion

    [ContextMenu("Debug Collision Setup")]
    public void DebugCollisionSetup() {
      // Check marble setup
      var marbleRB = Model.GetComponent<Rigidbody2D>();
      var marbleCollider = Model.GetComponent<Collider2D>();
      
      this.Log($"MARBLE - RB: {marbleRB != null}, Collider: {marbleCollider?.GetType().Name}, " +
               $"Layer: {LayerMask.LayerToName(Model.layer)}");
      
      // Check all PlayableElements
      var elements = FindObjectsByType<PlayableElement>(FindObjectsSortMode.None);
      this.Log($"Found {elements.Length} PlayableElements:");
      
      foreach (var element in elements) {
        var elementRB = element.GetComponent<Rigidbody2D>();
        var elementCollider = element.GetComponent<Collider2D>();
        
        this.Log($"  {element.name} - RB: {elementRB != null}, " +
                 $"Collider: {elementCollider?.GetType().Name}, " +
                 $"Layer: {LayerMask.LayerToName(element.gameObject.layer)}, " +
                 $"IsTrigger: {elementCollider?.isTrigger}");
        
        if (elementRB == null) {
          this.LogWarning($"  ⚠️ {element.name} missing Rigidbody2D - collisions won't work!");
        }
      }
      
      // Check layer collision matrix
      int marbleLayer = Model.layer;
      int interactivesLayer = LayerMask.NameToLayer("Interactives");
      bool canCollide = !Physics2D.GetIgnoreLayerCollision(marbleLayer, interactivesLayer);
      
      this.Log($"Layer collision enabled between '{LayerMask.LayerToName(marbleLayer)}' and " +
               $"'{LayerMask.LayerToName(interactivesLayer)}': {canCollide}");
    }
  }
}