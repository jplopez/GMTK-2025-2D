using Ameba;
using UnityEngine;

namespace GMTK {

  [RequireComponent(typeof(Collider2D))]
  public class Booster : MonoBehaviour {

    [Header("Booster Settings")]
    [Tooltip("The force applied in the boost")]
    public Vector2 BoostForce = Vector2.zero;

    [Header("Apply Boost:")]
    public bool BoostOnEntry = true;
    [Tooltip("This setting works best with both BoostOnEntry and BoostOnExit false and a cooldown < 1")]
    public bool BoostWhileIn = false;
    public bool BoostOnExit = true;

    [Tooltip("Cooldown time before the booster can be used again.")]
    public float CooldownTime = 5f;

    protected Collider2D _collider;
    private bool _isOnCooldown = false;

    private void Awake() {
      InitializationManager.WaitForInitialization(this, OnReady);
    }

    protected void OnReady() {
      //Only when BoostWhileIn is true we find the GameObject's collider
      //because the force will be applied during the Update, not in response to collisions.
      if (BoostWhileIn) {
        if (!TryGetComponent(out _collider)) {
          Debug.LogWarning($"[Booster] Collider2D missing on Booster {name}. BoostWhileIn will not work");
        }
      }
    }
    private void OnTriggerEnter2D(Collider2D collision) {
      if (_isOnCooldown) return;
      if (BoostOnEntry) HandleBoostRequest(collision);
    }

    private void OnTriggerExit2D(Collider2D collision) {
      if (_isOnCooldown) return;
      if (BoostOnExit) HandleBoostRequest(collision);
    }

    public void Update() {
      if (BoostWhileIn && _collider != null) 
        HandleBoostRequest(_collider);
    }

    private void HandleBoostRequest(Collider2D collision) {
      if (TryGetPlayableMarble(collision, out var player)) {
        player.ApplyForce(BoostForce);
        StartCoroutine(StartCooldown());
      }
    }

    private bool TryGetPlayableMarble(Collider2D other, out PlayableMarbleController marble) {
      if (!other.TryGetComponent<PlayableMarbleController>(out marble)) {
        //check if the parent is the marbel
        marble = other.gameObject.GetComponentInParent<PlayableMarbleController>();
      }
      return (marble != null && marble.isActiveAndEnabled);
    }

    private System.Collections.IEnumerator StartCooldown() {
      _isOnCooldown = true;
      yield return new WaitForSeconds(CooldownTime);
      _isOnCooldown = false;
    }
  }
}