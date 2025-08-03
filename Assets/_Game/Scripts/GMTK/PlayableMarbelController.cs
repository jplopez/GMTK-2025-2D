using UnityEngine;

namespace GMTK {
  public class PlayableMarbelController : MonoBehaviour {

    [Header("Model")]
    public GameObject Model;

    [Header("Physics")]
    public float Mass = 5f;
    public float GravityScale = 1f;
    public float AngularDamping = 0.05f;

    public Vector2 InitialForce = Vector2.zero;

    [Header("Spawn")]
    public Transform SpawnTransform;
    public bool LaunchOnStart = false;
    public LayerMask GroundedMask;

    protected Rigidbody2D _rb;
    protected SpriteRenderer _sr;

    public bool Grounded {  get =>  IsGrounded(); }

    void Start() {
      if (Model == null) { Model = this.gameObject; }
      _rb = Model.GetComponent<Rigidbody2D>();
      _sr = Model.GetComponent<SpriteRenderer>();
      Spawn();
      if (LaunchOnStart) { Launch(); }
    }

    void Update() {
      if(!_rb.mass.Equals(Mass)) _rb.mass = Mass;
      if(!_rb.gravityScale.Equals(GravityScale)) _rb.gravityScale = GravityScale;
      if(!_rb.angularDamping.Equals(AngularDamping)) _rb.angularDamping = AngularDamping; 
    }

    public void Spawn() {
      if (SpawnTransform != null) {
        Model.transform.position = SpawnTransform.position;
      }
      else { Model.transform.position = Vector3.zero; }
      StopMarble();
    }

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