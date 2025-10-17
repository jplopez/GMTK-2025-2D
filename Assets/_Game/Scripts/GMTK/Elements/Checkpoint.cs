using Ameba;
using MoreMountains.Feedbacks;
using System;
using System.Collections;
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
    [Tooltip("If the checkpoint should be hidden (active=false) upon initialization. Combine this with initializating upon an event, to reveal itself")]
    public bool HideOnInitialization = true;
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

    private bool _handlingTrigger = false;

    private void Awake() {

      if (!TryGetComponent<Collider2D>(out var col)) {
        this.LogError($"No Collider2D found on {gameObject.name}. Please add one and set it as Trigger.");
      }
      else if (!col.isTrigger) {
        this.LogWarning($"Collider2D on {gameObject.name} is not set as Trigger. Trigger events may not fire.");
      }

      EnsureDependencies();

      EnsureCheckpointID();
    }

    private void Start() {
      gameObject.SetActive(!HideOnInitialization);

      //initial state of the visual cue depends on the flags in EventTrigger mode
      //if it has both OnEnter and OnExit, it starts active
      ToggleVisualCue( (VisualCueActiveAtStart || EventTrigger.HasFlag(CheckpointEventTrigger.Always)));

    }
    private void Update() {
      if (_stateMachine.Current == GameStates.Playing && !_handlingTrigger) {
        ToggleVisualCue(ShowVisualCue.HasFlag(ShowVisualCueModes.NoCollision) || ShowVisualCue.HasFlag(ShowVisualCueModes.Always));
      }
    }

    private void OnDisable() {
      //disable visual cue and stop feedbacks
      ToggleVisualCue(false);
      StopAllFeedbacks();
    }

    private void OnTriggerEnter2D(Collider2D other) {
      if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
      bool showCue = ShowVisualCue.HasFlag(ShowVisualCueModes.OnEnter) || ShowVisualCue.HasFlag(ShowVisualCueModes.Always);
      StartCoroutine(HandleTriggerCoroutine(other, GameEventType.EnterCheckpoint, EventTrigger.HasFlag(CheckpointEventTrigger.OnEnter), showCue, OnEnterFeedback));
    }

    private void OnTriggerExit2D(Collider2D other) {
      if(!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
      bool showCue = ShowVisualCue.HasFlag(ShowVisualCueModes.OnExit) || ShowVisualCue.HasFlag(ShowVisualCueModes.Always);
      StartCoroutine(HandleTriggerCoroutine(other, GameEventType.ExitCheckpoint, EventTrigger.HasFlag(CheckpointEventTrigger.OnExit), showCue, OnExitFeedback));
    }

    /// <summary>
    /// Coroutine version of HandleTrigger that properly waits for feedback completion
    /// </summary>
    private IEnumerator HandleTriggerCoroutine(Collider2D other, GameEventType eventType, bool triggerEvent = true, bool visualCue = false, MMF_Player feedback = null) {
      //ignore collision if the game isn't on playing state
      if (other == null || (_stateMachine.Current != GameStates.Playing)) yield break;
      
      _handlingTrigger = true;
      this.Log($"Checkpoint {checkpointID} triggered {eventType}");
      
      if (TryGetPlayableMarble(other, out PlayableMarbleController marble)) {
        
        // Play feedback and wait for it to complete
        if (feedback != null) {
          this.Log($"Playing feedback for {eventType} on checkpoint {checkpointID}");
          yield return feedback.PlayFeedbacksCoroutine(transform.position, 1f, false);
          this.Log($"Feedback completed for {eventType} on checkpoint {checkpointID}");
        }

        // Set visual cue after feedback completes
        ToggleVisualCue(visualCue);

        // Now trigger the event after feedback has finished
        this.Log($"Checkpoint {checkpointID} triggered {eventType} by marble {marble.name}");
        if (triggerEvent) RaiseCheckpointEvent(eventType, marble);
        else this.LogWarning($"Checkpoint {checkpointID} {eventType} ignored");
      }
      else {
        this.LogDebug($"Checkpoint {checkpointID} triggered by non-marble object {other.name}, ignoring.");
      }
      
      _handlingTrigger = false;
    }

    private bool TryGetPlayableMarble(Collider2D other, out PlayableMarbleController marble) {
      //first check if the collider itself is the marble
      if (!other.TryGetComponent(out marble)) {
        //check if the parent is the marble 
        if (other.transform.parent != null && !other.transform.parent.TryGetComponent(out marble)) {
          return false;
        }
      }
      return (marble != null && marble.isActiveAndEnabled);
    }


    /// <summary>
    /// This methods updates the Visual Cue based on the game state and the ShowVisualCue flags.<br/>
    /// </summary>
    public void UpdateUI() {
      EnsureDependencies();
      bool showCue = false;
      //ensure reset stops any feedbacks
      if (_stateMachine.Current == GameStates.Reset) {
        StopAllFeedbacks();
      }
      //check initial states of checkpoint like hide it or play the cue
      if (_stateMachine.Current == GameStates.Reset
            || _stateMachine.Current == GameStates.Preparation) {
        if(HideOnInitialization) {
          showCue = false;
          gameObject.SetActive(false);
        }
        else {
          showCue = VisualCueActiveAtStart || EventTrigger.HasFlag(CheckpointEventTrigger.Always);
        }
      }
      // check if visual cue should be shown during playing state. This check also happens in Update()
      else if (_stateMachine.Current == GameStates.Playing) {
        showCue = ShowVisualCue.HasFlag(ShowVisualCueModes.NoCollision) || ShowVisualCue.HasFlag(ShowVisualCueModes.Always);
      }
      ToggleVisualCue(showCue);
    }

    private void RaiseCheckpointEvent(GameEventType eventType, PlayableMarbleController marble) {
      var eventArgs = new MarbleEventArgs() {
        EventType = eventType,
        Position = new Vector2(transform.position.x, transform.position.y),
        Marble = marble,
        HitCheckpoint = this
      };
      ////wait for feedbacks to stop
      //bool stillPlaying = waitForFeedbacks;
      //while(stillPlaying) {
      //  stillPlaying = (GameStateFeedback != null && GameStateFeedback.IsPlaying)
      //              || (OnExitFeedback != null && OnExitFeedback.IsPlaying); 
      //  if (stillPlaying) System.Threading.Thread.Sleep(10);
      //}
      _eventsChannel.Raise<EventArgs>(eventType, eventArgs);
    }

    private void ToggleVisualCue(bool active = true) {
      //play feedback if defined, otherwise just toggle the prefab active state
      if (VisualCueFeedback != null) {
        if (active) VisualCueFeedback.PlayFeedbacks();
        else VisualCueFeedback.StopFeedbacks();
      }

      if (VisualCuePrefab != null) VisualCuePrefab.SetActive(active);

    }

    private void EnsureDependencies() {
      if (_eventsChannel == null) {
        _eventsChannel = ServiceLocator.Get<GameEventChannel>();
      }
      if (_stateMachine == null) {
        _stateMachine = ServiceLocator.Get<GameStateMachine>();
      }
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

    private void StopAllFeedbacks() {
      if(OnEnterFeedback!=null) OnEnterFeedback.StopFeedbacks();
      if(OnExitFeedback!=null) OnExitFeedback.StopFeedbacks();
      if(VisualCueFeedback!=null) VisualCueFeedback.StopFeedbacks();
    }

  }

}