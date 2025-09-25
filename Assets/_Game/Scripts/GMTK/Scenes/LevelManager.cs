using System;
using UnityEngine;
using Ameba;

namespace GMTK {

  /// <summary>
  /// <para>
  /// Defines a playable level in the game.<br/>
  /// Manages the level's state and life cycle (start, reset, complete), monitoring marble's movements and interactions with checkpoints.
  /// Is also responsible for tracking level time, score, and stale conditions.<br/>
  /// </para>
  /// </summary>
  public class LevelManager : MonoBehaviour {

    [Header("Marble Settings")]
    [Tooltip("Reference to the Marble prefab")]
    public PlayableMarbleController PlayableMarble;
    [Tooltip("Initial force applied to the Marble when the level starts.")]
    public Vector2 MarbleInitialForce = Vector2.zero;

    [Header("Level Checkpoints")]
    [Tooltip("Transform representing the start checkpoint of the level.")]
    public Checkpoint StartLevelCheckpoint;
    [Tooltip("Optional. Transform representing the end checkpoint of the level. If missing, it will assume StartLevelCheckpoint")]
    public Checkpoint EndLevelCheckpoint;

    [Header("Level Stale Settings")]
    [Tooltip("Time in seconds after which the level is considered stale if the Marble hasn't moved.")]
    public float StaleTimeThreshold = 5f;
    public bool RestartOnStale = true;

    [Header("Level Time Settings")]
    public bool InfitiniteTime = false;
    public float LevelMaxTime = 300f; // 5 minutes max time for level

    [Header("Score Settings")]
    [Tooltip("Score multiplier for the level. Higher values yield higher scores. Score gets added during over time.")]
    public int ScoreMultiplier = 300;

    [Header("Winning Conditions")]
    [Tooltip("Number of checkpoints that must be reached to win the level. Currently not implemented.")]
    public int CheckpointsToWin = 1;
    [Tooltip("Number of checkpoints reached so far. Currently not implemented.")]
    public int CheckpointsReached = 0;

    //[Tooltip("LevelExtensions to encapsulate specific behaviours, that might not be needed everywhere")]
    //public List<LevelExtension> Extensions = new();

    public bool IsLevelStarted => _levelStarted;
    public bool IsLevelStale => _timeSinceLastMove >= StaleTimeThreshold;

    protected bool _levelStarted = false;
    protected bool _levelEnded = false;
    protected float _timeSinceLastMove = 0f;
    protected float _timeSinceLevelStart = 0f;

    protected LevelService _levelService;
    protected GameEventChannel _eventChannel;
    protected bool HasEndCheckpoint => EndLevelCheckpoint != null && !string.IsNullOrEmpty(EndLevelCheckpoint.ID);
    protected bool _isInitialized = false;

    #region MonoBehaviour methods

    // ensure this component always start as not initialized
    private void OnDisable() => _isInitialized = false;

    private void Awake() => Initialize();

    private void OnDestroy() => _eventChannel.RemoveListener<EventArgs>(GameEventType.EnterCheckpoint, HandleCheckpointEvent);

    public void Start() {
      //if (!_isInitialized) Initialize();
      if (!_isInitialized) {
        this.LogError("LevelManager not initialized. Aborting Start.");
        return;
      }

      if (!TryEnsureStartLevelCheckpoint()) {
        this.LogError("No StartLevelCheckpoint assigned or found in the scene, and could not create one from Marble's SpawnTransform. LevelManager can't start.");
        return;
      }

      StartMarble();
      ResetTimers();
    }

    public void Update() {
      if (!_isInitialized) return;

      //check if level has gone stale
      if (RestartOnStale && IsLevelStale && _levelStarted) {
        this.Log("Level has gone stale! Restarting level.");
        ResetLevel();
        return;
      }
      //check if level has ended
      if (_levelEnded) {
        _eventChannel.Raise(GameEventType.LevelObjectiveCompleted);
      }
      else if (_levelStarted) {
        _timeSinceLevelStart += Time.deltaTime;
        //check if player is out of time 
        if (!InfitiniteTime && LevelMaxTime > 0f && _timeSinceLevelStart >= LevelMaxTime) {
          this.Log("Level Time Expired! Restarting level.");
          ResetLevel();
        }
        UpdateMarbleMovement();
      }
    }

    #endregion

    #region Initialization 

    /// <summary>
    /// This methods ensures all dependencies and references are in place. If any is missing, it will log an error and return without initializing.
    /// When initialized, it will not run again.<br/>
    /// The method is made public so it can be called manually if needed (e.g. if LevelManager is added at runtime, or recovering from a crash).
    /// </summary>
    public virtual void Initialize() {
      if (_isInitialized) return;

      // get services from ServiceLocator. If any is missing, log error and return. LevelManager won't work.
      // this logic guarantees that LevelManager can be placed in any scene without relying on inspector references,
      // and that methods do not need to check on null references.
      if (_eventChannel == null) {
        if (ServiceLocator.TryGet<GameEventChannel>(out var theChannel)) {
          _eventChannel = theChannel;
          _eventChannel.AddListener<EventArgs>(GameEventType.EnterCheckpoint, HandleCheckpointEvent);
        }
        else {
          this.LogError($"_eventChannel is missing. LevelManager can't initialize");
          return;
        }
      }

      if (_levelService == null) {
        if (ServiceLocator.TryGet<LevelService>(out var theService)) {
          _levelService = theService;
        }
        else {
          this.LogError("_levelService is missing. LevelManager can't initialize");
          return;
        }
      }

      if (PlayableMarble == null) {
        if (FindFirstObjectByType<PlayableMarbleController>() is var theMarble) {
          PlayableMarble = theMarble;
        }
        else {
          this.LogError($"Could not find a PlayableMarbleController in the scene. Please assign one to LevelManager.");
          return;
        }
      }

      if (StartLevelCheckpoint == null) {
        // validate the checkpoint can be used as starting point
        if (FindFirstObjectByType<Checkpoint>() is var startCP) {
          if (string.IsNullOrEmpty(startCP.ID)) {
            this.LogWarning($"Can't find any Checkpoints in the Scene. Will try infer from Marble spawning point.");
          }
        }
        else {
          StartLevelCheckpoint = startCP;
        }
      }

      _isInitialized = true;
      this.Log($"LevelManager initialized? {_isInitialized}. StartLevelCheckpoint? {StartLevelCheckpoint != null}");
    }

