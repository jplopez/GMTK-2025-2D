using Ameba;
using MoreMountains.Feedbacks;
using System;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This checkpoint notifies if the Marble enters or exits its trigger area. 
  /// </summary>
  /// 
  [RequireComponent(typeof(Collider2D))]
  public class Checkpoint : MonoBehaviour {

    [Flags]
    public enum CheckpointEventTrigger { 
      None = 0,
      OnEnter = 1, 
      OnExit = 2,
      Always = OnEnter | OnExit
    }

    [Flags]
    public enum ShowVisualCueModes {
      OnEnter = 1,
      OnExit = 2,
      NoCollision = 4,
      Always = OnEnter | OnExit | NoCollision,
    }


    [Header("Checkpoint Settings")]
    [Tooltip("Unique identifier for this checkpoint. Used in events to identify which checkpoint was hit.")]
    [SerializeField] private string checkpointID;
    [Tooltip("Optional visual cue to show when the Marble is near the checkpoint. If null, no visual cue will be shown.")]
    public GameObject VisualCuePrefab;
    [Tooltip("If the Visual Cue should be active at the start of the game. Typically true for Start checkpoints, false for End checkpoints")]
    public bool VisualCueActiveAtStart = false;
    [Tooltip("When the Checkpoint should be trigger its event logic: OnEnter, OnExit. Typically a Start checkpoint is on exit, and an End checkpoint is on enter, while middle game are both")]
    public ShowVisualCueModes ShowVisualCue = ShowVisualCueModes.NoCollision;
    [Tooltip("Feedback to play when the checkpoint visual cue is showing")]
    public MMF_Player VisualCueFeedback;
    [Space(10)]
    [Header("Event Settings")]
    [Tooltip("When the Checkpoint should be trigger its event logic: OnEnter, OnExit. Typically a Start checkpoint is on exit, and an End checkpoint is on enter, while middle game are both")]
    public CheckpointEventTrigger EventTrigger = CheckpointEventTrigger.OnEnter;
    [Space(5)]
    [Tooltip("Feedback to play when the Marble enters the checkpoint")]
    public MMF_Player OnEnterFeedback;
    [Space(5)]
    [Tooltip("Feedback to play when the Marble exits the checkpoint")]
    public MMF_Player OnExitFeedback;

    public Vector2 Position => transform.position;

    public string ID => checkpointID;

    protected GameEventChannel _eventsChannel;
    protected GameStateMachine _stateMachine;

    private void Awake() {
      
      if (!TryGetComponent<Collider2D>(out var col)) {
        this.LogError($"No Collider2D found on {gameObject.name}. Please add one and set it as Trigger.");
      }
      else if (!col.isTrigger) {
        this.LogWarning($"Collider2D on {gameObject.name} is not set as Trigger. Trigger events may not fire.");
      }

      if(_eventsChannel == null) {
        _eventsChannel = ServiceLocator.Get<GameEventChannel>();
      }
      if (_stateMachine == null) {
        _stateMachine = ServiceLocator.Get<GameStateMachine>();
      }

      EnsureCheckpointID();
    }

    private void Start() {
      //initial state of the visual cue depends on the flags in EventTrigger mode
      //if it has both OnEnter and OnExit, it starts active
      if(VisualCuePrefab != null) {
        ActivateVisualCue(VisualCueActiveAtStart || EventTrigger.HasFlag(CheckpointEventTrigger.Always));
      }
    }

    private void OnTriggerEnter2D(Collider2D other) {
      bool showCue = ShowVisualCue.HasFlag(ShowVisualCueModes.OnEnter) || ShowVisualCue.HasFlag(ShowVisualCueModes.Always);
      HandleTrigger(other, GameEventType.EnterCheckpoint, EventTrigger.HasFlag(CheckpointEventTrigger.OnExit), showCue, OnEnterFeedback);
    }

    private void OnTriggerExit2D(Collider2D other) {
      bool showCue = ShowVisualCue.HasFlag(ShowVisualCueModes.OnExit) || ShowVisualCue.HasFlag(ShowVisualCueModes.Always);
      HandleTrigger(other, GameEventType.ExitCheckpoint, EventTrigger.HasFlag(CheckpointEventTrigger.OnEnter), showCue, OnExitFeedback);
    }

    /// <summary>
    /// Common method to handle OnTrigger events.<br/>
    /// This will check if the collider belongs to a <see cref="PlayableMarbleController"/>, and if so, raise the appropriate event with <see cref="MarbleEventArgs"/>.<br/>
    /// </summary>
    /// <param name="other">the Collider2D passed from the OnTrigger event methods</param>
    /// <param name="eventType"><see cref="GameEventType.EnterCheckpoint"/> or <see cref="GameEventType.ExitCheckpoint"/> </param>
    /// <param name="triggerEvent">If the event should be raised or not, based on EventTrigger flags. Typically true, unless you want to only play feedbacks and show visual cue.</param>
    /// <param name="visualCue">If the visual cue should be shown or not, based on ShowVisualCue flags. Typically is shown if the flag opposite of the eventType is present.</param>
    /// <param name="feedback">the <see cref="MMF_Player"/> feedback (if defined) </param>
    private void HandleTrigger(Collider2D other, GameEventType eventType, bool triggerEvent = true, bool visualCue = false, MMF_Player feedback = null ) {
      //ignore collision if the game isn't on playing state
      if (other == null || (_stateMachine.Current != GameStates.Playing)) return;

      if (TryGetPlayableMarble(other, out PlayableMarbleController marble)) {
        this.LogDebug($"Checkpoint {checkpointID} triggered {eventType} by marble {marble.name}");
        if (triggerEvent) {
          var eventArgs = new MarbleEventArgs() {
            EventType = eventType,
            Position = new Vector2(other.transform.position.x, other.transform.position.y),
            Marble = marble,
            HitCheckpoint = this
          };
          _eventsChannel.Raise<EventArgs>(eventType, eventArgs);
        } else {
          this.Log($"Checkpoint {checkpointID} triggered {eventType} but event raising is disabled.");
        }

        this.LogDebug($"Playing feedback for {eventType} on checkpoint {checkpointID}");
        feedback?.PlayFeedbacks();

        this.LogDebug($"Setting visual cue for checkpoint {checkpointID} to {visualCue}");
        ActivateVisualCue(visualCue);
        
        this.LogDebug($"Checkpoint {checkpointID} trigger complete");
      } else {
        this.LogDebug($"Checkpoint {checkpointID} triggered by non-marble object {other.name}, ignoring.");
      }
    }

    private bool TryGetPlayableMarble(Collider2D other, out PlayableMarbleController marble) {
      //first check if the collider itself is the marble
      if (!other.TryGetComponent(out marble)) {
        //check if the parent is the marble 
        if(!other.transform.parent.TryGetComponent(out marble)) {
          return false;
        }
      }
      return (marble != null && marble.isActiveAndEnabled);
    }

    /// <summary>
    /// This methods updates the Visual Cue based on the game state and the ShowVisualCue flags.<br/>
    /// </summary>
    public void UpdateUI() {
      bool showCue = false;
      if ( _stateMachine.Current == GameStates.Reset
            || _stateMachine.Current == GameStates.Preparation) {
        showCue = VisualCueActiveAtStart || EventTrigger.HasFlag(CheckpointEventTrigger.Always);
      }
      else if (_stateMachine.Current == GameStates.Playing) {
        showCue = ShowVisualCue.HasFlag(ShowVisualCueModes.NoCollision) || ShowVisualCue.HasFlag(ShowVisualCueModes.Always);
      }
      ActivateVisualCue(showCue);
    }

    private void ActivateVisualCue(bool active = true) {
      if (VisualCuePrefab == null) return;
      VisualCuePrefab.SetActive(active);
    }

    /// <summary>
    /// Ensure the checkpoint has a valid ID. If not, generate a new GUID and assign it. 
    /// This prevents errors during event handling.
    /// </summary>
    private void EnsureCheckpointID() {
      if (string.IsNullOrWhiteSpace(checkpointID)) {
        checkpointID = "Checkpoint_" + Guid.NewGuid().ToString();
        this.LogWarning($"Checkpoint on {gameObject.name} had no ID assigned. Generated ID: {checkpointID}");
      }
    }

  }

}