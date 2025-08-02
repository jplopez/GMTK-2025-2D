using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [Tooltip("Optional highlight model to show when hovering or dragging.")]
    public GameObject HighlightModel;

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
    //[Tooltip("If true, the object will be affected by gravity.")]
    //public bool Gravity = false;

    [Header("Debug. Read only")]
    [DisplayWithoutEdit] public SpriteRenderer _model;
    [SerializeField, DisplayWithoutEdit] protected Rigidbody2D _rigidBody;
    [SerializeField, DisplayWithoutEdit] protected PolygonCollider2D _collider;
    [Tooltip("whether this element is in the grid or not")]

    public bool IsRegistered => _isRegistered;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;
    protected bool _isRegistered = false;

    void Awake() => Initialize();

    private void OnValidate() {
      gameObject.layer = LayerMask.NameToLayer("Interactives");
      Initialize();
    }

    public virtual void Initialize() {

      if (SnapTransform == null) SnapTransform = this.transform;

      _initialPosition = SnapTransform.position;
      _initialRotation = SnapTransform.rotation;
      _initialScale = SnapTransform.localScale;

      // Ensure the GameObject has a SpriteRenderer
      if (gameObject.TryGetComponent(out SpriteRenderer renderer)) {
        _model = renderer;
        _model.sortingLayerName = "Interactives";
      }
      else {
        Debug.LogWarning($"[GridSnappable] No SpriteRenderer found on {gameObject.name}.");
      }
      //hide existing highlight
      if (HighlightModel != null) HighlightModel.SetActive(false); 

      // Ensure the GameObject has a Rigidbody2D and PolygonCollider2D
      if (gameObject.TryGetComponent(out Rigidbody2D rb)) {
        _rigidBody = rb;
      }
      else {
        Debug.LogWarning($"[GridSnappable] No Rigidbody2D found on {gameObject.name}. Adding one.");
        _rigidBody = gameObject.AddComponent<Rigidbody2D>();
      }
      //by default bodytype is Dynamic and no gravity
      _rigidBody.bodyType = RigidbodyType2D.Dynamic;
      _rigidBody.gravityScale = 0f;

      // StaticBody using Static bodytype
      _rigidBody.bodyType = StaticBody ? RigidbodyType2D.Static : _rigidBody.bodyType;

      //if not draggable we freeze movements in X and Y 
      if (!Draggable) {
        _rigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY;
      }

      // Rotation constraints. 
      // we're not doing the freezee = !Rotable, because 
      // the rigidbody could be modified from the editor
      _rigidBody.freezeRotation = CanRotate ? false : true;

      // override body settings if override is true and bodytype is Dynamic
      if (OverrideBodySettings && !StaticBody) {
        _rigidBody.mass = Mass;
        _rigidBody.angularDamping = AngularDamping;
      }

      if (gameObject.TryGetComponent(out PolygonCollider2D collider)) {
        _collider = collider;
      }
      else {
        Debug.LogWarning($"[GridSnappable] No PolygonCollider2D found on {gameObject.name}. Adding one.");
        _collider = gameObject.AddComponent<PolygonCollider2D>();
      }
    }

    // Ensure highlight sprites are initialized
    private void Start() {
      Debug.Log($"[GridSnappable] {name} using SnapTransform: {SnapTransform.name}");
    }

    public void OnPointerOver() => SetGlow(true);

    public void OnPointerOut() => SetGlow(false);

    public virtual void OnRegistered(Vector2Int coord) => _isRegistered = true;

    public virtual void OnRemoved(Vector2Int coord) => _isRegistered = false;

    public virtual void SetGlow(bool active) { if (HighlightModel != null) HighlightModel.SetActive(active); }

    public void UpdatePosition(Vector3 newPos) => SnapTransform.position = newPos;

    public void Rotate(Quaternion rot) => SnapTransform.rotation = rot;

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
      SnapTransform.SetLocalPositionAndRotation(_initialPosition, _initialRotation);
      SnapTransform.localScale = _initialScale;
    }

  }

}