using Ameba;
using System;
using System.Collections.Generic;
using UnityEngine;
using GMTK.Extensions;
using UnityEngine.Events;

namespace GMTK {

  [Flags]
  public enum SelectionTrigger {
    None = 0,
    OnHover = 1,
    OnClick = 2,
    OnDoubleClick = 4,

  }

  /// <summary>
  /// Represents any object in the game that can be snapped into a LevelGrid and dragged by the player.
  /// This class replaces GridSnappable and implements the IDraggable, ISelectable, and IHoverable interfaces for improved interaction support.
  /// Additional abilities are provided by PlayableElementComponent-derived classes.
  /// </summary>
  [AddComponentMenu("GMTK/Playable Element")]
  [RequireComponent(typeof(ElementPointerComponent))]
  public partial class PlayableElement : MonoBehaviour, IDraggable, ISelectable, IHoverable {

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
    [Space(10)]

    //UnityEvents
    //[Help("UnityEvents to add additional behaviors to PlayableElements. PlayableElementComponent-derived components attached to the same GameObject subscribe to this events automatically")]
    [Space]
    [Header("Selection")]
    public UnityEvent<PlayableElementEventArgs> OnSelected = new();
    public UnityEvent<PlayableElementEventArgs> OnDeselected = new();
    [Space]
    [Header("Hovering")]
    public UnityEvent<PlayableElementEventArgs> OnHovered = new();
    public UnityEvent<PlayableElementEventArgs> OnUnhovered = new();
    [Space]
    [Header("Drag Start")]
    public UnityEvent<PlayableElementEventArgs> OnDragStart = new();
    public UnityEvent<PlayableElementEventArgs> OnDragging = new();
    [Tooltip("The minimum distance the element needs to be dragged to notify 'OnDragging'")]
    [Range(0.01f, 0.1f)]
    public float DragMinDistance = 0.02f;
    [Tooltip("The time in seconds in between notification of 'OnDragging' ")]
    [Range(0.1f, 1f)]
    public float DragCooldown = 0.1f;
    public UnityEvent<PlayableElementEventArgs> OnDragEnd = new();
    [Space]
    [Header("Input Controls (rotate, flip)")]
    public UnityEvent<PlayableElementEventArgs> OnPlayerInput = new();
    public UnityEvent<PlayableElementEventArgs> OnFlip = new();
    public UnityEvent<PlayableElementEventArgs> OnRotate = new();


    // Public properties for compatibility with existing code
    public bool IsRegistered => _isRegistered;

    // Private fields
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    protected bool _isRegistered = false;
    private float _lastDragUpdateTime = 0f;
    private bool _canDoDraggingUpdate = true;
    protected SpriteRenderer _modelRenderer;
    protected PolygonCollider2D _collider;
    protected List<PlayableElementComponent> _components = new();

    protected GameEventChannel _gameEventChannel;

    private ElementPointerComponent _pointerComponent;

    private bool HasSelectionTrigger(SelectionTrigger trigger) => (SelectionTriggers & trigger) != 0;

    #region MonoBehaviour Methods

    private void Awake() => Initialize();

    private void OnValidate() {
      gameObject.layer = LayerMask.NameToLayer("Interactives");
      Initialize();
    }

    private void Start() => InitializeAllPlayableElementComponents();

    private void Update() {
      if (_components?.Count == 0) return;
      _components.ForEach(c => { if (c != null) c.RunBeforeUpdate(); });
      _components.ForEach(c => { if (c != null) c.RunOnUpdate(); });

      // Dragging update cooldown check
      // draggable time + cooldown should be less than current time to allow dragging update
      // remember: newer time is less than older time.
      if (_lastDragUpdateTime + DragCooldown < Time.time && IsBeingDragged) {
        //this.Log($"PlayableElement '{name}' can do dragging update again");
        _canDoDraggingUpdate = true;
      }
    }

    private void LateUpdate() => _components?.ForEach(c => { if (c != null) c.RunAfterUpdate(); });

    private void OnDestroy() => _components?.ForEach(c => { if (c != null) c.RunFinalize(); });

    #endregion

    #region Initialize 

    public virtual void Initialize() {
      // Default assignments
      if (SnapTransform == null) SnapTransform = transform;
      if (Model == null) Model = transform;

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
      _components?.Clear();
      _components?.AddRange(GetComponents<PlayableElementComponent>());
      _components?.ForEach(c => { c.SetPlayableElement(this); });
      _components?.ForEach(c => { if (c != null) c.TryInitialize(); });

      // Cache the ElementPointerComponent reference
      _pointerComponent = GetComponent<ElementPointerComponent>();
      this.Log($"PlayableElement '{name}' initialized with {_components.Count} PlayableElementComponents");
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
          this.LogWarning($"No SpriteRenderer found on {Model.name}.");
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
        this.LogWarning($"No PolygonCollider2D found on {Model.gameObject.name}. Adding one.");
        _collider = Model.gameObject.AddComponent<PolygonCollider2D>();
      }
    }