    protected virtual bool TryEnsureStartLevelCheckpoint() {
      // try to use Marble's spawn point as StartCheckpoint
      // we can't create the checkpoint during Awake (unity constraint)
      if (StartLevelCheckpoint == null && PlayableMarble.SpawnTransform != null) {
        StartLevelCheckpoint = gameObject.AddComponent<Checkpoint>();
        StartLevelCheckpoint.transform.position = PlayableMarble.SpawnTransform.position;
        StartLevelCheckpoint.name = "StartCheckpoint";
        this.Log($"Created StartCheckpoint at PlayableMarble's SpawnTransform {PlayableMarble.SpawnTransform.position}");
      }
      return StartLevelCheckpoint != null;
    }

    #endregion

    #region Marble/Timers

    protected virtual void StartMarble() {
      PlayableMarble.Model.transform.position = StartLevelCheckpoint.Position;
      if (PlayableMarble.SpawnTransform == null) PlayableMarble.SpawnTransform = StartLevelCheckpoint.transform;
      EndLevelCheckpoint = EndLevelCheckpoint == null ? StartLevelCheckpoint : EndLevelCheckpoint;
      if (PlayableMarble.InitialForce == null || PlayableMarble.InitialForce == Vector2.zero) {
        PlayableMarble.InitialForce = MarbleInitialForce;
      }
      PlayableMarble.Spawn();
    }

    /// <summary>
    /// Checks if the Marble is moving to inform the score.<br/>
    /// Update time since last move to signal if the level has gone stale.
    /// </summary>
    protected virtual void UpdateMarbleMovement() {
      if (PlayableMarble.IsMoving) {
        _eventChannel.Raise(GameEventType.RaiseScore, Time.deltaTime);
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0f;
      }
    }

    protected virtual void ResetTimers() {
      _timeSinceLevelStart = 0f;
      _timeSinceLastMove = 0f;
    }

    #endregion

    #region Checkpoint Events Handlers (Wrapper and actual)

    /// <summary>
    /// Wrapper to transfrom input from EventArgs -> MarbleEventArgs
    /// </summary>
    protected virtual void HandleCheckpointEvent(EventArgs eventArgs) {
      if (eventArgs is MarbleEventArgs marbleEventArgs) {
        if (string.IsNullOrEmpty(marbleEventArgs.HitCheckpoint.ID)) {
          this.LogWarning($"Can't resolve Checkpoint event {marbleEventArgs.EventType} because Checkpoint ID is null or empty");
          return;
        }

        if (marbleEventArgs.EventType == GameEventType.EnterCheckpoint) {
          HandleEnterCheckpoint(marbleEventArgs.HitCheckpoint.ID);
        }
        else if (marbleEventArgs.EventType == GameEventType.ExitCheckpoint) {
          this.Log("LevelManager doesn't handle ExitCheckpoint events"); return;
        }
      }
    }

    /// <summary>
    /// Actual Marble Event Handler
    /// </summary>
    protected virtual void HandleEnterCheckpoint(string checkpointID) {
      // mark level as complete when Marble enters end checkpoint
      if (EndLevelCheckpoint.ID.Equals(checkpointID) && _levelStarted && !_levelEnded) {
        this.Log($"Marble entered end checkpoint {EndLevelCheckpoint.ID}. Ending level.");
        _eventChannel.Raise(GameEventType.LevelObjectiveCompleted);
      }
    }

    #endregion

    #region Public API for GameState changes

    //public int GetScoreAtLevelStart() => _scoreAtLevelStart;

    public void StartLevel() {
      ResetTimers();
      _levelStarted = true;
      _levelEnded = false;
      StartLevelCheckpoint.enabled = false;
      EndLevelCheckpoint.enabled = true;
      this.Log($"LevelStarted? {_levelStarted}");
    }

    public void EndLevel() {
      if (!_levelStarted) {
        this.LogWarning("Level has not started yet!");
        return;
      }
      if (_levelEnded) {
        this.LogWarning("Level has already ended!");
        return;
      }
      _levelStarted = false;
      _levelEnded = true;
      this.Log($"LevelEnded? {_levelEnded}");
    }

    /// <summary>
    /// Repositions Marble in StartLevelCheckpoint, reset timers, 
    /// resets score to the value at the start of the level
    /// </summary>
    public void ResetLevel() {
      _levelStarted = false;
      _levelEnded = false;
      StartLevelCheckpoint.enabled = true;
      EndLevelCheckpoint.enabled = false;
      ResetTimers();
      //_eventChannel.Raise(GameEventType.SetScoreValue, _scoreAtLevelStart);
    }

    #endregion

  }

}