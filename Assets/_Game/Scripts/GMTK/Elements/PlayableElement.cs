using Ameba;
using System;
using System.Collections.Generic;
using UnityEngine;
using GMTK.Extensions;

namespace GMTK {

  /// <summary>
  /// Represents any object in the game that can be snapped into a LevelGrid and dragged by the player.
  /// This class replaces GridSnappable and implements the IDraggable, ISelectable, and IHoverable interfaces for improved interaction support.
  /// Additional abilities are provided by PlayableElementComponent-derived classes.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element")]
  [RequireComponent(typeof(PointerElementComponent))]
  public partial class PlayableElement : MonoBehaviour, IDraggable, ISelectable, IHoverable {

    public enum SnappableBodyType { Static, Interactive }

    [Header("Model Settings")]
    [Tooltip("If set, this transform will be used for snapping instead of the GameObject's transform.")]
    public Transform SnapTransform; // if null, uses this.transform
    [Tooltip("If set, this transform will be used to look for SpriteRenderers, RigidBody and Collisions. If empty, it will use the GameObject's transform")]
    public Transform Model;
    [Tooltip("(Optional) highlight model to show when hovering or dragging.")]
    public GameObject HighlightModel;

    [Tooltip("If true, the object can be dragged. Set it to false for elements that you want static in the playable area")]
    public bool Draggable = true;

    [Header("Local Grid Footprint")]
    public List<Vector2Int> OccupiedCells = new();

    //[Header("Actions")]
    [Tooltip("If true, the object can be flipped on the X and Y axis")]
    public bool Flippable = false;
    [Tooltip("If true, the object can be rotated in its Z axis")]
    public bool CanRotate = false;


    // Public properties for compatibility with existing code
    public bool IsRegistered => _isRegistered;

    // Private fields
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    protected bool _isRegistered = false;
    //protected SnappableBodyType _bodyType = SnappableBodyType.Static;
    protected SpriteRenderer _modelRenderer;
    protected PolygonCollider2D _collider;
    protected List<PlayableElementComponent> _components = new();

    protected GameEventChannel _gameEventChannel;

    private PointerElementComponent _pointerComponent;

    // Events for components to listen to
    [Obsolete("Use RaisePlayableElementEvent instead")]
    public event Action<PlayableElementEventArgs> OnPlayableElementEvent;

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

      // Get GameEventChannel from ServiceLocator
      _gameEventChannel = ServiceLocator.Get<GameEventChannel>();

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

      // Cache the PointerElementComponent reference
      _pointerComponent = GetComponent<PointerElementComponent>();
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

    #region Occupancy (Grid System Compatibility)

    public List<Vector2Int> GetFootprint() => OccupiedCells;

