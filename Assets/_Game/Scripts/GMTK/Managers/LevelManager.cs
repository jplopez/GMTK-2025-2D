using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace GMTK {

  public class LevelManager : MonoBehaviour {

    public PlayableMarbelController PlayableMarble;

    [Header("Marble Settings")]
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
    [Tooltip("Event channel to notify score raising events.")]
    [SerializeField] private EventChannel scoreEvents;
    [Tooltip("Score multiplier for the level. Higher values yield higher scores. Score gets added during over time.")]
    public int ScoreMultiplier = 100;

    [Header("Winning Conditions")]
    [Tooltip("Number of checkpoints that must be reached to win the level. Currently not implemented.")]
    public int CheckpointsToWin = 1;
    [Tooltip("Number of checkpoints reached so far. Currently not implemented.")]
    public int CheckpointsReached = 0;
    [Tooltip("LevelSequence to determine next level's scene name")]
    public LevelSequence LevelSequence;

    public bool IsLevelStarted => _levelStarted;

    public bool IsLevelStale => _timeSinceLastMove >= StaleTimeThreshold;

    protected bool _levelStarted = false;
    protected bool _levelEnded = false;
    protected Vector2 _lastMarblePosition = Vector2.zero;
    protected float _timeSinceLastMove = 0f;
    protected float _timeSinceLevelStart = 0f;

    public static UnityEvent OnLevelReset;


    private void OnEnable() {
      Checkpoint.OnMarbleEnteringCheckpoint += HandleMarbleEnter;
      Checkpoint.OnMarbleExitingCheckpoint += HandleMarbleExit;
    }

    private void OnDisable() {
      Checkpoint.OnMarbleEnteringCheckpoint -= HandleMarbleEnter;
      Checkpoint.OnMarbleExitingCheckpoint -= HandleMarbleExit;
    }

    protected void HandleMarbleEnter(PlayableMarbelController marble, string checkpointID) {
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

    protected void HandleMarbleExit(PlayableMarbelController marble, string checkpointID) {
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
      EndLevelCheckpoint = EndLevelCheckpoint == null ? StartLevelCheckpoint : EndLevelCheckpoint;
      PlayableMarble.InitialForce = MarbleInitialForce;
      PlayableMarble.Spawn();

      InitializeTimers();
    }

    public void Update() {
      if (PlayableMarble == null) return;
      if (_levelEnded) CompleteLevel();
      if (_levelStarted) {
        UpdateTimers();
        //check if player is out of time 
        if (!InfitiniteTime && LevelMaxTime > 0f && _timeSinceLevelStart >= LevelMaxTime) {
          Debug.Log("[LevelManager] Level Time Expired! Restarting level.");
          ResetLevel();
        }
      }
    }

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
      scoreEvents.NotifySetInt(0);
      OnLevelReset?.Invoke();
    }

    private void CompleteLevel() {
      if(LevelSequence != null) {
        LevelSequence.SetCurrentScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("LevelComplete"); // Load a generic level complete scene
      }
    }


    private void InitializeTimers() {
      _timeSinceLevelStart = 0f;
      _timeSinceLastMove = 0f;
      _lastMarblePosition = PlayableMarble.Model.transform.position;
    }
    private void UpdateTimers() {

      _timeSinceLevelStart += Time.deltaTime;
      UpdateScore(Time.deltaTime);
      Vector2 currentMarblePosition = PlayableMarble.Model.transform.position;
      if (Vector2.Distance(currentMarblePosition, _lastMarblePosition) < 0.01f) {
        _timeSinceLastMove += Time.deltaTime;
      }
      else {
        _timeSinceLastMove = 0f;
        _lastMarblePosition = currentMarblePosition;
      }
    }

    private void UpdateScore(float deltaTime) {
      int deltaScore = Mathf.RoundToInt(ScoreMultiplier * deltaTime);
      scoreEvents.NotifyRaiseInt(deltaScore);
    }
  }

}