using Ameba;
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
    [Tooltip("The minimum distance the marble's position has to change to consider is moving")]
    [Min(0.01f)]
    public float MinimalMovementThreshold = 0.01f;

    [Header("Spawn")]
    [Tooltip("Where should the Marble spawned")]
    public Transform SpawnTransform;
    [Tooltip("The LayerMask the marble should collide. Level and Interactables the most common")]
    public LayerMask GroundedMask;

    public bool Grounded { get => IsGrounded(); }
    public bool IsMoving => _timeSinceLastMove > 0f;

    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;
    protected GameEventChannel _eventChannel;
    protected Vector2 _lastMarblePosition = Vector2.zero;
    protected float _timeSinceLastMove = 0f;

    //private void Awake() {
    //  Game.Context.AddStateChangeListener(HandleStateChange);
    //}

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


      Vector2 currentMarblePosition = Model.transform.position;
      if (Vector2.Distance(currentMarblePosition, _lastMarblePosition) < MinimalMovementThreshold) {
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0;
      }
    }

    private void LateUpdate() {
      //last position is update end of the update, to prevent overridings
      _lastMarblePosition = Model.transform.position;
    }

    public void Spawn() {
      if (SpawnTransform != null) {
        Model.transform.position = SpawnTransform.position;
      }
      else { Model.transform.position = Vector3.zero; }
      StopMarble();
    }

    //public void HandleStateChange(StateMachineEventArg<GameStates> eventArg) {
    //  if (eventArg == null) return;
    //  switch(eventArg.ToState) {
    //    case GameStates.Reset:
    //    case GameStates.Preparation:
    //      Spawn();
    //      Model.SetActive(false);
    //      break;
    //    case GameStates.Playing:
    //      Model.SetActive(true);
    //      break;
    //  }
    //}

    public void StopMarble() {
      GravityScale = 0f;
      ResetForces();
    }

    public void Launch() {
      ResetForces();
      GravityScale = 1f;
      _rb.AddForce(InitialForce, ForceMode2D.Impulse);
    }

    private void ResetForces() {
      if (_rb == null) return;
      _rb.linearVelocity = Vector2.zero;
      _rb.angularVelocity = 0f;
      _rb.rotation = 0f;
    }
    private bool IsGrounded() {
      RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1f, GroundedMask);
      return hit.collider != null;
    }

    public void ApplyBoost(Vector2 boostForce) {
      _rb.AddForce(boostForce, ForceMode2D.Impulse);
    }

  }
}