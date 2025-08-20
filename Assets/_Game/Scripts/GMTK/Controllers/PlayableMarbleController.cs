using UnityEngine;

namespace GMTK {
  public class PlayableMarbleController : MonoBehaviour {

    [Header("Model")]
    public GameObject Model;

    [Header("Physics")]
    public float Mass = 5f;
    public float GravityScale = 1f;
    public float AngularDamping = 0.05f;
    public Vector2 InitialForce = Vector2.zero;
    [Tooltip("The minimum distance the marble's position has to change between Update calls, to consider is moving")]
    [Min(0.001f)]
    public float MinimalMovementThreshold = 0.01f;

    [Header("Spawn")]
    [Tooltip("Where should the Marble spawned")]
    public Transform SpawnTransform;
    [Tooltip("The LayerMask the marble should collide. Level and Interactables the most common")]
    public LayerMask GroundedMask;

    /// <summary>
    /// Whether the Marble is currently colliding with an object is considered ground (ex: wall, platform)
    /// </summary>
    public bool Grounded { get => IsGrounded(); }

    /// <summary>
    /// Whether the marble has moved since the last Update call
    /// </summary>
    public bool IsMoving => _timeSinceLastMove > 0f;

    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;
    protected GameEventChannel _eventChannel;
    protected Vector2 _lastMarblePosition = Vector2.zero;
    protected float _timeSinceLastMove = 0f;

    #region MonoBehaviour methods

    void Start() {

      if (_eventChannel == null) {
        _eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }

      if (Model == null) { Model = this.gameObject; }
      //at the start we make the last position equal to its current position.
      _lastMarblePosition = Model.transform.position;

      _rb = Model.GetComponent<Rigidbody2D>();
      _sr = Model.GetComponent<SpriteRenderer>();
      Spawn();
    }

    void Update() {
      if (!_rb.mass.Equals(Mass)) _rb.mass = Mass;
      if (!_rb.gravityScale.Equals(GravityScale)) _rb.gravityScale = GravityScale;
      if (!_rb.angularDamping.Equals(AngularDamping)) _rb.angularDamping = AngularDamping;

      //calculate if marble has moved since last update
      Vector2 currentMarblePosition = Model.transform.position;
      if (Vector2.Distance(currentMarblePosition, _lastMarblePosition) <= MinimalMovementThreshold) {
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0;
      }
    }

    private void LateUpdate() {
      //last position is updated last to prevent overridings
      _lastMarblePosition = Model.transform.position;
    }

    #endregion

    protected virtual bool IsGrounded() {
      RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, GroundedMask);
      return hit.collider != null;
    }

    #region Public API

    public void Spawn() {
      if (SpawnTransform != null) {
        Model.transform.position = SpawnTransform.position;
      }
      else { Model.transform.position = Vector3.zero; }
      Model.SetActive(true);
      StopMarble();
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
      if(_rb != null) _rb.AddForce(force, ForceMode2D.Impulse);
    }

    #endregion

  }
}