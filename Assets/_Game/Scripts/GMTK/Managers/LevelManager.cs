using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GMTK {

  public class LevelManager : MonoBehaviour {

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

    //Main Game controller 
    protected GameContext _controller;

    public bool IsLevelStarted => _levelStarted;
    public bool IsLevelStale => _timeSinceLastMove >= StaleTimeThreshold;

    protected bool _levelStarted = false;
    protected bool _levelEnded = false;
    //protected Vector2 _lastMarblePosition = Vector2.zero;
    protected float _timeSinceLastMove = 0f;
    protected float _timeSinceLevelStart = 0f;

    protected LevelSequence _levelSequence;
    protected GameEventChannel _eventChannel;
    //public static UnityEvent LevelReset;


    private void OnEnable() {
      Checkpoint.OnMarbleEnteringCheckpoint += HandleMarbleEnter;
      Checkpoint.OnMarbleExitingCheckpoint += HandleMarbleExit;
    }

    private void OnDisable() {
      Checkpoint.OnMarbleEnteringCheckpoint -= HandleMarbleEnter;
      Checkpoint.OnMarbleExitingCheckpoint -= HandleMarbleExit;
    }

    private void Awake() {
      if (_controller == null) {
        _controller = FindAnyObjectByType<GameContext>();
        _eventChannel = _controller.EventsChannel;
        _levelSequence = _controller.LevelSequence;
      }
      if (_eventChannel == null) {
        _eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }
      _eventChannel.AddListener(GameEventType.LevelReset, ResetLevel);
      _eventChannel.AddListener(GameEventType.LevelPlay, StartLevel);
    }

    private void OnDestroy() {
      _eventChannel.RemoveListener(GameEventType.LevelReset, ResetLevel);
      _eventChannel.RemoveListener(GameEventType.LevelPlay, StartLevel);
    }

    protected void HandleMarbleEnter(PlayableMarbleController marble, string checkpointID) {
      if (marble == null || string.IsNullOrEmpty(checkpointID)) {
        Debug.LogWarning("[LevelManager] Marble or Checkpoint is null in HandleMarbleEnter.");
        return;
      }
      // mark level as complete when marble enters end checkpoint
      if (EndLevelCheckpoint.ID.Equals(checkpointID) && _levelStarted && !_levelEnded) {
        Debug.Log($"[LevelManager] Marble entered end checkpoint {EndLevelCheckpoint.ID}. Ending level.");
        EndLevel();
      }
    }

    protected void HandleMarbleExit(PlayableMarbleController marble, string checkpointID) {
      if (marble == null || string.IsNullOrEmpty(checkpointID)) {
        Debug.LogWarning("[LevelManager] Marble or Checkpoint is null in HandleMarbleEnter.");
        return;
      }

      // start level when marble exists start checkpoint
      if (StartLevelCheckpoint.ID.Equals(checkpointID) && !_levelStarted) {
        Debug.Log($"[LevelManager] Marble entered start checkpoint {StartLevelCheckpoint.ID}. Starting level.");
        StartLevel();
      }
    }

    public void Start() {
      if (PlayableMarble == null) {
        Debug.LogWarning("[LevelManager] PlayableMarble is not assigned in LevelManager.");
        return;
      }
      if (StartLevelCheckpoint == null) {
        Debug.LogWarning("[LevelManager] StartLevelCheckpoint is not assigned in LevelManager.");
        return;
      }
      PlayableMarble.Model.transform.position = StartLevelCheckpoint.Position;
      if (PlayableMarble.SpawnTransform == null) PlayableMarble.SpawnTransform = StartLevelCheckpoint.transform;
      EndLevelCheckpoint = EndLevelCheckpoint == null ? StartLevelCheckpoint : EndLevelCheckpoint;
      PlayableMarble.InitialForce = MarbleInitialForce;
      PlayableMarble.Spawn();

      InitializeTimers();
    }

    public void Update() {
      if (_levelEnded) CompleteLevel();
      if (_levelStarted) {
        _timeSinceLevelStart += Time.deltaTime;
        //UpdateTimers();
        //check if player is out of time 
        if (!InfitiniteTime && LevelMaxTime > 0f && _timeSinceLevelStart >= LevelMaxTime) {
          Debug.Log("[LevelManager] Level Time Expired! Restarting level.");
          ResetLevel();
        }
        CheckMarbleMovement();
      }
    }

    private void CheckMarbleMovement() {
      if (PlayableMarble == null) {
        Debug.LogWarning($"No PlayableMarble found on LevelManager {name}");
        return;
      }
      if (PlayableMarble.IsMoving) {
        //int deltaScore = CalculateDeltaScore(Time.deltaTime);
        //Debug.Log($"Adding {deltaScore} to Marble's Score");
        _eventChannel.Raise(GameEventType.ScoreRaised, Time.deltaTime);
        _timeSinceLastMove += Time.deltaTime;
      } else {
        _timeSinceLastMove = 0f;
      }
    }

    //private int CalculateDeltaScore(float seed) {
    //  //return Mathf.RoundToInt(Mathf.Clamp(seed * ScoreMultiplier,0,10));
    //  return (int)seed * 1000;
    //}

    public void StartLevel() {
      InitializeTimers();
      if (PlayableMarble != null) {
        PlayableMarble.Launch();
      }
      else {
        Debug.LogWarning("[LevelManager] PlayableMarble is not assigned in LevelManager.");
      }
      _levelStarted = true;
      _levelEnded = false;
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
      if (PlayableMarble != null) {
        PlayableMarble.StopMarble();
      }
      Debug.Log($"LevelEnded? {_levelEnded}");
    }

    public void ResetLevel() {
      PlayableMarble.Model.transform.position = StartLevelCheckpoint.Position;
      PlayableMarble.Spawn();
      PlayableMarble.InitialForce = MarbleInitialForce;
      _levelStarted = false;
      _levelEnded = false;
      InitializeTimers();
      _eventChannel.Raise(GameEventType.ScoreChanged, 0);
    }

    private void CompleteLevel() {
      if(_levelSequence != null) {
        _levelSequence.SetCurrentScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelComplete"); // Load a generic level complete scene
      }
    }

    private void InitializeTimers() {
      _timeSinceLevelStart = 0f;
      _timeSinceLastMove = 0f;
    }
    
  }

}