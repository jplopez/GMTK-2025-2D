
using UnityEngine;

namespace GMTK {

  [RequireComponent(typeof(Collider2D))]
  public class Booster : MonoBehaviour {

    [Tooltip("The force applied in the boost")]
    public Vector2 BoostForce = Vector2.zero;

    [Tooltip("Cooldown time before the booster can be used again.")]
    public float CooldownTime = 5f;

    private bool _isOnCooldown = false;

    private void OnTriggerEnter2D(Collider2D collision) {
      if (_isOnCooldown) return;
      HandleBoostRequest(collision);
    }

    private void OnTriggerExit2D(Collider2D collision) => HandleBoostRequest(collision);

    private void HandleBoostRequest(Collider2D collision) {
      if (TryGetPlayableMarble(collision, out var player)) {
        player.ApplyBoost(BoostForce);
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