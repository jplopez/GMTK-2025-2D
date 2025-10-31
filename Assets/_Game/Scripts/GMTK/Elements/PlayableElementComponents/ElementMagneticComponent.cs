using System;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace GMTK {

  [Flags]
  public enum MagneticSourceType {
    MarbleOnly = 1,
    Elements = 2,
    Both = MarbleOnly | Elements
  }

  /// <summary>
  /// Represents a playable element component that applies magnetic forces to objects within a defined area of effect.
  /// </summary>
  /// <remarks>This component allows for the simulation of magnetic attraction or repulsion within a specified
  /// area.  The magnetic force can be configured to accelerate over time, and feedback mechanisms can be triggered 
  /// based on the magnetic interactions. The behavior of the magnetic force and feedbacks can be customized  using the
  /// provided settings.</remarks>
  [AddComponentMenu("GMTK/Playable Element Components/Element Magnetic Component")]
  public class ElementMagneticComponent : PlayableElementComponent {

    [Header("Magnetic Field Settings")]
    [Tooltip("The force of the magnetism, positive values attract, negative values repel")]
    [Range(-500f, 500f)]
    public float MagneticForce = 10f;
    [Tooltip("The area of effect for the magnetism")]
    [Range(1f, 10f)]
    public float MagneticRadius = 3f;
    [Tooltip("Defines what types of objects are affected by the magnetic force")]
    public MagneticSourceType MagneticSource = MagneticSourceType.MarbleOnly;
    [Tooltip("If this is true, the magnetic force will accelerate over time while an object is in the magnetic area")]
    public bool AccelerateOverTime = false;
    [Tooltip("The rate at which the magnetic force accelerates over time")]
    [MMFCondition("AccelerateOverTime", true)]
    public float AccelerationRate = 1f;
    [Tooltip("The curve used to evaluate the acceleration over time. Zero is at the edge of the MagneticArea, 1 is right before hitting the Playable Element")]
    [MMFCondition("AccelerateOverTime", true)]
    public AnimationCurve AccelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public enum RepulsionFeedbackSources {
      None,
      UseAttractionFeedback,
      UseAttractionPlayingBackwards,
      UseSeparateFeedback
    }

    [Header("Feedbacks")]
    [Tooltip("Feedback played when no object is in the magnetic area")]
    public MMF_Player IdleFeedback;
    [Tooltip("Feedback played when an object is attracted or repelled")]
    public MMF_Player AttractionFeedback;
    [Space]
    [Tooltip("Defines how repulsion feedback is handled")]
    public RepulsionFeedbackSources RepulsionFeedbackSource = RepulsionFeedbackSources.None;
    [Space]
    [MMFInformation("If you want only repulsion feedback: set RepulsionFeedbackSource to 'Use Separate Feedback', leave AttractionFeedback empty and RepulsionFeedback filled", MMFInformationAttribute.InformationType.Info, false)]
    [MMFEnumCondition("RepulsionFeedbackSource", (int)RepulsionFeedbackSources.UseSeparateFeedback)]
    [Tooltip("Feedback played when an object is repelled. If UseAttractionForRepulsion is true, this feedback will be ignored")]
    public MMF_Player RepulsionFeedback;
    [Space(10)]
    [Header("Debug")]
    [Tooltip("Enable debug logging for magnetic interactions")]
    public bool EnableDebugLogging = false;
    [Tooltip("Show magnetic field gizmos in the scene view")]
    public bool ShowMagneticFieldGizmos = true;
    [Tooltip("Color for attraction field gizmo")]
    public Color AttractionGizmoColor = Color.blue;
    [Tooltip("Color for repulsion field gizmo")]
    public Color RepulsionGizmoColor = Color.red;

    protected bool HasMagneticZone => _magneticField != null && _magneticField.isActiveAndEnabled && _magneticField.gameObject.activeInHierarchy;

    // Internal state
    private readonly HashSet<Rigidbody2D> _objectsInField = new();
    private readonly Dictionary<Rigidbody2D, float> _accelerationTimers = new();
    private readonly Dictionary<Rigidbody2D, Vector2> _lastAppliedForces = new();

    private CircleCollider2D _magneticField;
    private MagneticFieldState _currentState = MagneticFieldState.Idle;

    private enum MagneticFieldState {
      Idle,
      Attracting,
      Repelling
    }

    #region Component Lifecycle


    private void OnValidate() {
      if (TryGetComponent<CircleCollider2D>(out var circle)) {
        _magneticField = circle;
        _magneticField.radius = MagneticRadius;
        _magneticField.isTrigger = true;
      }
    }

    protected override void Initialize() {
      // Ensure we have a magnetic area
      if (_magneticField == null) {
        _magneticField = _playableElement.gameObject.AddComponent<CircleCollider2D>();
      }
      _magneticField.radius = MagneticRadius;
      _magneticField.isTrigger = true;

      // Validate curve
      if (AccelerationCurve == null || AccelerationCurve.keys.Length == 0) {
        AccelerationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        this.LogWarning($"AccelerationCurve was invalid on {_playableElement.name}. Reset to default.");
      }

      // Initialize feedback state
      _currentState = MagneticFieldState.Idle;
      PlayFeedback(IdleFeedback);

      this.LogDebug("ElementMagneticComponent initialized");
    }

    protected override bool Validate() {
      return _magneticField != null;
    }

    protected override void OnUpdate() {
      // Update magnetic forces for all objects in field
      ApplyMagneticForces();

      // Update feedback state
      UpdateFeedbackState();
    }

    protected override void FinalizeComponent() {
      // Stop all feedbacks
      StopAllFeedbacks();

      // Clear tracking data
      _objectsInField.Clear();
      _accelerationTimers.Clear();
      _lastAppliedForces.Clear();
    }

    protected override void ResetComponent() {
      // Clear all magnetic interactions
      _objectsInField.Clear();
      _accelerationTimers.Clear();
      _lastAppliedForces.Clear();

      // Reset to idle state
      _currentState = MagneticFieldState.Idle;
      PlayFeedback(IdleFeedback);
    }

    #endregion

    #region Magnetic Force Logic

    private void ApplyMagneticForces() {
      if (!IsActive || !HasMagneticZone) return;

      // Remove any objects that are null or inactive
      CleanupObjectsList();

      // Apply forces to all objects in field
      foreach (var rb in _objectsInField) {
        if (rb == null || !rb.gameObject.activeInHierarchy) continue;

        ApplyMagneticForceToObject(rb);
      }
    }

    private void ApplyMagneticForceToObject(Rigidbody2D targetRb) {
      if (targetRb == null) return;

      // Calculate force direction (from target to magnet center for attraction, opposite for repulsion)
      Vector2 magnetCenter = _playableElement.SnapTransform.position;
      Vector2 targetPosition = targetRb.position;
      Vector2 direction = (magnetCenter - targetPosition).normalized;

      // For repulsion, reverse the direction
      if (MagneticForce < 0) {
        direction = -direction;
      }

      // Calculate base force magnitude
      float baseForceMagnitude = Mathf.Abs(MagneticForce);

      // Apply acceleration over time if enabled
      if (AccelerateOverTime) {
        baseForceMagnitude = CalculateAcceleratedForce(targetRb, baseForceMagnitude);
      }

      // Apply force
      Vector2 force = direction * baseForceMagnitude;
      this.Log($"Force = {direction} - {baseForceMagnitude}");
      targetRb.AddForce(force, ForceMode2D.Force);

      // Store for debugging
      _lastAppliedForces[targetRb] = force;

      this.Log($"Applied magnetic force to {targetRb.name}: {force}");
    }

    private float CalculateAcceleratedForce(Rigidbody2D targetRb, float baseForce) {
      // Update acceleration timer
      if (!_accelerationTimers.ContainsKey(targetRb)) {
        _accelerationTimers[targetRb] = 0f;
      }

      _accelerationTimers[targetRb] += Time.fixedDeltaTime * AccelerationRate;

      // Use acceleration curve to modify force
      float accelerationFactor = AccelerationCurve.Evaluate(_accelerationTimers[targetRb]);
      return baseForce * accelerationFactor;
    }

    private void CleanupObjectsList() {
      _objectsInField.RemoveWhere(rb => rb == null || !rb.gameObject.activeInHierarchy);

      // Clean up dictionaries
      var keysToRemove = new List<Rigidbody2D>();
      foreach (var key in _accelerationTimers.Keys) {
        if (key == null || !key.gameObject.activeInHierarchy || !_objectsInField.Contains(key)) {
          keysToRemove.Add(key);
        }
      }

      foreach (var key in keysToRemove) {
        _accelerationTimers.Remove(key);
        _lastAppliedForces.Remove(key);
      }
    }

    #endregion

    #region Trigger Events

    private void OnTriggerEnter2D(Collider2D other) {
      if (!IsActive) return;

      // Check if this object should be affected by magnetism
      if (!ShouldAffectObject(other)) return;

      // Get rigidbody
      if (other.TryGetComponent<Rigidbody2D>(out var rb)) {
        // Add to field
        _objectsInField.Add(rb);
        _accelerationTimers[rb] = 0f;
        this.Log($"Object {other.name} entered magnetic field");
      }
    }

    private void OnTriggerExit2D(Collider2D other) {
      if (!IsActive) return;

      // Get rigidbody
      if (other.TryGetComponent<Rigidbody2D>(out var rb)) {
        // Remove from field
        _objectsInField.Remove(rb);
        _accelerationTimers.Remove(rb);
        _lastAppliedForces.Remove(rb);
        this.Log($"Object {other.name} exited magnetic field");
      }
    }

    private bool ShouldAffectObject(Collider2D other) {
      // Check for marble
      bool isMarble = other.GetComponent<PlayableMarbleController>() != null;
      if (isMarble && MagneticSource.HasFlag(MagneticSourceType.MarbleOnly)) {
        return true;
      }

      // Check for other playable elements (but not self)
      bool isPlayableElement = other.GetComponent<PlayableElement>() != null &&
                              other.gameObject != _playableElement.gameObject;
      if (isPlayableElement && MagneticSource.HasFlag(MagneticSourceType.Elements)) {
        return true;
      }

      return false;
    }

    #endregion

    #region Feedback Management

    private void UpdateFeedbackState() {
      MagneticFieldState newState = DetermineFeedbackState();

      // Only change feedback if state actually changed
      if (newState != _currentState) {
        _currentState = newState;
        PlayFeedbackForState(newState);
      }
    }

    private MagneticFieldState DetermineFeedbackState() {
      // if we have objects in field, we assume we're attracting or repelling based on force sign
      if (_objectsInField.Count == 0) {
        return MagneticFieldState.Idle;
      }
      return MagneticForce >= 0 ? MagneticFieldState.Attracting : MagneticFieldState.Repelling;
    }

    private void PlayFeedbackForState(MagneticFieldState state) {
      // Stop current feedback
      StopAllFeedbacks();

      MMF_Player feedback = null;
      // Play appropriate feedback
      switch (state) {
        case MagneticFieldState.Idle: feedback = IdleFeedback; break;
        case MagneticFieldState.Attracting: feedback = AttractionFeedback; break;
        case MagneticFieldState.Repelling: feedback = GetRepulsionFeedback(); break;
      }
      // check for play in reverse
      var playReverse = state == MagneticFieldState.Repelling 
        && RepulsionFeedbackSource == RepulsionFeedbackSources.UseAttractionPlayingBackwards;

      PlayFeedback(feedback, playReverse);
    }

    // returns the appropriate repulsion feedback based on RepulsionFeedbackSource setting
    private MMF_Player GetRepulsionFeedback() => RepulsionFeedbackSource switch {
      RepulsionFeedbackSources.None => null,
      RepulsionFeedbackSources.UseAttractionFeedback => AttractionFeedback,
      RepulsionFeedbackSources.UseAttractionPlayingBackwards => AttractionFeedback,
      RepulsionFeedbackSources.UseSeparateFeedback => RepulsionFeedback,
      _ => null
    };

    private void StopAllFeedbacks() {
      StopFeedback(IdleFeedback);
      StopFeedback(AttractionFeedback);
      StopFeedback(RepulsionFeedback);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Sets the magnetic force strength. Positive for attraction, negative for repulsion.
    /// </summary>
    /// <param name="force">Force magnitude. Positive = attract, negative = repel</param>
    public void SetMagneticForce(float force) {
      MagneticForce = force;
      this.Log($"Magnetic force set to {force}");
    }

    /// <summary>
    /// Gets the number of objects currently in the magnetic field.
    /// </summary>
    /// <returns>Count of objects being affected</returns>
    public int GetObjectsInFieldCount() => _objectsInField.Count;

    /// <summary>
    /// Gets all objects currently in the magnetic field.
    /// </summary>
    /// <returns>Read-only collection of rigidbodies in field</returns>
    public IReadOnlyCollection<Rigidbody2D> GetObjectsInField() => _objectsInField;

    /// <summary>
    /// Toggles between attraction and repulsion.
    /// </summary>
    public void ToggleMagneticPolarity() {
      MagneticForce = -MagneticForce;
      this.Log($"Magnetic polarity toggled. New force: {MagneticForce}");
    }

    /// <summary>
    /// Sets what types of objects are affected by the magnetic field.
    /// </summary>
    /// <param name="sourceType">Types of objects to affect</param>
    public void SetMagneticSourceType(MagneticSourceType sourceType) {
      MagneticSource = sourceType;

      // Re-evaluate all objects in field
      CleanupObjectsList();
      this.Log($"Magnetic source type set to {sourceType}");
    }

    /// <summary>
    /// Enables or disables acceleration over time.
    /// </summary>
    /// <param name="enable">Whether to enable acceleration</param>
    /// <param name="rate">Acceleration rate (if enabling)</param>
    public void SetAccelerationOverTime(bool enable, float rate = 1f) {
      AccelerateOverTime = enable;
      AccelerationRate = Mathf.Max(0f, rate);

      if (!enable) {
        _accelerationTimers.Clear();
      }

      this.Log($"Acceleration over time {(enable ? "enabled" : "disabled")} with rate {rate}");
    }

    /// <summary>
    /// Gets the last applied force for a specific object.
    /// </summary>
    /// <param name="rb">The rigidbody to check</param>
    /// <returns>Last applied force vector, or Vector2.zero if not found</returns>
    public Vector2 GetLastAppliedForce(Rigidbody2D rb) {
      return _lastAppliedForces.TryGetValue(rb, out Vector2 force) ? force : Vector2.zero;
    }

    #endregion

    #region Debug and Visualization

    private void OnDrawGizmos() {
      if (!ShowMagneticFieldGizmos) return;

      DrawMagneticFieldGizmos();
    }

    private void OnDrawGizmosSelected() {
      if (!ShowMagneticFieldGizmos) return;

      DrawMagneticFieldGizmos();
      DrawForceVectors();
    }

    private void DrawMagneticFieldGizmos() {
      if (!HasMagneticZone) return;

      // Choose color based on magnetic force
      Gizmos.color = MagneticForce >= 0 ? AttractionGizmoColor : RepulsionGizmoColor;

      // Draw field area
      Vector3 center = _magneticField.bounds.center;
      Vector3 size = _magneticField.bounds.size;

      if (_magneticField is CircleCollider2D circle) {
        Gizmos.DrawWireSphere(center, circle.radius);
      }
      else {
        Gizmos.DrawWireCube(center, size);
      }

      // Draw field direction indicator
      if (MagneticForce >= 0) {
        // Attraction - arrows pointing inward
        DrawArrowToward(center);
      }
      else {
        // Repulsion - arrows pointing outward
        DrawArrowAway(center);
      }
    }

    private void DrawForceVectors() {
      if (!Application.isPlaying) return;

      Gizmos.color = Color.yellow;

      foreach (var kvp in _lastAppliedForces) {
        if (kvp.Key == null) continue;

        Vector3 objectPos = kvp.Key.position;
        Vector3 force = kvp.Value;

        // Scale force vector for visibility
        Vector3 forceVector = force.normalized * Mathf.Min(force.magnitude * 0.1f, 2f);

        Gizmos.DrawLine(objectPos, objectPos + forceVector);
        Gizmos.DrawSphere(objectPos + forceVector, 0.1f);
      }
    }

    private void DrawArrowToward(Vector3 center) {
      // Draw simple arrows pointing toward center
      float offset = _magneticField.bounds.extents.x * 0.7f;

      Vector3[] directions = {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down
      };

      foreach (var dir in directions) {
        Vector3 start = center + dir * offset;
        Vector3 end = center + (0.5f * offset * dir);
        Gizmos.DrawLine(start, end);

        // Arrow head
        Vector3 arrowHead1 = end + (Quaternion.Euler(0, 0, 30) * (start - end).normalized) * 0.2f;
        Vector3 arrowHead2 = end + (Quaternion.Euler(0, 0, -30) * (start - end).normalized) * 0.2f;
        Gizmos.DrawLine(end, arrowHead1);
        Gizmos.DrawLine(end, arrowHead2);
      }
    }

    private void DrawArrowAway(Vector3 center) {
      // Draw simple arrows pointing away from center
      float offset = _magneticField.bounds.extents.x * 0.3f;

      Vector3[] directions = {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down
      };

      foreach (var dir in directions) {
        Vector3 start = center + dir * offset;
        Vector3 end = center + (0.8f * _magneticField.bounds.extents.x * dir);
        Gizmos.DrawLine(start, end);

        // Arrow head
        Vector3 arrowHead1 = end + (Quaternion.Euler(0, 0, 150) * (end - start).normalized) * 0.2f;
        Vector3 arrowHead2 = end + (Quaternion.Euler(0, 0, -150) * (end - start).normalized) * 0.2f;
        Gizmos.DrawLine(end, arrowHead1);
        Gizmos.DrawLine(end, arrowHead2);
      }
    }

    [ContextMenu("Test Magnetic Field")]
    private void TestMagneticField() {
      this.Log("=== Magnetic Field Test ===");
      this.Log($"Magnetic Force: {MagneticForce}");
      this.Log($"Magnetic Area: {(_magneticField != null ? _magneticField.GetType().Name : "None")}");
      this.Log($"Acceleration Over Time: {AccelerateOverTime} (Rate: {AccelerationRate})");
      this.Log($"Magnetic Source: {MagneticSource}");
      this.Log($"Objects in Field: {_objectsInField.Count}");

      foreach (var obj in _objectsInField) {
        if (obj != null) {
          Vector2 lastForce = GetLastAppliedForce(obj);
          this.Log($"  - {obj.name}: Last Force = {lastForce}");
        }
      }
    }

    #endregion
  }
}