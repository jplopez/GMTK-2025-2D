using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK {


  /// <summary>
  /// Represents any object in the game that can be snapped into a LevelGrid.
  /// This object controls the overall settings like transform, sprite, collider and state. 
  /// Additional Abilities are provided by SnappableComponent-derived classes
  /// </summary>
  /// 
  //[ExecuteInEditMode]
  public class GridSnappable : MonoBehaviour {

    public enum SnappableBodyType { Static, Interactive }

    public enum BehaviourDelegateType { None, Components }

    [HideInInspector] public SnappableTemplate AppliedTemplate;


    [Header("Snappable Settings")]
    [Tooltip("If set, this transform will be used for snapping instead of the GameObject's transform.")]
    public Transform SnapTransform; //if null, uses this.transform
    [Tooltip("If set, this transform will be used to look for SpriteRenders, RigidBody and Collisions. If empty, it will the GameObject's transform")]
    public Transform Model;
    [Tooltip("(Optional) highlight model to show when hovering or dragging.")]
    public GameObject HighlightModel;

    [Header("Dragging")]
    [Tooltip("If true, the object can be dragged. Set it to false for elements that you want static in the playable area")]
    public bool Draggable = true;

    [Header("Local Grid Footprint")]
    [SerializeField] protected List<Vector2Int> _occupiedCells = new();

    [Header("Actions")]
    [Tooltip("Who should solve inputs like rotation and flip. By default, the Snappable handles them, but they can be delegated to components.")]
    public BehaviourDelegateType BehaviourDelegate = BehaviourDelegateType.None;
    [Tooltip("If true, the object can be flipped on the X and Y axis. This flag applies to all BehaviourDelegate values ")]
    public bool Flippable = false;
    [Tooltip("If true, the object can be rotated in its Z axis. This flag applies to all BehaviourDelegate values ")]
    public bool CanRotate = false;

    [Header("Feedbacks")]
    [Tooltip("The feedback when the when the pointer is over the element.")]
    public GameObject PointerOnFeedback;
    [Tooltip("The feedback when the when the pointer moves out of the element.")]
    public GameObject PointerOutFeedback;


    //public void SetStatic() => _bodyType = SnappableBodyType.Static;
    //public bool IsStatic() => _bodyType == SnappableBodyType.Static;
    //public void SetInteractive() => _bodyType = SnappableBodyType.Interactive;
    //public bool IsInteractive() => _bodyType == SnappableBodyType.Interactive;
    public bool IsRegistered => _isRegistered;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    protected bool _isRegistered = false;
    protected SnappableBodyType _bodyType = SnappableBodyType.Static;
    protected SpriteRenderer _modelRenderer;
    protected PolygonCollider2D _collider;
    protected List<SnappableComponent> _components = new();

    protected event Action<GridSnappableEventArgs> OnSnappableEvent;

    #region MonoBehaviour Methods

    void Awake() => Initialize();

    private void OnValidate() {
      gameObject.layer = LayerMask.NameToLayer("Interactives");
      Initialize();
    }

    // Ensure highlight sprites are initialized
    private void Start() {
      //Debug.Log($"[GridSnappable] {name} using SnapTransform: {SnapTransform.name}");
      InitializeAllSnappableComponents();
    }

    private void Update() {
      _components.ForEach(component => component.RunBeforeUpdate());
      //Add any early-update logic here

      _components.ForEach(c => c.RunOnUpdate());

      //Add GridSnappable Update logic here
    }

    private void LateUpdate() {
      _components.ForEach(c => c.RunAfterUpdate());
    }

    private void OnDestroy() {
      _components.ForEach(c => c.RunFinalize());
    }

    #endregion

    #region Initialize 

    public virtual void Initialize() {

      // Default assignments
      if (SnapTransform == null) SnapTransform = this.transform;
      if (Model == null) Model = this.transform;

      //store initial position for the ResetToStartingState function
      _initialPosition = SnapTransform.position;
      _initialRotation = SnapTransform.rotation;
      _initialScale = SnapTransform.localScale;
      if (CheckForRenderers())
        InitGridSnappable();
    }

    private void InitializeAllSnappableComponents() {
      _components.Clear();
      _components.AddRange(GetComponents<SnappableComponent>());
      _components.ForEach(comp => comp.TryInitialize());
    }

    private bool CheckForRenderers() {
      //Check Model has a sprite renderer
      if (Model.TryGetComponent(out SpriteRenderer renderer)) {
        _modelRenderer = renderer;
        return true;
      }
      else {
        SpriteRenderer[] renderers = Model.GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0) {
          Debug.LogWarning($"[GridSnappable] No SpriteRenderer found on {Model.name}.");
        }
      }
      return false;
    }

    private void InitGridSnappable() {
      //hide existing highlight
      if (HighlightModel != null) HighlightModel.SetActive(false);

      if (Model.gameObject.TryGetComponent(out PolygonCollider2D collider)) {
        _collider = collider;
      }
      else {
        Debug.LogWarning($"[GridSnappable] No PolygonCollider2D found on {Model.gameObject.name}. Adding one.");
        _collider = Model.gameObject.AddComponent<PolygonCollider2D>();
      }

    }

    #endregion

    #region Occupancy
    public List<Vector2Int> GetFootprint() => _occupiedCells;

    public IEnumerable<Vector2Int> GetWorldOccupiedCells(Vector2Int gridOrigin, bool flippedX = false, bool flippedY = false, int rotation = 0) {
      foreach (var local in _occupiedCells) {
        var transformed = TransformLocalCell(local, flippedX, flippedY, rotation);
        yield return transformed + gridOrigin;
      }
    }

    #endregion

    #region Grid Utils

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

    #region EventHandlers

    public void OnPointerOver() {
      GridSnappableEventArgs eventArgs = new(this, transform.position, SnappableComponentEventType.OnPointerOver);
      OnSnappableEvent?.Invoke(eventArgs);
      //TODO move to feedback
      SetGlow(true);
    }
    public void OnPointerOut() {
      GridSnappableEventArgs eventArgs = new(this, transform.position, SnappableComponentEventType.OnPointerOut);
      OnSnappableEvent?.Invoke(eventArgs);
      //TODO move to feedback
      SetGlow(false);
    }
    public virtual void SetGlow(bool active) { if (HighlightModel != null) HighlightModel.SetActive(active); }

    [Obsolete]
    public virtual void SetRegistered(bool registered = true) => _isRegistered = registered;

    public void AddComponentListener(SnappableComponent component) => OnSnappableEvent += component.OnSnappableEvent;

    public void RemoveComponentListener(SnappableComponent component) => OnSnappableEvent -= component.OnSnappableEvent;

    #endregion

    #region Transformation methods

    public void UpdatePosition(Vector3 newPos) => SnapTransform.position = newPos;

    public Quaternion GetRotation() => SnapTransform.rotation;

    public Vector3 GetPosition() => SnapTransform.position;

    public void RotateClockwise() {
      if (CanRotate) {
        TryExecuteOrDelegateToComponents(SnappableComponentEventType.RotateCW,
          snappable => {
            snappable.SnapTransform.Rotate(Vector3.forward, -90f);
          });
      }
    }

    public void RotateCounterClockwise() {
      if (CanRotate) {
        TryExecuteOrDelegateToComponents(SnappableComponentEventType.RotateCCW,
          snappable => {
            snappable.SnapTransform.Rotate(Vector3.forward, 90f);
          });
      }
    }

    public void FlipX() {
      if (Flippable) {
        TryExecuteOrDelegateToComponents(SnappableComponentEventType.FlippedX,
          snappable => {
            Vector3 scale = snappable.SnapTransform.localScale;
            scale.x *= -1;
            snappable.SnapTransform.localScale = scale;
          });
      }
    }

    public void FlipY() {
      if (Flippable) {
        TryExecuteOrDelegateToComponents(SnappableComponentEventType.FlippedY,
          snappable => {
            Vector3 scale = snappable.SnapTransform.localScale;
            scale.y *= -1;
            snappable.SnapTransform.localScale = scale;
          });
      }
    }

    private void TryExecuteOrDelegateToComponents(SnappableComponentEventType eventType, Action<GridSnappable> callback) {
      switch (BehaviourDelegate) {

        case BehaviourDelegateType.None:
          callback?.Invoke(this); break;

        case BehaviourDelegateType.Components:
          GridSnappableEventArgs eventArgs = new(this, transform.position, eventType);
          OnSnappableEvent?.Invoke(eventArgs);
          break;
      }
    }

    public void ResetSnappable() {
      //Debug.Log($"ResetSnappable {name}");
      SnapTransform.SetLocalPositionAndRotation(_initialPosition, _initialRotation);
      SnapTransform.localScale = _initialScale;

      _components.ForEach(c => c.RunResetComponent());
    }

    #endregion

    public override string ToString() => name;
  }

}