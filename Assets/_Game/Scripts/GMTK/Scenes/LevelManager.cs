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

    //private const string LEVEL_COMPLETE_SCENE_NAME = "LevelComplete";

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

    #region MonoBehaviour methods

    private void Awake() {

      _eventChannel = ServiceLocator.Get<GameEventChannel>();
      _levelService = ServiceLocator.Get<LevelService>();
      //_levelOrderManager = ServiceLocator.Get<LevelOrderManager>();
      if (_eventChannel == null) {
        this.Log($"LevelManager: _eventChannel is missing. LevelManager won't be able to handle game events");
        return;
      }

      //if (_levelOrderManager != null && _levelService != null) {
      //  _levelOrderManager.Initialize(_levelService);
      //}

      _eventChannel.AddListener<EventArgs>(GameEventType.EnterCheckpoint, HandleCheckPointEvent);
    }

    private void OnDestroy() {
      _eventChannel.RemoveListener<EventArgs>(GameEventType.EnterCheckpoint, HandleCheckPointEvent);
    }

    public void Start() {
      if (StartLevelCheckpoint == null) {
        this.LogWarning("StartLevelCheckpoint is not assigned in LevelManager.");
        return;
      }
      StartMarble();
      ResetTimers();
    }

    private void StartMarble() {
      if (PlayableMarble == null) {
        this.LogWarning("PlayableMarble is not assigned in LevelManager.");
        return;
      }
      PlayableMarble.Model.transform.position = StartLevelCheckpoint.Position;
      if (PlayableMarble.SpawnTransform == null) PlayableMarble.SpawnTransform = StartLevelCheckpoint.transform;
      EndLevelCheckpoint = EndLevelCheckpoint == null ? StartLevelCheckpoint : EndLevelCheckpoint;
      if (PlayableMarble.InitialForce == null || PlayableMarble.InitialForce == Vector2.zero) {
        PlayableMarble.InitialForce = MarbleInitialForce;
      }
      PlayableMarble.Spawn();
    }

    public void Update() {
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

    #region Checkpoint Events Handlers (Wrapper and actual)

    /// <summary>
    /// Wrapper to transfrom input from EventArgs -> MarbleEventArgs
    /// </summary>
    private void HandleCheckPointEvent(EventArgs eventArgs) {
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
    protected void HandleEnterCheckpoint(string checkpointID) {
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

    #region Utilities

    /// <summary>
    /// Checks if the Marble is moving to inform the score.<br/>
    /// Update time since last move to signal if the level has gone stale.
    /// </summary>
    private void UpdateMarbleMovement() {
      if (PlayableMarble == null) {
        this.LogWarning($"No PlayableMarble found on LevelManager {name}");
        return;
      }
      if (PlayableMarble.IsMoving) {
        _eventChannel.Raise(GameEventType.RaiseScore, Time.deltaTime);
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0f;
      }
    }

    /// <summary>
    /// Triggers the LevelComplete scene loading using the new LevelOrderManager.<br/>
    /// This now supports non-linear progression and intermediate scenes.
    /// </summary>
    //private void LoadLevelCompleteScene() {
      //if (_levelService.CurrentLevelConfig.HasLevelCompleteScene) {
      //  this.Log($"[LevelManager] Loading configured level complete scene: {_levelService.CurrentLevelConfig.LevelCompleteSceneName}");
      //  UnityEngine.SceneManagement.SceneManager.LoadScene(_levelService.CurrentLevelConfig.LevelCompleteSceneName);
      //} else {
      //  this.LogWarning($"[LevelManager] Current level config does not have a LevelCompleteScene configured.");
      //} 
    //}

    ///// <summary>
    ///// Get the next gameplay level scene name
    ///// </summary>
    //public string GetNextLevelScene() {
    //  if (_levelOrderManager == null) return null;

    //  string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    //  return _levelOrderManager.GetNextLevelScene(currentSceneName);
    //}

    ///// <summary>
    ///// Check if there's a next level available
    ///// </summary>
    //public bool HasNextLevel() {
    //  if (_levelOrderManager == null) return false;

    //  string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    //  return _levelOrderManager.HasNextLevel(currentSceneName);
    //}

    private void ResetTimers() {
      _timeSinceLevelStart = 0f;
      _timeSinceLastMove = 0f;
    }

    #endregion
  }

}