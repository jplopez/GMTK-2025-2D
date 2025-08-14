using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {


  /// <summary>
  /// Represents any object in the game that can be snapped into a LevelGrid.
  /// This object controls the overall settings like transform, sprite, collider and state. 
  /// Additional Abilities are provided by SnappableComponent-derived classes
  /// </summary>
  /// 
  [ExecuteInEditMode]
  public class GridSnappable : MonoBehaviour {

    public enum SnappableBodyType { Static, Interactive }

    [HideInInspector] public SnappableTemplate AppliedTemplate;

    [Header("Snappable Settings")]
    [Tooltip("If set, this transform will be used for snapping instead of the GameObject's transform.")]
    public Transform SnapTransform; //if null, uses this.transform
    [Tooltip("If set, this transform will be used to look for SpriteRenders, RigidBody and Collisions. If empty, it will the GameObject's transform")]
    public Transform Model;
    [Tooltip("(Optional) highlight model to show when hovering or dragging.")]
    public GameObject HighlightModel;

    [Header("Local Grid Footprint")]
    [SerializeField] protected List<Vector2Int> _occupiedCells = new();

    [Header("Collision (Read-Only")]
    [SerializeField, DisplayWithoutEdit] protected Rigidbody2D _rigidBody;
    [SerializeField, DisplayWithoutEdit] protected PolygonCollider2D _collider;

    [Header("Behavior Settings")]
    [Tooltip("If true, the object can be dragged with the mouse.")]
    public bool Draggable = true;
    [Tooltip("If true, the object can be rotated.")]
    public bool CanRotate = false;
    [Tooltip("If true, the object can be flipped in the X and Y axises.")]
    public bool Flippable = false;

    [Header("Body Settings. Only if not StaticBody")]
    [Tooltip("If true, body is static, otherwise you can define Mass and Angular drag.")]
    public bool StaticBody = false;
    [Tooltip("If true, overrides the default body settings with the values from this object.")]
    public bool OverrideBodySettings = false;
    [Tooltip("Mass of the object. Higher mass means more inertia.")]
    [Range(0.1f, 100f)]
    public float Mass = 1f;
    [Tooltip("Angular drag of the object. Higher values mean more resistance to rotation.")]
    [Range(0f, 10f)]
    public float AngularDamping = 0.05f;

    public void SetStatic() => _bodyType = SnappableBodyType.Static;
    public bool IsStatic() => _bodyType == SnappableBodyType.Static;
    public void SetInteractive() => _bodyType= SnappableBodyType.Interactive;
    public bool IsInteractive() => _bodyType == SnappableBodyType.Interactive;
    public bool IsRegistered => _isRegistered;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    protected bool _isRegistered = false;
    protected SnappableBodyType _bodyType = SnappableBodyType.Static;
    protected SpriteRenderer _modelRenderer;
    protected List<SnappableComponent> _components = new();


    #region MonoBehaviour Methods

    void Awake() => Initialize();

    private void OnValidate() {
      gameObject.layer = LayerMask.NameToLayer("Interactives");
      Initialize();
    }

    // Ensure highlight sprites are initialized
    private void Start() {
      Debug.Log($"[GridSnappable] {name} using SnapTransform: {SnapTransform.name}");

      InitializeAllSnappableComponents();
    }

    private void Update() {
      _components.ForEach( component => component.RunBeforeUpdate());
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

      //store initial position for the Reset function
      _initialPosition = SnapTransform.position;
      _initialRotation = SnapTransform.rotation;
      _initialScale = SnapTransform.localScale;
      if(CheckForRenderers()) 
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

      // Ensure the GameObject has a Rigidbody2D and PolygonCollider2D. Add them to the Model if missing
      if (!Model.TryGetComponent(out Rigidbody2D rb)) {
        Debug.LogWarning($"[GridSnappable] No Rigidbody2D found on {Model.gameObject.name}. Adding one.");
        rb = Model.gameObject.AddComponent<Rigidbody2D>();
      }
      //by default bodytype is Dynamic and no gravity
      rb.bodyType = RigidbodyType2D.Dynamic;
      rb.gravityScale = 0f;

      // StaticBody using Static bodytype
      rb.bodyType = StaticBody ? RigidbodyType2D.Static : rb.bodyType;

      //if not draggable we freeze movements in X and Y 
      if (!Draggable) {
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
      }

      // Rotation constraints. 
      // we're not doing the freezee = !Rotable, because 
      // the rigidbody could be modified from the editor
      rb.freezeRotation = CanRotate ? false : true;

      // override body settings if override is true and bodytype is Dynamic
      if (OverrideBodySettings && !StaticBody) {
        rb.mass = Mass;
        rb.angularDamping = AngularDamping;
      }

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

    public void OnPointerOver() => SetGlow(true);

    public void OnPointerOut() => SetGlow(false);


    [Obsolete]
    public virtual void SetRegistered(bool registered = true) => _isRegistered = registered;

    public virtual void SetGlow(bool active) { if (HighlightModel != null) HighlightModel.SetActive(active); }


    #endregion

    #region Transformation methods

    public void UpdatePosition(Vector3 newPos) => SnapTransform.position = newPos;

    public void Rotate(Quaternion rot) => SnapTransform.rotation = rot;

    public void SetRotationAt(float angleDegrees) {
      if (!CanRotate) return;
      SnapTransform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);
    }

    public Quaternion GetRotation() => SnapTransform.rotation;

    public Vector3 GetPosition() => SnapTransform.position;

    public void RotateClockwise() {
      if (!CanRotate) return;
      SnapTransform.Rotate(Vector3.forward, -90f);
    }

    public void RotateCounterClockwise() {
      if (!CanRotate) return;
      SnapTransform.Rotate(Vector3.forward, 90f);
    }

    public void FlipX() {
      if (!Flippable) return;
      Vector3 scale = SnapTransform.localScale;
      scale.x *= -1;
      SnapTransform.localScale = scale;
    }

    public void FlipY() {
      if (!Flippable) return;
      Vector3 scale = SnapTransform.localScale;
      scale.y *= -1;
      SnapTransform.localScale = scale;
    }

    public void IncreaseScale(float amount) {
      amount = Mathf.Clamp(amount, 0.1f, 2f);
      SnapTransform.localScale += new Vector3(amount, amount, 0f);
    }

    public void ResetSnappable() {
      Debug.Log($"ResetSnappable {name}");
      SnapTransform.SetLocalPositionAndRotation(_initialPosition, _initialRotation);
      SnapTransform.localScale = _initialScale;

      _components.ForEach( c => c.RunResetComponent() );
    }

    #endregion

    public override string ToString() => name;
  }

}