    public IEnumerable<Vector2Int> GetWorldOccupiedCells(Vector2Int gridOrigin, bool flippedX = false, bool flippedY = false, int rotation = 0) {
      // Patch while we fill occupied cells for all snappables.
      if (OccupiedCells.Count == 0) {
        yield return TransformLocalCell(Vector2Int.zero, flippedX, flippedY, rotation) + gridOrigin;
      }
      else {
        foreach (var local in OccupiedCells) {
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
        90 => new Vector2Int(-y - 1, x),
        180 => new Vector2Int(-x - 1, -y - 1),
        270 => new Vector2Int(y, -x - 1),
        _ => new Vector2Int(x, y),
      };
    }

    #endregion

    #region Event System

    [Obsolete("Use RaisePlayableElementEvent instead")]
    public void AddComponentListener(PlayableElementComponent component) => OnPlayableElementEvent += component.OnPlayableElementEvent;

    [Obsolete("Use RaisePlayableElementEvent instead")]
    public void RemoveComponentListener(PlayableElementComponent component) => OnPlayableElementEvent -= component.OnPlayableElementEvent;

    [Obsolete("Use RaisePlayableElementEvent instead")]
    public void InvokePlayableElementEvent(PlayableElementEventArgs eventArgs) => OnPlayableElementEvent?.Invoke(eventArgs);

    /// <summary>
    /// Helper method to raise PlayableElement events and reduce code duplication.
    /// </summary>
    /// <param name="eventType">The type of event to raise</param>
    /// <param name="worldPosition">The world position for the event (uses transform.position if not provided)</param>
    protected virtual PlayableElementEventArgs RaisePlayableElementEvent(PlayableElementEventType eventType, Vector3? worldPosition = null) {
      var eventArgs = BuildEventArgs(eventType);
      _gameEventChannel.Raise(GameEventType.PlayableElementEvent, eventArgs);
      OnPlayableElementEvent?.Invoke(eventArgs);

      this.LogDebug($"PlayableElementEvent {eventType} started on {name}");
      return eventArgs;
    }

    protected PlayableElementEventArgs BuildEventArgs(PlayableElementEventType eventType, Vector3? worldPosition = null) {
      var position = worldPosition ?? transform.position;
      return new PlayableElementEventArgs(this, position, eventType);
    }

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
    public void RotateClockwise() => InnerRotate(PlayableElementEventType.RotateCW);

    /// <summary>
    /// Rotates the PlayableElement counter clockwise, if CanRotate is true
    /// </summary>
    public void RotateCounterClockwise() => InnerRotate(PlayableElementEventType.RotateCCW);

    /// <summary>
    /// Internal method to handle rotation logic for both clockwise and counter-clockwise rotation
    /// </summary>
    /// <param name="eventType">The type of rotation event (RotateCW or RotateCCW)</param>
    private void InnerRotate(PlayableElementEventType eventType) {
      if (!CanRotate) return;

      // Notify components first - they might handle the rotation
      var eventArgs = new PlayableElementEventArgs(this, transform.position, eventType);
      OnPlayableElementEvent?.Invoke(eventArgs);

      // If no component handled it, do the default rotation using extension methods
      if (!eventArgs.Handled) {
        if (eventType == PlayableElementEventType.RotateCW) {
          SnapTransform.RotateClockwise();
        } else {
          SnapTransform.RotateCounterClockwise();
        }

        string direction = eventType == PlayableElementEventType.RotateCW ? "clockwise" : "counter-clockwise";
        this.LogDebug($"Rotated {name} {direction} (flippedX: {SnapTransform.IsFlippedX()}, flippedY: {SnapTransform.IsFlippedY()})");
      }
    }

    /// <summary>
    /// Flips the PlayableElement on the X-axis (up-down), if Flippable is true
    /// </summary>
    public void FlipX() => InnerFlip(flipX: true);

    /// <summary>
    /// Flips the PlayableElement on the Y-axis (left-right), if Flippable is true
    /// </summary>
    public void FlipY() => InnerFlip(flipY: true);

    /// <summary>
    /// Convenience method to flip the PlayableElement on the specified axes, if Flippable is true
    /// </summary>
    /// <param name="flipX"></param>
    /// <param name="flipY"></param>
    private void InnerFlip(bool flipX = false, bool flipY = false) {
      if (!Flippable || (!flipX && !flipY)) return;
      this.Log($"Flipping {(flipX ? "X" : "")} {(flipY ? "Y" : "")} on {name}");

      PlayableElementEventType eventType = flipX ? PlayableElementEventType.FlippedX : PlayableElementEventType.FlippedY;

      // Notify components first - they might handle the flip
      PlayableElementEventArgs eventArgs = RaisePlayableElementEvent(eventType);

      // If no component handled it, do the default flip using extension methods
      if (eventArgs != null && eventArgs.Handled) return;

      if (flipX) {
        SnapTransform.FlipX();
      }

      if (flipY) {
        SnapTransform.FlipY();
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

  #region Casting to GridSnappable (for compatibility)

  public static class PlayableElementExtensions {
    public static GridSnappable ToGridSnappable(this PlayableElement element, float ttl = 0.2f) {
      if (element.TryGetComponent(out GridSnappable existing)) {
        return existing;
      }
      else {

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
        snappable.transform.parent = null;
        GameObject.Destroy(snappable, ttl); // destroy after ttl seconds to avoid cluttering the scene
        return snappable;
      }
    }
  }
  #endregion
}