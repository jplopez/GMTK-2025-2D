using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using Ameba;
using MoreMountains.Tools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GMTK {
  /// <summary>
  /// This script controls the behaviours of the Playable Marble such as physics, spawning, collision and fall thresholds, and feedbacks<br/>
  /// It also includes a comprehensive Gizmo to debug the Marble's movements and collisions while moving.
  /// </summary>
  public class PlayableMarbleController : MonoBehaviour {

    [Header("Model")]
    public GameObject Model;
    public bool HideAtInitialization = true;

    [Header("Movement and Speed")]
    [Tooltip("The maximum linear speed the Marble can reach")]
    public float MaxLinearSpeed = 20f;
    [Tooltip("The minimum linear speed the Marble needs to start playing rolling sound")]
    public float MinLinearSpeedForRollingSound = 0.5f;
    [Tooltip("Curve to map linear speed (0 to MaxLinearSpeed) to rolling sound volume (0 to 1)")]
    public AnimationCurve LinearSpeedToRollingVolume = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [Tooltip("The maximum angular speed the Marble can reach")]
    public float MaxAngularSpeed = 720f; // degrees per second

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

    [Header("Intensity Calculator")]
    [Tooltip("The intensity calculator component that determines collision feedback strength")]
    public MarbleCollisionIntensityCalculator IntensityCalculator;

    [Header("Feedback Settings")]
    public float FeedbackCooldown = 0.1f;

    [Header("Collision Feedbacks")]
    [Tooltip("collision feedback")]
    public MMF_Player CollisionFeedback;
    [Tooltip("Boundary collision feedback (hitting level bounds)")]
    public MMF_Player BoundaryCollisionFeedback;

    [Header("Gizmos")]
    [Help("Use these Gizmos to debug the Marble's direction and collisions. The Movement Direction Gizmo is only visible in the Editor. The Collision Display can be seen in game, but are not carried over to builds. All Gizmos can be toggled on/off")]
    [SerializeField] private bool enableGizmos = true;

    [Header("Movement Direction Gizmo")]
    [SerializeField] private bool showMovementArrow = true;
    [SerializeField] private Color movementArrowColor = Color.cyan;
    [SerializeField] private float arrowLength = 2f;
    [SerializeField] private float arrowThickness = 0.15f;
    [SerializeField] private float minVelocityToShow = 0.1f;

    [Header("Collision Display")]
    [SerializeField] private bool showCollisionWindow = true;
    [SerializeField] private int maxCollisionsToShow = 5;
    [SerializeField] private Color collisionTextColor = Color.black;
    [SerializeField] private Color collisionDataColor = new(0.2f, 0.8f, 0.2f, 1f); // Light green
    [SerializeField] private Vector2 windowOffset = new(10, 10);
    [SerializeField] private float windowWidth = 400f;
    [SerializeField] private int fontSize = 12;

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

    // Collision display data
    protected List<CollisionDisplayData> _collisionHistory = new();
    protected GUIStyle _textStyle;
    protected GUIStyle _dataStyle;
    protected bool _stylesInitialized = false;

    // Feedback cooldown to prevent spam
    protected float _lastFeedbackTime = 0f;

    #region MonoBehaviour methods
    private void Awake() {

      if (ServiceLocator.TryGet<GameEventChannel>(out var eventChannel)) {
        _eventChannel = eventChannel;
      }
      else {
        this.LogWarning($"No GameEventChannel found in ServiceLocator. Attempting to load from Resources.");
        _eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }

      if (Model == null) { Model = this.gameObject; }

      // Auto-populate RotationIntensityCalculator if not assigned
      if (IntensityCalculator == null) {
        IntensityCalculator = GetComponent<MarbleCollisionIntensityCalculator>();
        if (IntensityCalculator == null) {
          this.LogWarning($"No MarbleCollisionIntensityCalculator found on {name}. Adding one with default settings.");
          IntensityCalculator = gameObject.AddComponent<MarbleCollisionIntensityCalculator>();
        }
      }

      //at the start we make the last position equal to its current position.
      _lastMarblePosition = Model.transform.position;
      _lastGroundedPosition = Model.transform.position;

      _rb = Model.GetComponent<Rigidbody2D>();
      _sr = Model.GetComponent<SpriteRenderer>();
      Spawn(hidden:HideAtInitialization);
    }

    protected void Update() {
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

      //adjust speed and movement based on settings
      ApplyMovementConstraints();

      // Track fall distance
      UpdateFallDistance();
      // adjust sound clips and volumes based on movement
      //UpdateMovementSounds();

      // Store last velocity for collision calculations
      _lastVelocity = _rb.linearVelocity;
    }

    private void LateUpdate() {
      //last position is updated last to prevent overridings
      _lastMarblePosition = Model.transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision) {
      this.LogDebug($"Collision entered with {collision.gameObject.name}");
      if (!_activeCollisions.Contains(collision)) {
        _activeCollisions.Add(collision);
      }

      // Calculate collision intensity and play appropriate feedback
      HandleCollisionFeedback(collision);
    }

    private void OnCollisionExit2D(Collision2D collision) {
      this.LogDebug($"Collision exited with {collision.gameObject.name}");
      _activeCollisions.Remove(collision);
    }

#if UNITY_EDITOR
    private void OnGUI() {
      if (!enableGizmos || !showCollisionWindow || !Application.isPlaying) return;

      InitializeGUIStyles();
      DrawCollisionWindow();
    }
#endif

    #endregion

    protected virtual bool IsGrounded() {
      RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, GroundedMask);
      return hit.collider != null;
    }

    protected virtual void ApplyMovementConstraints() {
      // Clamp linear velocity
      if (_rb.linearVelocity.magnitude > MaxLinearSpeed) {
        _rb.linearVelocity = _rb.linearVelocity.normalized * MaxLinearSpeed;
      }
      // Clamp angular velocity
      if (Mathf.Abs(_rb.angularVelocity) > MaxAngularSpeed) {
        _rb.angularVelocity = Mathf.Sign(_rb.angularVelocity) * MaxAngularSpeed;
      }
    }

    /// <summary>
    /// Updates the movement-related sounds based on current speed.
    /// This is a workaround, until I figure out how to pass a variable
    /// to an MMF_Player's AudioSource feedback.
    /// </summary>
    protected virtual void UpdateMovementSounds() {

      float speed = _rb.linearVelocity.magnitude;
      //this.Log($"linearVelocity: {_rb.linearVelocity}, speed: {speed}");
      bool longWhoosh = false;
      bool shortWhoosh = false;
      if (speed > MinLinearSpeedForRollingSound) {
        float speedToVolume = Mathf.Clamp01(speed / MaxLinearSpeed);
        longWhoosh = LinearSpeedToRollingVolume.Evaluate(speedToVolume) > 0.1f;
        shortWhoosh = !longWhoosh;
      }
      //this.Log($"Speed: {speed:F2}, LongWhoosh: {longWhoosh}, ShortWhoosh: {shortWhoosh}");
      // Adjust audio feedback chances based on speed
      var audioFeedbacks = CollisionFeedback.GetFeedbacksOfType<MMF_MMSoundManagerSound>();
      foreach (var audioFeedback in audioFeedbacks) {
        //this.Log($"Adjusting Audio Feedback '{audioFeedback.GetLabel()}'");
        if (audioFeedback.GetLabel() == "Whoosh-Long") {
          audioFeedback.Active = longWhoosh;
        }
        if (audioFeedback.GetLabel() == "Whoosh-Short") {
          audioFeedback.Active = shortWhoosh;
        }

      }
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

      // Ensure we have an intensity calculator
      if (IntensityCalculator == null) {
        this.LogWarning("No RotationIntensityCalculator assigned. Cannot calculate collision intensity.");
        return;
      }

      // Get collision context
      CollisionIntensityContext context = GetCollisionContext(collision);

      // Calculate collision intensity using the calculator
      float intensity = IntensityCalculator.CalculateIntensity(context);

      // Add to collision history for display BEFORE playing feedback
      AddCollisionToHistory(collision.gameObject.name, context, intensity);

      // Play appropriate feedback
      PlayCollisionFeedback(intensity, context);

      _lastFeedbackTime = Time.time;

      // Log collision for debugging
      this.LogDebug($"Collision with {collision.gameObject.name} - Intensity: {intensity:F2}, " +
                   $"Velocity: {context.Velocity:F2}, Fall: {context.FallDistance:F2}, " +
                   $"Angle: {context.CollisionAngle:F2}, Type: {context.CollisionType}");
    }

    /// <summary>
    /// Gets collision context information
    /// </summary>
    protected CollisionIntensityContext GetCollisionContext(Collision2D collision) {
      var context = new CollisionIntensityContext {
        // Velocity at collision
        Velocity = _lastVelocity.magnitude,

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

    private float CalculateCollisionIntensity(Collision2D collision) {
      if (IntensityCalculator.TryCalculateIntensity(GetCollisionContext(collision), out float intensity)) {
        return intensity;
      }
      else {
        return 0f;
      }
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
    protected void PlayCollisionFeedback(float intensity, CollisionIntensityContext context) {

      //collision feedback based on type
      MMF_Player collisionFeedback = context.CollisionType switch {
        CollisionType.LevelGridBound => BoundaryCollisionFeedback,
        CollisionType.PlayableElement => CollisionFeedback,
        _ => null
      };
      if (collisionFeedback != null) {
        collisionFeedback.FeedbacksIntensity = intensity;
        collisionFeedback.PlayFeedbacks();
      }
    }

    #endregion

    #region Collision Display

    /// <summary>
    /// Adds collision data to history for display using CollisionContext data
    /// </summary>
    private void AddCollisionToHistory(string objectName, CollisionIntensityContext context, float intensity) {
      var displayData = new CollisionDisplayData {
        ObjectName = objectName,
        Intensity = intensity,
        Speed = context.Velocity,
        FallDistance = context.FallDistance,
        CollisionAngle = context.CollisionAngle,
        CollisionType = context.CollisionType,
        Timestamp = Time.time,
      };

      // Add to front of list (newest first)
      _collisionHistory.Insert(0, displayData);

      // Limit history size
      if (_collisionHistory.Count > maxCollisionsToShow) {
        _collisionHistory.RemoveAt(_collisionHistory.Count - 1);
      }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Initializes GUI styles for collision display
    /// </summary>
    private void InitializeGUIStyles() {
      if (_stylesInitialized) return;

      _textStyle = new GUIStyle(GUI.skin.label) {
        font = Font.CreateDynamicFontFromOSFont("Consolas", fontSize),
        fontSize = fontSize,
        normal = { textColor = collisionTextColor },
        fontStyle = FontStyle.Bold
      };

      _dataStyle = new GUIStyle(GUI.skin.label) {
        font = Font.CreateDynamicFontFromOSFont("Consolas", fontSize),
        fontSize = fontSize,
        normal = { textColor = collisionDataColor },
        fontStyle = FontStyle.Normal
      };

      _stylesInitialized = true;
    }

    /// <summary>
    /// Draws the collision information window
    /// </summary>
    private void DrawCollisionWindow() {
      if (_collisionHistory.Count == 0) return;

      float windowHeight = (_collisionHistory.Count * (fontSize * 2)) + 40;
      Rect windowRect = new(
        windowOffset.x,
        Screen.height - windowHeight - windowOffset.y,
        windowWidth,
        windowHeight
      );

      // Draw background
      GUI.Box(windowRect, "Active Collisions", GUI.skin.window);

      // Draw collision data
      float yOffset = 25;
      for (int i = 0; i < _collisionHistory.Count; i++) {
        var collision = _collisionHistory[i];

        // Check if collision is still active (remove old ones)
        if (Time.time - collision.Timestamp > 5f) {
          _collisionHistory.RemoveAt(i);
          i--;
          continue;
        }

        Rect textRect = new(
          windowRect.x + 5,
          windowRect.y + yOffset,
          windowRect.width - 10,
          fontSize * 5
        );

        // Use the pre-constructed display string and split it for different colors
        string collisionText = "Collision with ";
        string collisionData = collision.DisplayString;

        // Draw text part in one color
        GUI.Label(textRect, collisionText, _textStyle);

        // Calculate width of "Collision with " text to offset the data
        Vector2 textSize = _textStyle.CalcSize(new GUIContent(collisionText));
        Rect dataRect = new(
          textRect.x + textSize.x,
          textRect.y,
          textRect.width - textSize.x,
          textRect.height
        );

        // Draw data part in different color
        GUI.Label(dataRect, collisionData, _dataStyle);

        yOffset += fontSize * 2;
      }
    }
#endif

    #endregion

    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmos() {
      if (!enableGizmos) return;

      DrawMovementArrow();
    }

    private void OnDrawGizmosSelected() {
      if (!enableGizmos) return;

      DrawMovementArrow();
      DrawCollisionGizmos();
    }

    /// <summary>
    /// Draws an arrow showing the marble's movement direction
    /// </summary>
    private void DrawMovementArrow() {
      if (!showMovementArrow || _rb == null) return;

      Vector2 velocity = Application.isPlaying ? _rb.linearVelocity : _lastVelocity;
      if (velocity.sqrMagnitude < minVelocityToShow * minVelocityToShow) return;

      Vector3 marblePos = Model != null ? Model.transform.position : transform.position;
      Vector3 direction = velocity.normalized;
      Vector3 arrowEnd = marblePos + (direction * arrowLength);

      // Set gizmo color
      Gizmos.color = movementArrowColor;

      // Draw main arrow line (thicker)
      for (float i = -arrowThickness; i <= arrowThickness; i += arrowThickness / 3f) {
        for (float j = -arrowThickness; j <= arrowThickness; j += arrowThickness / 3f) {
          Vector3 offset = new(i, j, 0);
          Gizmos.DrawLine(marblePos + offset, arrowEnd + offset);
        }
      }

      // Draw arrowhead
      Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * (arrowThickness * 3);
      Vector3 arrowheadBase = arrowEnd - (direction * (arrowThickness * 4));

      Gizmos.DrawLine(arrowEnd, arrowheadBase + perpendicular);
      Gizmos.DrawLine(arrowEnd, arrowheadBase - perpendicular);
      Gizmos.DrawLine(arrowheadBase + perpendicular, arrowheadBase - perpendicular);

      // Draw velocity magnitude label
      if (Application.isPlaying) {
        Handles.color = movementArrowColor;
        Handles.Label(arrowEnd + Vector3.up * 0.3f, $"v: {velocity.magnitude:F1}");
      }
    }

    /// <summary>
    /// Draws gizmos for active collisions
    /// </summary>
    private void DrawCollisionGizmos() {
      if (_activeCollisions == null) return;

      foreach (var collision in _activeCollisions) {
        if (collision?.gameObject == null) continue;

        Vector3 collisionPoint = collision.transform.position;

        // Different colors for different collision types
        CollisionType type = DetermineCollisionType(collision);
        Gizmos.color = type switch {
          CollisionType.PlayableElement => Color.yellow,
          CollisionType.LevelGridBound => Color.red,
          CollisionType.Checkpoint => Color.green,
          _ => Color.white
        };

        // Draw a wire sphere at collision point
        Gizmos.DrawWireSphere(collisionPoint, 0.3f);

        // Draw connection line from marble to collision
        Vector3 marblePos = Model != null ? Model.transform.position : transform.position;
        Gizmos.color = Color.Lerp(Gizmos.color, Color.white, 0.5f);
        Gizmos.DrawLine(marblePos, collisionPoint);

        // Show collision intensity
        if (Application.isPlaying) {
          float intensity = CalculateCollisionIntensity(collision);
          Handles.color = Gizmos.color;
          Handles.Label(collisionPoint + Vector3.up * 0.5f, $"I: {intensity:F2}");
        }
      }
    }
#endif

    #endregion

    #region Public API

    public void Spawn(bool hidden=false) {
      if (SpawnTransform != null) {
        Model.transform.position = SpawnTransform.position;
      }
      else { Model.transform.position = Vector3.zero; }

      StopMarble();

      // Reset collision tracking
      _lastGroundedPosition = Model.transform.position;
      _fallDistance = 0f;
      _wasGrounded = true;
      _activeCollisions.Clear();
      _collisionHistory.Clear();

      Model.SetActive(!hidden);
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
      if (_rb != null) _rb.AddForce(force, ForceMode2D.Impulse);
    }

    /// <summary>
    /// Gets the current collision intensity for external systems
    /// </summary>
    public float GetCurrentCollisionIntensity() {
      if (_activeCollisions.Count == 0 || IntensityCalculator == null) return 0f;

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


    [ContextMenu("Debug Collision Setup")]
    public void DebugCollisionSetup() {
      // Check marble setup
      var marbleRB = Model.GetComponent<Rigidbody2D>();
      if (Model.TryGetComponent<Collider2D>(out var marbleCollider)) {
        this.Log($"MARBLE - RB: {marbleRB != null}, " +
                  $"Collider: {marbleCollider.GetType().Name}, " +
                  $"Layer: {LayerMask.LayerToName(Model.layer)}");
      }
      else {
        this.LogWarning($"  ⚠️ Marble {Model.name} missing Collider2D!");
      }

      // Check intensity calculator
      if (IntensityCalculator != null) {
        this.Log($"Intensity Calculator: {IntensityCalculator.GetType().Name} on {IntensityCalculator.gameObject.name}");
        this.Log($"  Velocity Range: {IntensityCalculator.MinCollisionVelocity:F1} - {IntensityCalculator.MaxCollisionVelocity:F1}");
        this.Log($"  Fall Range: {IntensityCalculator.MinFallDistance:F1} - {IntensityCalculator.MaxFallDistance:F1}");
        this.Log($"  Intensity Range: {IntensityCalculator.CollisionIntensityRange.x:F2} - {IntensityCalculator.CollisionIntensityRange.y:F2}");
      }
      else {
        this.LogWarning("No RotationIntensityCalculator assigned!");
      }

      // Check all PlayableElements
      var elements = FindObjectsByType<PlayableElement>(FindObjectsSortMode.None);
      this.Log($"Found {elements.Length} PlayableElements:");

      foreach (var element in elements) {
        string logStr = $"  {element.name} - RB : ";

        if (element.TryGetComponent<Rigidbody2D>(out var elementRB)) {
          logStr += $"{elementRB != null}, ";
        }
        else {
          logStr += $"null, ";
          this.LogWarning($"  ⚠️ {element.name} missing Rigidbody2D - collisions won't work!");
        }
        if (element.TryGetComponent<Collider2D>(out var elementCollider)) {
          logStr += $"Collider: {elementCollider.GetType().Name}, " +
                 $"Layer: {LayerMask.LayerToName(element.gameObject.layer)}, " +
                 $"IsTrigger: {elementCollider.isTrigger}";
        }
        else {
          this.LogWarning($"  ⚠️ {element.name} missing Collider2D!");
        }
        this.Log(logStr);
      }

      // Check layer collision matrix
      int marbleLayer = Model.layer;
      int interactivesLayer = LayerMask.NameToLayer("Interactives");
      bool canCollide = !Physics2D.GetIgnoreLayerCollision(marbleLayer, interactivesLayer);

      this.Log($"Layer collision enabled between '{LayerMask.LayerToName(marbleLayer)}' and " +
               $"'{LayerMask.LayerToName(interactivesLayer)}': {canCollide}");
    }
  }

  #region Helper Structures

  /// <summary>
  /// Context information for a collision
  /// </summary>
  public struct CollisionContext {
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
  public struct MaterialProperties {
    public FrictionLevel Friction;
    public BouncinessLevel Bounciness;
    public PhysicsMaterial2D PhysicsMaterial;
    public bool HasCustomProperties;

    public MaterialProperties Clone(MaterialProperties other) {
      if (other.Equals(null)) return new();
      return new() {
        Friction = other.Friction,
        Bounciness = other.Bounciness,
        PhysicsMaterial = other.PhysicsMaterial,
        HasCustomProperties = other.HasCustomProperties
      };
    }
  }

  /// <summary>
  /// Types of collisions for specialized feedback
  /// </summary>
  public enum CollisionType {
    Generic,
    PlayableElement,
    LevelGridBound,
    Checkpoint
  }

  /// <summary>
  /// Data structure for collision display
  /// </summary>
  public struct CollisionDisplayData {

    private const int SM_Width = 12;
    private const int M_Width = 20;
    private const int LG_Width = 40;

    public string ObjectName;
    public float Intensity;
    public float Speed;
    public float FallDistance;
    public float CollisionAngle;
    public CollisionType CollisionType;
    public float Timestamp;
    // Pre-constructed display string
    public readonly string DisplayString => BuildDisplayString();

    readonly string BuildDisplayString() {
      return $"{ObjectName,-LG_Width}"[..LG_Width] +
             $"Inten: {Intensity,-SM_Width:F2}" +
             $"Velocity: {Speed,-SM_Width:F2}" +
             $"Fall : {FallDistance,-SM_Width:F2} " +
             $"Angle: {CollisionAngle,-SM_Width:F2}" +
             $"Type : {CollisionType,-M_Width}";
    }
  }

  #endregion
}