    #endregion

    #region Occupancy (Grid System Compatibility)

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

    /// <summary>
    /// <para>
    /// Raises a game event through the game event channel, using the specified parameters to construct the event arguments.
    /// Subclasses can override this method to customize event handling behavior.
    /// </para>
    /// <para>
    /// The returned <see cref="PlayableElementEventArgs"/> instance contains the details of the raised event.
    /// Also informs it the event has already been handled by a <see cref="PlayableElementComponent"/>.
    /// </para>
    /// <para>
    /// <see cref="PlayableElementComponent"/> can mark the event as handled by setting the Handled property to true.
    /// This is useful to chain handlers or preventing further default processing.
    /// </para>
    /// </summary>
    /// <param name="gameEvent">The type of game event to raise. This determines the category of the event.</param>
    /// <param name="eventType">The specific type of playable element event to include in the raised event.</param>
    /// <param name="worldPosition">An optional world position associated with the event. If not provided, the event will not include positional
    /// data.</param>
    /// <returns>A <see cref="PlayableElementEventArgs"/> instance containing the details of the raised event.</returns>
    protected virtual PlayableElementEventArgs RaiseGameEvent(GameEventType gameEvent, PlayableElementEventType eventType, Vector3? worldPosition = null) {
      var eventArgs = BuildEventArgs(gameEvent, eventType, worldPosition);
      _gameEventChannel.Raise(gameEvent, eventArgs);
      return eventArgs;
    }

    /// <summary>
    /// Utility method to build PlayableElementEventArgs instances.
    /// </summary>
    protected PlayableElementEventArgs BuildEventArgs(GameEventType gameEvent, PlayableElementEventType eventType, Vector3? worldPosition = null) =>
       new(this, worldPosition ?? transform.position, eventType);

    /// <summary>
    /// This methods invokes the PlayableElementEvent handlers for the ElementPointerComponent, if it exists.<br/>
    /// The purpose is to prioritize the handling by that component, before raising the event to other listeners.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns>true if the event was handled by the ElementPointerComponent. false otherwise</returns>
    private bool TryDelegateToPointerComponent(PlayableElementEventArgs eventArgs) {
      if (_pointerComponent == null) return false;

      this.LogDebug($"Delegating {eventArgs.EventType} event to ElementPointerComponent on {name}");
      switch (eventArgs.EventType) {
        case PlayableElementEventType.Selected:
          _pointerComponent.OnSelected(eventArgs);
          break;
        case PlayableElementEventType.Deselected:
          _pointerComponent.OnDeselected(eventArgs);
          break;
        case PlayableElementEventType.PointerOver:
          _pointerComponent.OnHovered(eventArgs);
          break;
        case PlayableElementEventType.PointerOut:
          _pointerComponent.OnUnhovered(eventArgs);
          break;
        default:
          this.LogWarning($"{eventArgs.EventType} not handled in TryDelegateToPointerComponent for {name}");
          return false;
      }
      return true;
    }

    protected bool CanDoDraggingUpdate => _canDoDraggingUpdate;

    protected void ResetDraggingUpdateTimer() {
      _lastDragUpdateTime = Time.time;
      _canDoDraggingUpdate = false;
    }

    #endregion

    #region Transformation methods

    public void ResetTransform() {
      SnapTransform.SetPositionAndRotation(_initialPosition, _initialRotation);
      SnapTransform.localScale = _initialScale;
    }

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
      //RaisePlayableElementEvent(eventType);
      OnRotate?.Invoke(eventArgs);

      // If no component handled it, do the default rotation using extension methods
      if (!eventArgs.Handled) {
        if (eventType == PlayableElementEventType.RotateCW) {
          SnapTransform.RotateClockwise();
        }
        else {
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
      this.LogDebug($"Flipping {(flipX ? "X" : "")} {(flipY ? "Y" : "")} on {name}");

      PlayableElementEventType eventType = flipX ? PlayableElementEventType.FlippedX : PlayableElementEventType.FlippedY;

      // Notify components first - they might handle the flip
      PlayableElementEventArgs eventArgs = new(this, transform.position, eventType); 
      OnFlip?.Invoke(eventArgs);

      // If no component handled it, do the default flip using extension methods
      if (eventArgs != null && eventArgs.Handled) return;

      if (flipX) {
        SnapTransform.FlipX();
      }

      if (flipY) {
        SnapTransform.FlipY();
      }
    }

    #endregion

    public override string ToString() => name;
  }

}