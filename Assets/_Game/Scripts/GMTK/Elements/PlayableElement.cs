using Ameba;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Represents any object in the game that can be snapped into a LevelGrid and dragged by the player.
  /// This class replaces GridSnappable and implements the IDraggable interface for improved dragging support.
  /// Additional abilities are provided by PlayableElementComponent-derived classes.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element")]
  public class PlayableElement : MonoBehaviour, IDraggable {

    public enum SnappableBodyType { Static, Interactive }

    [Header("Snappable Settings")]
    [Tooltip("If set, this transform will be used for snapping instead of the GameObject's transform.")]
    public Transform SnapTransform; // if null, uses this.transform
    [Tooltip("If set, this transform will be used to look for SpriteRenderers, RigidBody and Collisions. If empty, it will use the GameObject's transform")]
    public Transform Model;
    [Tooltip("(Optional) highlight model to show when hovering or dragging.")]
    public GameObject HighlightModel;

    [Header("Dragging")]
    [Tooltip("If true, the object can be dragged. Set it to false for elements that you want static in the playable area")]
    public bool Draggable = true;

    [Header("Local Grid Footprint")]
    [SerializeField] protected List<Vector2Int> _occupiedCells = new();

    [Header("Actions")]
    [Tooltip("If true, the object can be flipped on the X and Y axis")]
    public bool Flippable = false;
    [Tooltip("If true, the object can be rotated in its Z axis")]
    public bool CanRotate = false;
 
    [Header("Feedbacks")]
    [Tooltip("The feedback when the pointer selects the element.")]
    public GameObject SelectedFeedback;
    [Tooltip("The feedback when the pointer is over the element.")]
    public GameObject PointerOnFeedback;
    [Tooltip("The feedback when the pointer moves out of the element.")]
    public GameObject PointerOutFeedback;

    // IDraggable interface properties
    public bool IsDraggable => Draggable;
    public bool IsBeingDragged { get; private set; }
    public bool IsActive { get; set; }
    public Transform DragTransform => SnapTransform != null ? SnapTransform : transform;
    public Collider2D InteractionCollider => _collider;

    // Public properties for compatibility with existing code
    public bool IsRegistered => _isRegistered;

    // Private fields
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    protected bool _isRegistered = false;
    protected SnappableBodyType _bodyType = SnappableBodyType.Static;
    protected SpriteRenderer _modelRenderer;
    protected PolygonCollider2D _collider;
    protected List<PlayableElementComponent> _components = new();

    // Events for components to listen to
    protected event Action<PlayableElementEventArgs> OnPlayableElementEvent;

    #region MonoBehaviour Methods

    private void Awake() => Initialize();

    private void OnValidate() {
      gameObject.layer = LayerMask.NameToLayer("Interactives");
      Initialize();
    }

    private void Start() => InitializeAllPlayableElementComponents();

    private void Update() {
      _components.ForEach(component => component.RunBeforeUpdate());
      _components.ForEach(c => c.RunOnUpdate());
    }

    private void LateUpdate() => _components.ForEach(c => c.RunAfterUpdate());

    private void OnDestroy() => _components.ForEach(c => c.RunFinalize());

    #endregion

    #region Initialize 

    public virtual void Initialize() {
      // Default assignments
      if (SnapTransform == null) SnapTransform = this.transform;
      if (Model == null) Model = this.transform;

      // Store initial position for the ResetToStartingState function
      _initialPosition = SnapTransform.position;
      _initialRotation = SnapTransform.rotation;
      _initialScale = SnapTransform.localScale;

      if (CheckForRenderers())
        InitPlayableElement();
    }

    private void InitializeAllPlayableElementComponents() {
      _components.Clear();
      _components.AddRange(GetComponents<PlayableElementComponent>());
      _components.ForEach(comp => comp.TryInitialize());
    }

    private bool CheckForRenderers() {
      // Check Model has a sprite renderer
      if (Model.TryGetComponent(out SpriteRenderer renderer)) {
        _modelRenderer = renderer;
        return true;
      }
      else {
        SpriteRenderer[] renderers = Model.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0) {
          this.LogWarning($"[PlayableElement] No SpriteRenderer found on {Model.name}.");
        }
      }
      return false;
    }

    private void InitPlayableElement() {
      // Hide existing highlight
      if (HighlightModel != null) HighlightModel.SetActive(false);

      if (Model.gameObject.TryGetComponent(out PolygonCollider2D collider)) {
        _collider = collider;
      }
      else {
        this.LogWarning($"[PlayableElement] No PolygonCollider2D found on {Model.gameObject.name}. Adding one.");
        _collider = Model.gameObject.AddComponent<PolygonCollider2D>();
      }
    }

    #endregion

    #region IDraggable Implementation

    public virtual void OnDragStart() {
      IsBeingDragged = true;
      
      // Notify components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.DragStart);
      OnPlayableElementEvent?.Invoke(eventArgs);

      this.Log($"Drag started on {name}");
    }

    public virtual void OnDragUpdate(Vector3 worldPosition) {
      if (IsBeingDragged) {
        UpdatePosition(worldPosition);
        
        // Notify components
        var eventArgs = new PlayableElementEventArgs(this, worldPosition, PlayableElementEventType.DragUpdate);
        OnPlayableElementEvent?.Invoke(eventArgs);
      }
    }

    public virtual void OnDragEnd() {
      IsBeingDragged = false;

      // Notify components
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.DragEnd);
      OnPlayableElementEvent?.Invoke(eventArgs);

      this.Log($"Drag ended on {name}");
    }

    public virtual void OnPointerEnter() => OnPointerOver();

    public virtual void OnPointerExit() => OnPointerOut();

    public virtual void OnBecomeActive() {
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.BecomeActive);
      OnPlayableElementEvent?.Invoke(eventArgs);
      this.Log($"became active");
    }

    public virtual void OnBecomeInactive() {
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.BecomeInactive);
      OnPlayableElementEvent?.Invoke(eventArgs);
      this.Log($"became inactive");
    }

    #endregion

    #region Occupancy (Grid System Compatibility)

    public List<Vector2Int> GetFootprint() => _occupiedCells;

    public IEnumerable<Vector2Int> GetWorldOccupiedCells(Vector2Int gridOrigin, bool flippedX = false, bool flippedY = false, int rotation = 0) {
      // Patch while we fill occupied cells for all snappables.
      if (_occupiedCells.Count == 0) {
        yield return TransformLocalCell(Vector2Int.zero, flippedX, flippedY, rotation) + gridOrigin;
      }
      else {
        foreach (var local in _occupiedCells) {
          var transformed = TransformLocalCell(local, flippedX, flippedY, rotation);
          yield return transformed + gridOrigin;
        }
      }
    }

    private Vector2Int TransformLocalCell(Vector2Int cell, bool flipX, bool flipY, int rotation) {
      int x = flipX ? -cell.x : cell.x;
      int y = flipY ? -cell.y : cell.y;

      // Rotation in 90° increments
      return (rotation % 360) switch {
        90 => new Vector2Int(-y, x),
        180 => new Vector2Int(-x, -y),
        270 => new Vector2Int(y, -x),
        _ => new Vector2Int(x, y),
      };
    }

    #endregion

    #region Event Handlers (Compatibility with existing system)

    public void OnPointerOver() {
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.PointerOver);
      OnPlayableElementEvent?.Invoke(eventArgs);
      SetGlow(true);
    }

    public void OnPointerOut() {
      var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.PointerOut);
      OnPlayableElementEvent?.Invoke(eventArgs);
      SetGlow(false);
    }

    public virtual void SetGlow(bool active) {
      if (HighlightModel != null) HighlightModel.SetActive(active);
    }

    public void AddComponentListener(PlayableElementComponent component) => OnPlayableElementEvent += component.OnPlayableElementEvent;

    public void RemoveComponentListener(PlayableElementComponent component) => OnPlayableElementEvent -= component.OnPlayableElementEvent;

    #endregion

    #region Transformation methods

    /// <summary>
    /// Updates the position on the Transform specified in SnapTransform
    /// </summary>
    public void UpdatePosition(Vector3 newPos) => SnapTransform.position = newPos;

    /// <summary>
    /// Returns the rotation from the Transform specified in SnapTransform
    /// </summary>
    public Quaternion GetRotation() => SnapTransform.rotation;

    /// <summary>
    /// Returns the position from the Transform specified in SnapTransform
    /// </summary>
    public Vector3 GetPosition() => SnapTransform.position;

    #endregion

    #region Actions: rotate, flip

    /// <summary>
    /// Rotates the PlayableElement clockwise, if CanRotate is true
    /// </summary>
    public void RotateClockwise() {
      if (CanRotate) {
        // Notify components first - they might handle the rotation
        var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.RotateCW);
        OnPlayableElementEvent?.Invoke(eventArgs);
        
        // If no component handled it, do the default rotation
        if (!eventArgs.Handled) {
          SnapTransform.Rotate(Vector3.forward, -90f);
        }
      }
    }

    /// <summary>
    /// Rotates the PlayableElement counter clockwise, if CanRotate is true
    /// </summary>
    public void RotateCounterClockwise() {
      if (CanRotate) {
        // Notify components first - they might handle the rotation
        var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.RotateCCW);
        OnPlayableElementEvent?.Invoke(eventArgs);
        
        // If no component handled it, do the default rotation
        if (!eventArgs.Handled) {
          SnapTransform.Rotate(Vector3.forward, 90f);
        }
      }
    }

    /// <summary>
    /// Flips the PlayableElement on the X-axis (up-down), if Flippable is true
    /// </summary>
    public void FlipX() {
      if (Flippable) {
        // Notify components first - they might handle the flip
        var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.FlippedX);
        OnPlayableElementEvent?.Invoke(eventArgs);
        
        // If no component handled it, do the default flip
        if (!eventArgs.Handled) {
          Vector3 scale = SnapTransform.localScale;
          scale.x *= -1;
          SnapTransform.localScale = scale;
        }
      }
    }

    /// <summary>
    /// Flips the PlayableElement on the Y-axis (left-right), if Flippable is true
    /// </summary>
    public void FlipY() {
      if (Flippable) {
        // Notify components first - they might handle the flip
        var eventArgs = new PlayableElementEventArgs(this, transform.position, PlayableElementEventType.FlippedY);
        OnPlayableElementEvent?.Invoke(eventArgs);
        
        // If no component handled it, do the default flip
        if (!eventArgs.Handled) {
          Vector3 scale = SnapTransform.localScale;
          scale.y *= -1;
          SnapTransform.localScale = scale;
        }
      }
    }

    /// <summary>
    /// Resets the PlayableElement transform specified in SnapTransform to the initial position and rotation at the time of initialization
    /// </summary>
    public virtual void ResetSnaggable() {
      SnapTransform.SetLocalPositionAndRotation(_initialPosition, _initialRotation);
      SnapTransform.localScale = _initialScale;
      _components.ForEach(c => c.RunResetComponent());
    }

    #endregion

    public override string ToString() => name;
  }

  #region Event System for PlayableElement

  public enum PlayableElementEventType {
    DragStart, DragUpdate, DragEnd,
    PointerOver, PointerOut,
    BecomeActive, BecomeInactive,
    RotateCW, RotateCCW,
    FlippedX, FlippedY
  }

  public class PlayableElementEventArgs : EventArgs {
    public PlayableElement Element { get; }
    public Vector3 WorldPosition { get; }
    public PlayableElementEventType EventType { get; }
    public bool Handled { get; set; } = false;

    public PlayableElementEventArgs(PlayableElement element, Vector3 worldPosition, PlayableElementEventType eventType) {
      Element = element;
      WorldPosition = worldPosition;
      EventType = eventType;
    }
  }

  #endregion

  #region Casting to GridSnappable (for compatibility)

  public static class PlayableElementExtensions {
    public static GridSnappable ToGridSnappable(this PlayableElement element, float ttl=0.2f) {
      if(element.TryGetComponent(out GridSnappable existing)) {
        return existing;
      } else {

        //instance a GridSnappable and copy properties
        GridSnappable snappable = element.gameObject.AddComponent<GridSnappable>();
        snappable.transform.parent = null;

        snappable.gameObject.name = element.gameObject.name; // keep the name
        snappable.BehaviourDelegate = GridSnappable.BehaviourDelegateType.Components;
        snappable.SnapTransform = element.SnapTransform;
        snappable.Model = element.Model;
        snappable.HighlightModel = element.HighlightModel;
        snappable.Draggable = element.Draggable;
        snappable.Flippable = element.Flippable;
        snappable.CanRotate = element.CanRotate;
        snappable.PointerOnFeedback = element.PointerOnFeedback;
        snappable.PointerOutFeedback = element.PointerOutFeedback;
        snappable.transform.parent = null;
        GameObject.Destroy(snappable, ttl); // destroy after ttl seconds to avoid cluttering the scene
        return snappable;
      }
    }
  }
  #endregion
}