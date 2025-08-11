using System;
using UnityEngine;


namespace GMTK {


  /// <summary>
  /// Provides functionality for snapping a game object to a _grid when dragged with the mouse.
  /// </summary>
  /// <remarks>This component allows a game object to be moved freely with the mouse and automatically snaps it
  /// to the nearest _grid position when the mouse is released. An optional visual highlight is displayed during dragging
  /// to indicate the snap position.</remarks>
  /// 
  [ExecuteInEditMode]
  public class GridSnappable : MonoBehaviour {

    [HideInInspector] public SnappableTemplate AppliedTemplate;

    [Header("Snappable Settings")]
    [Tooltip("If set, this transform will be used for snapping instead of the GameObject's transform.")]
    public Transform SnapTransform; //if null, uses this.transform
    [Tooltip("If set, this transform will be used to look for SpriteRenders, RigidBody and Collisions. If empty, it will the GameObject's transform")]
    public Transform Model;
    [Tooltip("(Optional) highlight model to show when hovering or dragging.")]
    public GameObject HighlightModel;

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


    public bool IsRegistered => _isRegistered;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    protected bool _isRegistered = false;

    protected SpriteRenderer _modelRenderer;

    void Awake() => Initialize();

    private void OnValidate() {
      gameObject.layer = LayerMask.NameToLayer("Interactives");
      Initialize();
    }

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

    //Vector3 lastPos = Vector3.zero;
    //private void Update() {
    //  if (!lastPos.Equals(transform.position)) {
    //    //System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
    //    Debug.Log($"Snappable {name} changed position {lastPos} -> {transform.position}");
    //    lastPos = transform.position;
    //  }
    //}

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

    // Ensure highlight sprites are initialized
    private void Start() {
      Debug.Log($"[GridSnappable] {name} using SnapTransform: {SnapTransform.name}");
    }

    public void OnPointerOver() => SetGlow(true);

    public void OnPointerOut() => SetGlow(false);

    public virtual void SetRegistered(bool registered = true) => _isRegistered = registered;

    public virtual void SetGlow(bool active) { if (HighlightModel != null) HighlightModel.SetActive(active); }

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

    public void ResetTransform() {
      Debug.Log($"ResetTransform {name}");
      SnapTransform.SetLocalPositionAndRotation(_initialPosition, _initialRotation);
      SnapTransform.localScale = _initialScale;
    }

  }

}