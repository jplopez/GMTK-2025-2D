using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This checkpoint notifies if the marble enters or exits its trigger area. 
  /// </summary>
  /// 
  [RequireComponent(typeof(Collider2D))]
  public class Checkpoint : MonoBehaviour {

    public enum VisualCueMode {
      None,
      OnEnter,
      OnExit,
      Always
    }

    [SerializeField] private string checkpointID;

    [Tooltip("Optional visual cue to show when the marble is near the checkpoint. If null, no visual cue will be shown.")]
    public GameObject VisualCuePrefab;
    [Tooltip("When to show the visual cue. None = never, OnEnter = when marble enters, OnExit = when marble exits, Always = always visible")]
    public VisualCueMode CueMode = VisualCueMode.OnEnter;

    public Vector2 Position => transform.position;
    public string ID => checkpointID;

    public delegate void MarbleCheckpointEvent(PlayableMarbleController marble, string checkpointID);
    public static event MarbleCheckpointEvent OnMarbleEnteringCheckpoint;
    public static event MarbleCheckpointEvent OnMarbleExitingCheckpoint;

    private void Awake() {
      if (!TryGetComponent<Collider2D>(out var col)) {
        Debug.LogError($"[Checkpoint] No Collider2D found on {gameObject.name}. Please add one and set it as Trigger.");
      }
      else if (!col.isTrigger) {
        Debug.LogWarning($"[Checkpoint] Collider2D on {gameObject.name} is not set as Trigger. Trigger events may not fire.");
      }
    }

    private void Start() {
      if (VisualCuePrefab != null) {
        ActivateVisualCue(CueMode == VisualCueMode.Always);
      }
    }

    private void OnTriggerEnter2D(Collider2D other) {
      if (TryGetPlayableMarble(other, out PlayableMarbleController marble))
        OnMarbleEnteringCheckpoint?.Invoke(marble, ID);
      if (CueMode == VisualCueMode.OnEnter || CueMode == VisualCueMode.Always) {
        ActivateVisualCue();
      }
      else {
        ActivateVisualCue(false);
      }
    }

    private void OnTriggerExit2D(Collider2D other) {
      if (TryGetPlayableMarble(other, out PlayableMarbleController marble))
        OnMarbleExitingCheckpoint?.Invoke(marble, ID);
      if (CueMode == VisualCueMode.OnExit || CueMode == VisualCueMode.Always) {
        ActivateVisualCue();
      }
      else {
        ActivateVisualCue(false);
      }
    }

    private bool TryGetPlayableMarble(Collider2D other, out PlayableMarbleController marble) {
      if (!other.TryGetComponent(out marble)) {
        //check if the parent is the marbel
        marble = other.gameObject.GetComponentInParent<PlayableMarbleController>();
      }
      return (marble != null && marble.isActiveAndEnabled);
    }

    private void ActivateVisualCue(bool active = true) {
      if (VisualCuePrefab == null) return;
      VisualCuePrefab.SetActive(active);
    }

  }

}