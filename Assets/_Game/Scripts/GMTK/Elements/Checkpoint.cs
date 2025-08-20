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
    [Tooltip("When to show the visual cue. None = never, OnEnter = when marble enters, BoostOnExit = when marble exits, Always = always visible")]
    public VisualCueMode CueMode = VisualCueMode.OnEnter;

    public Vector2 Position => transform.position;
    public string ID => checkpointID;

    protected GameEventChannel _eventsChannel;

    private void Awake() {
      if (!TryGetComponent<Collider2D>(out var col)) {
        Debug.LogError($"[Checkpoint] No Collider2D found on {gameObject.name}. Please add one and set it as Trigger.");
      }
      else if (!col.isTrigger) {
        Debug.LogWarning($"[Checkpoint] Collider2D on {gameObject.name} is not set as Trigger. Trigger events may not fire.");
      }

      if(_eventsChannel == null) {
        _eventsChannel = Game.Context.EventsChannel;
      }
    }

    private void Start() {
      if (VisualCuePrefab != null) {
        ActivateVisualCue(CueMode == VisualCueMode.Always);
      }
    }

    private void OnTriggerEnter2D(Collider2D other) {
      //ignore collision if the game isn't on playing state
      if (Game.Context.CurrentGameState != GameStates.Playing) return;

      if (TryGetPlayableMarble(other, out PlayableMarbleController marble)) {
        var eventArgs = new MarbleEventArgs() {
          EventType = GameEventType.EnterCheckpoint,
          Position = new Vector2(other.transform.position.x, other.transform.position.y),
          Marble = marble,
          HitCheckpoint = this
        };
        _eventsChannel.Raise(GameEventType.EnterCheckpoint, eventArgs);

      }
      ActivateVisualCue(CueMode == VisualCueMode.Always || CueMode == VisualCueMode.OnEnter);
    }

    private void OnTriggerExit2D(Collider2D other) {
      //ignore collision if the game isn't on playing state
      if (Game.Context.CurrentGameState != GameStates.Playing) return;

      if (TryGetPlayableMarble(other, out PlayableMarbleController marble)) {
        var eventArgs = new MarbleEventArgs() {
          EventType = GameEventType.ExitCheckpoint,
          Position = new Vector2(other.transform.position.x, other.transform.position.y),
          Marble = marble,
          HitCheckpoint = this
        };
        _eventsChannel.Raise(GameEventType.ExitCheckpoint, eventArgs);
      }

      ActivateVisualCue(CueMode == VisualCueMode.Always || CueMode == VisualCueMode.OnExit);
    }

    private bool TryGetPlayableMarble(Collider2D other, out PlayableMarbleController marble) {
      if (!other.TryGetComponent(out marble)) {
        //check if the parent is the marbel
        marble = other.gameObject.GetComponentInParent<PlayableMarbleController>();
      }
      return (marble != null && marble.isActiveAndEnabled);
    }

    public void UpdateUI() {
      //updates only if game is playing
      if (Game.Context.CurrentGameState != GameStates.Playing) return;
      //this only updates for the permanent CueModes. OnEnter/BoostOnExit are handled on the OnTrigger* methods
      switch (CueMode) {
        case VisualCueMode.Always:
          ActivateVisualCue(true); break;
        case VisualCueMode.None:
          ActivateVisualCue(false); break;
        default:
          break;
      }
    }

    private void ActivateVisualCue(bool active = true) {
      if (VisualCuePrefab == null) return;
      VisualCuePrefab.SetActive(active);
    }

  }

}