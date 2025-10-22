using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace GMTK {

  [Flags]
  public enum ForceType { Linear = 1, Radial = 2 }

  [Flags]
  public enum ForceReceiver { Marble = 1, Elements = 2 }

  /// <summary>
  /// This component acts as a force applier, applying forces to the Marble, other PlayableElements, and optionally other GameObjects.<br/>
  /// Forces can be applied either instantaneously via collision events or continuously while the Marble or other entities are in the trigger area.<br/>
  /// Use this component to create boosters or wind zones, among other effects.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element Components/Force Applier")]
  public class ForceApplierElementComponent : PlayableElementComponent {

    [Header("Force Applier Settings")]
    public ForceType AppliedForces = ForceType.Linear;
    [Tooltip("The force vector to apply. For linear forces, this is a direction and magnitude. For radial forces, the X component is the radial force magnitude, and the Y component is the tangential force magnitude.")]
    public Vector2 Force = Vector2.zero;
    [Tooltip("The torque to apply. Only used for radial forces.")]
    public Quaternion Torque = Quaternion.identity;
    [Tooltip("If this is set, the force will be applied relative to this transform's orientation. If empty, it will assume the GameObject")]
    public Transform ForceTransform;

    [Header("Affected Entities")]
    [Tooltip("What entities can be affected by the force")]
    public ForceReceiver AffectedEntities = ForceReceiver.Marble;
    [Space]
    [Tooltip("Other GameObjects you would like to get forces applied, who aren't Marble or PlayableElements")]
    public List<GameObject> OtherEntities = new();
    [Space]

    [Help("CONTINUOUS: Set ForceTriggerArea for ongoing force while in area. " +
      "INSTANTANEOUS: Enable TriggerOnCollision and choose Enter/Exit/Stay phases. " +
      "Both modes can work simultaneously.")]

    [Header("Force Trigger Settings")]
    [Tooltip("If this is set, the force will be applied continuously while the target is inside this trigger area")]
    public Collider2D ForceTriggerArea;
    [Tooltip("If the force is applied continuously, this is the interval in seconds at which the force will be applied")]
    public float ForceTriggerCooldown = 0.5f;
    [Tooltip("Whether the force should be applied instantaneously when the element is collided by a valid entity")]
    public bool TriggerOnCollision = true;
    
    [Space]
    [Tooltip("If TriggerOnCollision is true, specify in which collision events to trigger the instant force")]
    [MMFCondition("TriggerOnCollision", true)]
    public bool TriggerOnCollisionEnter = true;
    [MMFCondition("TriggerOnCollision", true)]
    public bool TriggerOnCollisionExit = false;
    [MMFCondition("TriggerOnCollision", true)]
    public bool TriggerOnCollisionStay = false;
    [Space(10)]

    [Header("Feedbacks")]
    [Tooltip("The feedback player when force is applied through the triggering area (continuous)")]
    public MMF_Player AreaForceFeedback;
    [Tooltip("The feedback played when force is applied on collisions (instantaneous)")]
    public MMF_Player CollisionForceFeedback;

    [Header("Debug")]
    [Tooltip("Enable debug logging for force application")]
    public bool EnableDebugLogging = false;
    [Tooltip("Show force gizmos in the scene view")]
    public bool ShowForceGizmos = true;
    [Tooltip("Color for linear force gizmo")]
    public Color LinearForceGizmoColor = Color.green;
    [Tooltip("Color for radial force gizmo")]
    public Color RadialForceGizmoColor = Color.yellow;

    // Properties
    protected bool IsApplyingForce => Force != Vector2.zero;
    protected bool IsApplyingTorque => AppliedForces.HasFlag(ForceType.Radial) && Torque != Quaternion.identity;
    protected bool HasTriggerArea => ForceTriggerArea != null && ForceTriggerArea.isActiveAndEnabled;
    protected bool HasOtherAffectedEntities => OtherEntities != null && OtherEntities.Count > 0;

    // State tracking
    private readonly HashSet<GameObject> _objectsInTriggerArea = new();
    private readonly Dictionary<GameObject, float> _lastForceApplicationTime = new();
    private readonly Dictionary<GameObject, Vector2> _lastAppliedForces = new();
    private ForceApplicationState _currentState = ForceApplicationState.Idle;
    private Coroutine _continuousForceCoroutine;

    private enum ForceApplicationState {
      Idle,
      ApplyingContinuous,
      ApplyingInstantaneous
    }

    #region Component Lifecycle

    protected override void Initialize() {
      // Default transform
      if (ForceTransform == null) ForceTransform = _playableElement.SnapTransform;

      // Ensure other entities list is initialized
      OtherEntities ??= new();

      // Validate setup - both trigger area and collision triggering are optional
      ValidateSetup();

      // Ensure trigger area is properly configured if provided
      if (HasTriggerArea && !ForceTriggerArea.isTrigger) {
        ForceTriggerArea.isTrigger = true;
        this.LogWarning($"ForceTriggerArea on {_playableElement.name} was not set as trigger. Fixed automatically.");
      }
      this.Log("ForceApplierElementComponent initialized");
    }

    protected override bool Validate() {
      // Component needs either a trigger area or collision triggering enabled
      bool hasValidSetup = HasTriggerArea || TriggerOnCollision;

      if (!hasValidSetup) {
        Debug.LogWarning($"[ForceApplierElementComponent] {_playableElement.name} has no triggering area and collision triggering is disabled. This component will not work.");
        return false;
      }

      return IsApplyingForce || IsApplyingTorque;
    }

    protected override void OnUpdate() {
      // Clean up objects list
      CleanupObjectsList();

      // Update state based on objects in trigger area
      UpdateForceApplicationState();
    }

    protected override void FinalizeComponent() {
      // Stop continuous force application
      if (_continuousForceCoroutine != null) {
        StopCoroutine(_continuousForceCoroutine);
        _continuousForceCoroutine = null;
      }

      // Clear tracking data
      _objectsInTriggerArea.Clear();
      _lastForceApplicationTime.Clear();
      _lastAppliedForces.Clear();

    }

    protected override void ResetComponent() {
      // Stop any ongoing force application
      if (_continuousForceCoroutine != null) {
        StopCoroutine(_continuousForceCoroutine);
        _continuousForceCoroutine = null;
      }

      // Clear all tracking data
      _objectsInTriggerArea.Clear();
      _lastForceApplicationTime.Clear();
      _lastAppliedForces.Clear();

      // Reset state
      _currentState = ForceApplicationState.Idle;
    }

    #endregion

    #region Trigger Area Events (Continuous Force Application)

    private void OnTriggerEnter2D(Collider2D other) {
      if (!IsActive || !HasTriggerArea) return;

      // Check if this object should be affected by forces
      if (!ShouldAffectObject(other.gameObject)) return;

      // Add to trigger area
      _objectsInTriggerArea.Add(other.gameObject);
      this.Log($"Object {other.name} entered force trigger area");

      // Start continuous force application if not already running
      if (_continuousForceCoroutine == null && HasObjectsInTriggerArea()) {
        _continuousForceCoroutine = StartCoroutine(ContinuousForceApplicationCoroutine());
      }
    }

    private void OnTriggerExit2D(Collider2D other) {
      if (!IsActive || !HasTriggerArea) return;

      // Remove from trigger area
      _objectsInTriggerArea.Remove(other.gameObject);
      _lastForceApplicationTime.Remove(other.gameObject);
      _lastAppliedForces.Remove(other.gameObject);

      this.Log($"Object {other.name} exited force trigger area");

      // Stop continuous force application if no objects remain
      if (!HasObjectsInTriggerArea() && _continuousForceCoroutine != null) {
        StopCoroutine(_continuousForceCoroutine);
        _continuousForceCoroutine = null;
        _currentState = ForceApplicationState.Idle;
      }
    }

    #endregion

    #region Collision Events (Instantaneous Force Application)

    private void OnCollisionEnter2D(Collision2D collision) {
      if (IsActive 
        && TriggerOnCollision 
        && TriggerOnCollisionEnter) CommonOnCollision2D(collision);
    }

    private void OnCollisionExit2D(Collision2D collision) {
      if (IsActive
        && TriggerOnCollision
        && TriggerOnCollisionExit) CommonOnCollision2D(collision);
    }

    private void OnCollisionStay2D(Collision2D collision) {
      if (IsActive && TriggerOnCollision && TriggerOnCollisionStay) {
        // Add cooldown check for Stay events to prevent frame-rate dependent behavior
        if (CanApplyForceTo(collision.gameObject)) {
            CommonOnCollision2D(collision);
            _lastForceApplicationTime[collision.gameObject] = Time.time;
        }
    }
    }

    private void CommonOnCollision2D(Collision2D collision) { 
      // Check if this object should be affected by forces
      if (!ShouldAffectObject(collision.gameObject)) return;

      this.Log($"Trigger Flags: Enter? {TriggerOnCollisionEnter}, Exit? {TriggerOnCollisionExit}, Stay? {TriggerOnCollisionStay}"); 
      this.Log($"Collision detected with {collision.gameObject.name} - applying instantaneous force");

      // Apply instantaneous force to the colliding object
      ApplyInstantaneousForceToObject(collision.gameObject);
    }

    #endregion

    #region Force Application Logic

    private IEnumerator ContinuousForceApplicationCoroutine() {
      _currentState = ForceApplicationState.ApplyingContinuous;

      while (HasObjectsInTriggerArea()) {
        foreach (var obj in _objectsInTriggerArea.ToArray()) {
          if (obj == null || !obj.activeInHierarchy) continue;

          // Check cooldown
          if (CanApplyForceTo(obj)) {
            ApplyContinuousForceToObject(obj);
            _lastForceApplicationTime[obj] = Time.time;
          }
        }
        yield return new WaitForSeconds(ForceTriggerCooldown);
      }

      _currentState = ForceApplicationState.Idle;
      _continuousForceCoroutine = null;
    }

    private void ApplyContinuousForceToObject(GameObject target) {
      if (target == null) return;

      Vector2 forceToApply = CalculateForceForObject(target);

      // Apply force based on object type
      bool forceApplied = false;

      // Try PlayableMarbleController first
      if (target.TryGetComponent<PlayableMarbleController>(out var marble)) {
        marble.ApplyForce(forceToApply);
        forceApplied = true;
        this.Log($"Applied continuous force {forceToApply} to marble {target.name}");
      }
      // Then try other PlayableElements with Rigidbody2D
      else if (target.TryGetComponent<PlayableElement>(out var _) &&
               target.TryGetComponent<Rigidbody2D>(out var rb)) {
        ApplyForceToRigidbody(rb, forceToApply);
        forceApplied = true;
        this.Log($"Applied continuous force {forceToApply} to playable element {target.name}");
      }
      // Finally try other GameObjects with Rigidbody2D
      else if (target.TryGetComponent<Rigidbody2D>(out var otherRb)) {
        ApplyForceToRigidbody(otherRb, forceToApply);
        forceApplied = true;
        this.Log($"Applied continuous force {forceToApply} to rigidbody {target.name}");
      }

      if (forceApplied) {
        // Store for debugging
        _lastAppliedForces[target] = forceToApply;

        // Apply torque if needed
        if (IsApplyingTorque && target.TryGetComponent<Rigidbody2D>(out var rbForTorque)) {
          ApplyTorqueToRigidbody(rbForTorque);
        }

        // Play continuous feedback
        PlayFeedback(AreaForceFeedback);
      }
    }

    private void ApplyInstantaneousForceToObject(GameObject target) {
      if (target == null) return;

      Vector2 forceToApply = CalculateForceForObject(target);

      // Apply force based on object type
      bool forceApplied = false;

      // Try PlayableMarbleController first
      if (target.TryGetComponent<PlayableMarbleController>(out var marble)) {
        marble.ApplyForce(forceToApply);
        forceApplied = true;
        this.Log($"Applied instantaneous force {forceToApply} to marble {target.name}");
      }
      // Then try other PlayableElements with Rigidbody2D
      else if (target.TryGetComponent<PlayableElement>(out var _) &&
               target.TryGetComponent<Rigidbody2D>(out var rb)) {
        ApplyForceToRigidbody(rb, forceToApply);
        forceApplied = true;
        this.Log($"Applied instantaneous force {forceToApply} to playable element {target.name}");
      }
      // Finally try other GameObjects with Rigidbody2D
      else if (target.TryGetComponent<Rigidbody2D>(out var otherRb)) {
        ApplyForceToRigidbody(otherRb, forceToApply);
        forceApplied = true;
        this.Log($"Applied instantaneous force {forceToApply} to rigidbody {target.name}");
      }

      if (forceApplied) {
        // Store for debugging
        _lastAppliedForces[target] = forceToApply;

        // Apply torque if needed
        if (IsApplyingTorque && target.TryGetComponent<Rigidbody2D>(out var rbForTorque)) {
          ApplyTorqueToRigidbody(rbForTorque);
        }

        // Play instantaneous feedback
        PlayFeedback(CollisionForceFeedback);
      }
    }

    private Vector2 CalculateForceForObject(GameObject target) {
      Vector2 calculatedForce = Force;

      if (AppliedForces.HasFlag(ForceType.Linear)) {
        // For linear forces, apply the force in the direction specified by ForceTransform
        calculatedForce = ForceTransform.TransformDirection(Force);
      }

      if (AppliedForces.HasFlag(ForceType.Radial)) {
        // For radial forces, calculate direction from/to target
        Vector2 centerPosition = ForceTransform.position;
        Vector2 targetPosition = target.transform.position;
        Vector2 direction = (targetPosition - centerPosition).normalized;

        // X component is radial (toward/away from center)
        // Y component is tangential (perpendicular to radial)
        Vector2 radialComponent = direction * Force.x;
        Vector2 tangentialComponent = new Vector2(-direction.y, direction.x) * Force.y;

        calculatedForce = radialComponent + tangentialComponent;
      }

      return calculatedForce;
    }

    private void ApplyForceToRigidbody(Rigidbody2D rb, Vector2 force) {
      if (rb == null) return;
      rb.AddForce(force, ForceMode2D.Force);
    }

    private void ApplyTorqueToRigidbody(Rigidbody2D rb) {
      if (rb == null || Torque == Quaternion.identity) return;

      // Convert quaternion to angular velocity
      float torqueZ = Torque.eulerAngles.z;
      rb.AddTorque(torqueZ, ForceMode2D.Force);
    }

    private bool CanApplyForceTo(GameObject target) {
      if (!_lastForceApplicationTime.TryGetValue(target, out float lastTime)) {
        return true;
      }

      return Time.time - lastTime >= ForceTriggerCooldown;
    }

    private bool HasObjectsInTriggerArea() {
      return HasTriggerArea && _objectsInTriggerArea.Count > 0;
    }

    #endregion

    #region Object Filtering

    private bool ShouldAffectObject(GameObject obj) {
      if (obj == null) return false;

      // Don't affect self
      if (obj == _playableElement.gameObject) return false;

      // Check for marble
      if (obj.TryGetComponent<PlayableMarbleController>(out _) &&
          AffectedEntities.HasFlag(ForceReceiver.Marble)) {
        return true;
      }

      // Check for other playable elements (but not self)
      if (obj.TryGetComponent<PlayableElement>(out var element) &&
          element.gameObject != _playableElement.gameObject &&
          AffectedEntities.HasFlag(ForceReceiver.Elements)) {
        return true;
      }

      // Check if it's in the other entities list
      if (HasOtherAffectedEntities && OtherEntities.Contains(obj)) {
        return true;
      }

      return false;
    }

    private void CleanupObjectsList() {
      _objectsInTriggerArea.RemoveWhere(obj => obj == null || !obj.activeInHierarchy);

      // Clean up dictionaries
      var keysToRemove = new List<GameObject>();
      foreach (var key in _lastForceApplicationTime.Keys) {
        if (key == null || !key.activeInHierarchy || !_objectsInTriggerArea.Contains(key)) {
          keysToRemove.Add(key);
        }
      }

      foreach (var key in keysToRemove) {
        _lastForceApplicationTime.Remove(key);
        _lastAppliedForces.Remove(key);
      }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// This method is called when the PlayableElementEventType.CollisionStart event is received.
    /// That event is triggered by the PlayableElement when it collides with another object.
    /// This component uses this method to apply the force if TriggerOnCollision is true
    /// </summary>
    /// <param name="evt"></param>
    //protected void OnCollisionStart(PlayableElementEventArgs evt) => OnCollision(evt);

    //protected void OnCollisionEnd(PlayableElementEventArgs evt) => OnCollision(evt);

    //protected void OnCollisioning(PlayableElementEventArgs evt) => OnCollision(evt);

    ///// <summary>
    ///// This component listens for collision events from the PlayableElement.
    ///// If the EventType matches the configured CollisionEventType and TriggerOnCollision is true, then the instantanous force is applied
    ///// </summary>
    ///// <param name="evt"></param>
    //private void OnCollision(PlayableElementEventArgs evt) {
    //  if (TriggerOnCollision && CollisionEventType == evt.EventType) {
    //    ApplyInstantaneousForceToObject(evt.OtherObject);
    //  }
    //}

    #endregion

    #region State Management

    private void UpdateForceApplicationState() {
      var newState = DetermineCurrentState();

      if (newState != _currentState) {
        _currentState = newState;
        this.Log($"Force application state changed to: {newState}");
      }
    }

    private ForceApplicationState DetermineCurrentState() {
      if (_continuousForceCoroutine != null && HasObjectsInTriggerArea()) {
        return ForceApplicationState.ApplyingContinuous;
      }
      return ForceApplicationState.Idle;
    }

    #endregion

    #region Validation and Setup

    private void ValidateSetup() {
      if (!HasTriggerArea && !TriggerOnCollision) {
        this.LogWarning($"{_playableElement.name} has no Force Trigger Area and collision triggering is disabled. This component will not work");
      }
      else if (!HasTriggerArea) {
        this.Log($"{_playableElement.name} doesn't have a Force Trigger Area. It will only apply forces on collisions");
      }
      else if (!TriggerOnCollision) {
        this.Log($"{_playableElement.name} has collision triggering disabled. It will only apply forces through the trigger area");
      }
      else {
        this.Log($"{_playableElement.name} is configured for both continuous (trigger area) and instantaneous (collision) force application");
      }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Triggers an instantaneous force application to all valid objects in range.
    /// </summary>
    public void TriggerInstantaneousForce() {
      if (!IsActive) return;

      _currentState = ForceApplicationState.ApplyingInstantaneous;

      // Get all objects that should be affected
      var affectedObjects = GetAllAffectedObjects();

      foreach (var obj in affectedObjects) {
        ApplyInstantaneousForceToObject(obj);
      }

      _currentState = ForceApplicationState.Idle;
      this.Log($"Applied instantaneous force to {affectedObjects.Count} objects via public API");
    }

    private List<GameObject> GetAllAffectedObjects() {
      var objects = new List<GameObject>();

      // Add objects currently in trigger area
      if (HasTriggerArea) {
        objects.AddRange(_objectsInTriggerArea);
      }

      // Add other specified entities if they're within range or always affected
      if (HasOtherAffectedEntities) {
        foreach (var entity in OtherEntities) {
          if (entity != null && entity.activeInHierarchy && !objects.Contains(entity)) {
            objects.Add(entity);
          }
        }
      }
      
      return objects;
    }

    /// <summary>
    /// Sets the force vector to apply.
    /// </summary>
    /// <param name="newForce">The new force vector</param>
    public void SetForce(Vector2 newForce) {
      Force = newForce;
      this.Log($"Force set to {newForce}");
    }

    /// <summary>
    /// Sets the force type (Linear/Radial).
    /// </summary>
    /// <param name="forceType">The type of force to apply</param>
    public void SetForceType(ForceType forceType) {
      AppliedForces = forceType;
      this.Log($"Force type set to {forceType}");
    }

    /// <summary>
    /// Sets what entities are affected by the force.
    /// </summary>
    /// <param name="entities">The types of entities to affect</param>
    public void SetAffectedEntities(ForceReceiver entities) {
      AffectedEntities = entities;
      this.Log($"Affected entities set to {entities}");
    }

    /// <summary>
    /// Enables or disables collision-based force triggering.
    /// </summary>
    /// <param name="enabled">Whether collision triggering should be enabled</param>
    public void SetTriggerOnCollision(bool enabled) => TriggerOnCollision = enabled;
      

    /// <summary>
    /// Adds a GameObject to the other entities list.
    /// </summary>
    /// <param name="obj">GameObject to add</param>
    public void AddOtherEntity(GameObject obj) {
      if (obj != null && !OtherEntities.Contains(obj)) {
        OtherEntities.Add(obj);
        this.Log($"Added other entity: {obj.name}");
      }
    }

    /// <summary>
    /// Removes a GameObject from the other entities list.
    /// </summary>
    /// <param name="obj">GameObject to remove</param>
    public void RemoveOtherEntity(GameObject obj) {
      if (OtherEntities.Remove(obj)) {
        this.Log($"Removed other entity: {obj.name}");
      }
    }

    /// <summary>
    /// Gets the number of objects currently in the trigger area.
    /// </summary>
    /// <returns>Count of objects in trigger area</returns>
    public int GetObjectsInTriggerAreaCount() => _objectsInTriggerArea.Count;

    /// <summary>
    /// Gets the last applied force for a specific object.
    /// </summary>
    /// <param name="obj">The object to check</param>
    /// <returns>Last applied force vector, or Vector2.zero if not found</returns>
    public Vector2 GetLastAppliedForce(GameObject obj) {
      return _lastAppliedForces.TryGetValue(obj, out Vector2 force) ? force : Vector2.zero;
    }

    /// <summary>
    /// Gets the current force application state.
    /// </summary>
    /// <returns>Current state of force application</returns>
    public string GetCurrentState() => _currentState.ToString();

    #endregion

    #region Debug and Visualization

    private void OnDrawGizmos() {
      if (!ShowForceGizmos) return;

      DrawForceGizmos();
    }

    private void OnDrawGizmosSelected() {
      if (!ShowForceGizmos) return;

      DrawForceGizmos();
      DrawTriggerAreaGizmo();
      DrawForceVectors();
    }

    private void DrawForceGizmos() {
      if (ForceTransform == null) return;

      // Choose color based on force type
      Gizmos.color = AppliedForces.HasFlag(ForceType.Linear) ? LinearForceGizmoColor : RadialForceGizmoColor;

      Vector3 center = ForceTransform.position;

      if (AppliedForces.HasFlag(ForceType.Linear)) {
        // Draw linear force arrow
        Vector3 forceDirection = ForceTransform.TransformDirection(Force.normalized);
        Vector3 arrowEnd = center + (forceDirection * Mathf.Min(Force.magnitude * 0.1f, 2f));

        Gizmos.DrawLine(center, arrowEnd);

        // Arrow head
        Vector3 arrowHead1 = arrowEnd + (Quaternion.Euler(0, 0, 30) * (-forceDirection) * 0.3f);
        Vector3 arrowHead2 = arrowEnd + (Quaternion.Euler(0, 0, -30) * (-forceDirection) * 0.3f);
        Gizmos.DrawLine(arrowEnd, arrowHead1);
        Gizmos.DrawLine(arrowEnd, arrowHead2);
      }

      if (AppliedForces.HasFlag(ForceType.Radial)) {
        // Draw radial force indicators
        DrawRadialForceGizmo(center);
      }
    }

    private void DrawRadialForceGizmo(Vector3 center) {
      // Draw circular indicators for radial force
      float radius = Mathf.Max(Force.magnitude * 0.1f, 1f);

      // Draw circle
      const int segments = 16;
      for (int i = 0; i < segments; i++) {
        float angle1 = (i * 360f / segments) * Mathf.Deg2Rad;
        float angle2 = ((i + 1) * 360f / segments) * Mathf.Deg2Rad;

        Vector3 point1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * radius;
        Vector3 point2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * radius;

        Gizmos.DrawLine(point1, point2);
      }

      // Draw direction indicators
      if (Force.x != 0) { // Radial component
        Gizmos.color = Force.x > 0 ? Color.red : Color.blue; // Red for outward, blue for inward
        Gizmos.DrawWireSphere(center, radius * 1.1f);
      }

      if (Force.y != 0) { // Tangential component
        Gizmos.color = Force.y > 0 ? Color.green : Color.yellow;
        // Draw tangential arrows
        for (int i = 0; i < 4; i++) {
          float angle = (i * 90f) * Mathf.Deg2Rad;
          Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
          Vector3 tangent = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0) * (Force.y > 0 ? 1 : -1);
          Gizmos.DrawLine(pos, pos + (tangent * 0.5f));
        }
      }
    }

    private void DrawTriggerAreaGizmo() {
      if (!HasTriggerArea) return;

      Gizmos.color = Color.cyan;
      Vector3 center = ForceTriggerArea.bounds.center;
      Vector3 size = ForceTriggerArea.bounds.size;

      if (ForceTriggerArea is CircleCollider2D circle) {
        Gizmos.DrawWireSphere(center, circle.radius);
      }
      else {
        Gizmos.DrawWireCube(center, size);
      }
    }

    private void DrawForceVectors() {
      if (!Application.isPlaying) return;

      Gizmos.color = Color.magenta;

      foreach (var kvp in _lastAppliedForces) {
        if (kvp.Key == null) continue;

        Vector3 objectPos = kvp.Key.transform.position;
        Vector3 force = kvp.Value;

        // Scale force vector for visibility
        Vector3 forceVector = force.normalized * Mathf.Min(force.magnitude * 0.1f, 2f);

        Gizmos.DrawLine(objectPos, objectPos + forceVector);
        Gizmos.DrawSphere(objectPos + forceVector, 0.1f);
      }
    }

    [ContextMenu("Test Force Application")]
    private void TestForceApplication() {
      this.Log("=== Force Application Test ===");
      this.Log($"Force: {Force}");
      this.Log($"Force Type: {AppliedForces}");
      this.Log($"Affected Entities: {AffectedEntities}");
      this.Log($"Has Trigger Area: {HasTriggerArea}");
      this.Log($"Trigger On Collision: {TriggerOnCollision}");
      this.Log($"Objects in Trigger Area: {_objectsInTriggerArea.Count}");
      this.Log($"Current State: {_currentState}");

      if (HasOtherAffectedEntities) {
        this.Log($"Other Entities Count: {OtherEntities.Count}");
        foreach (var entity in OtherEntities) {
          this.Log($"  - {(entity != null ? entity.name : "NULL")}");
        }
      }
    }

    [ContextMenu("Trigger Instantaneous Force")]
    private void ContextMenuTriggerForce() {
      TriggerInstantaneousForce();
    }

    #endregion
  }
}