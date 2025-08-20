using System;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK {
  public class LevelManager : MonoBehaviour {

    private const string LEVEL_COMPLETE_SCENE_NAME = "LevelComplete";

    [Header("Marble Settings")]
    [Tooltip("Reference to the Marble prefab")]
    public PlayableMarbleController PlayableMarble;
    [Tooltip("Initial force applied to the marble when the level starts.")]
    public Vector2 MarbleInitialForce = Vector2.zero;

    [Header("Level Checkpoints")]
    [Tooltip("Transform representing the start checkpoint of the level.")]
    public Checkpoint StartLevelCheckpoint;
    [Tooltip("Optional. Transform representing the end checkpoint of the level. If missing, it will assume StartLevelCheckpoint")]
    public Checkpoint EndLevelCheckpoint;

    [Header("Level Stale Settings")]
    [Tooltip("Time in seconds after which the level is considered stale if the marble hasn't moved.")]
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

    [Tooltip("LevelExtensions to encapsulate specific behaviours, that might not be needed everywhere")]
    public List<LevelExtension> Extensions = new();

    public bool IsLevelStarted => _levelStarted;
    public bool IsLevelStale => _timeSinceLastMove >= StaleTimeThreshold;

    protected bool _levelStarted = false;
    protected bool _levelEnded = false;
    protected float _timeSinceLastMove = 0f;
    protected float _timeSinceLevelStart = 0f;
    protected int _scoreAtLevelStart = 0;
    protected LevelSequence _levelSequence;
    protected GameEventChannel _eventChannel;

    #region MonoBehaviour methods

    private void Awake() {
      _eventChannel = Game.Context.EventsChannel;
      _levelSequence = Game.Context.LevelSequence;
      _scoreAtLevelStart = Game.Context.MarbleScoreKeeper.GetScore();
      if (_eventChannel == null) {
        Debug.Log($"LevelManager: EventChannel is missing. LevelManager won't be able to handle game events");
        return;
      }
      _eventChannel.AddListener(GameEventType.EnterCheckpoint, HandleCheckPointEvent);
      //_eventChannel.AddListener(GameEventType.ExitCheckpoint, HandleCheckPointEvent);
    }

    private void OnDestroy() {
      _eventChannel.RemoveListener(GameEventType.EnterCheckpoint, HandleCheckPointEvent);
      //_eventChannel.RemoveListener(GameEventType.ExitCheckpoint, HandleCheckPointEvent);
    }

    public void Start() {
      if (StartLevelCheckpoint == null) {
        Debug.LogWarning("[LevelManager] StartLevelCheckpoint is not assigned in LevelManager.");
        return;
      }
      StartMarble();
      ResetTimers();
    }

    private void StartMarble() {
      if (PlayableMarble == null) {
        Debug.LogWarning("[LevelManager] PlayableMarble is not assigned in LevelManager.");
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
      if (_levelEnded) LoadCompleteLevelScene();
      if (_levelStarted) {
        _timeSinceLevelStart += Time.deltaTime;
        //check if player is out of time 
        if (!InfitiniteTime && LevelMaxTime > 0f && _timeSinceLevelStart >= LevelMaxTime) {
          Debug.Log("[LevelManager] Level Time Expired! Restarting level.");
          ResetLevel();
        }
        UpdateMarbleMovement();
      }
    }

    #endregion

    #region Checkpoint Handler Wrapper EventArgs -> MarbleEventArgs

    private void HandleCheckPointEvent(EventArgs eventArgs) {
      if (eventArgs is MarbleEventArgs marbleEventArgs) {
        if (string.IsNullOrEmpty(marbleEventArgs.HitCheckpoint.ID)) {
          Debug.LogWarning($"[LevelManager] Can't resolve Checkpoint event {marbleEventArgs.EventType} because Checkpoint ID is null or empty");
          return;
        }

        if (marbleEventArgs.EventType == GameEventType.EnterCheckpoint) {
          HandleEnterCheckpoint(marbleEventArgs.HitCheckpoint.ID);
        }
        else if (marbleEventArgs.EventType == GameEventType.ExitCheckpoint) {
          Debug.Log("LevelManager doesn't handle ExitCheckpoint events"); return;
          //HandleExitCheckpoint(marbleEventArgs.HitCheckpoint.ID);
        }
      }
    }

    #endregion


    #region Marble Event Handlers

    protected void HandleEnterCheckpoint(string checkpointID) {
      // mark level as complete when marble enters end checkpoint
      if (EndLevelCheckpoint.ID.Equals(checkpointID) && _levelStarted && !_levelEnded) {
        Debug.Log($"[LevelManager] Marble entered end checkpoint {EndLevelCheckpoint.ID}. Ending level.");
        _eventChannel.Raise(GameEventType.LevelObjectiveCompleted);
        //EndLevel();
      }
    }

    //protected void HandleExitCheckpoint(string checkpointID) {
    //  // start level when marble exists start checkpoint
    //  if (StartLevelCheckpoint.ID.Equals(checkpointID) && !_levelStarted) {
    //    Debug.Log($"[LevelManager] Marble entered start checkpoint {StartLevelCheckpoint.ID}. Starting level.");
    //    Game.Context.EventsChannel.Raise(GameEventType.LevelStart);
    //    //StartLevel();
    //  }
    //}

    #endregion

    #region Public API for GameState changes

    public void StartLevel() {
      ResetTimers();
      //if (PlayableMarble != null) {
      //  PlayableMarble.Launch();
      //}
      //else {
      //  Debug.LogWarning("[LevelManager] PlayableMarble is not assigned in LevelManager.");
      //}
      _levelStarted = true;
      _levelEnded = false;
      StartLevelCheckpoint.enabled = false;
      EndLevelCheckpoint.enabled = true;
      Debug.Log($"LevelStarted? {_levelStarted}");
    }

    public void EndLevel() {
      if (!_levelStarted) {
        Debug.LogWarning("[LevelManager] Level has not started yet!");
        return;
      }
      if (_levelEnded) {
        Debug.LogWarning("[LevelManager] Level has already ended!");
        return;
      }
      _levelStarted = false;
      _levelEnded = true;
      //if (PlayableMarble != null) {
      //  PlayableMarble.StopMarble();
      //}
      Debug.Log($"LevelEnded? {_levelEnded}");
    }

    /// <summary>
    /// Repositions Marble in StartLevelCheckpoint, reset timers, 
    /// resets score to the value at the start of the level
    /// </summary>
    public void ResetLevel() {
      //PlayableMarble.Model.transform.position = StartLevelCheckpoint.Position;
      //PlayableMarble.Spawn();
      //PlayableMarble.InitialForce = MarbleInitialForce;
      _levelStarted = false;
      _levelEnded = false;
      StartLevelCheckpoint.enabled = true;
      EndLevelCheckpoint.enabled = false;
      ResetTimers();
      _eventChannel.Raise(GameEventType.ScoreChanged, _scoreAtLevelStart);
    }

    #endregion

    /// <summary>
    /// Checks if the marble is moving to inform the score.<br/>
    /// Update time since last move to signal if the level has gone stale.
    /// </summary>
    private void UpdateMarbleMovement() {
      if (PlayableMarble == null) {
        Debug.LogWarning($"No PlayableMarble found on LevelManager {name}");
        return;
      }
      if (PlayableMarble.IsMoving) {
        _eventChannel.Raise(GameEventType.ScoreRaised, Time.deltaTime);
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0f;
      }
    }

    /// <summary>
    /// Triggers the LevelComplete scene loading.<br/>
    /// TODO: turn this into event-driven and async scene loading to measure percentage of progress.
    /// </summary>
    private void LoadCompleteLevelScene() {
      var levelSequence = Game.Context.LevelSequence;
      levelSequence.SetCurrentScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
      UnityEngine.SceneManagement.SceneManager.LoadScene(LEVEL_COMPLETE_SCENE_NAME); // Load a generic level complete scene
    }
    private void ResetTimers() {
      _timeSinceLevelStart = 0f;
      _timeSinceLastMove = 0f;
    }

  }

}