using Ameba;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GMTK {

  public class HudController : MonoBehaviour {

    [Header("Score")]
    [SerializeField] protected TMP_Text scoreText;
    public bool PauseScore = false;

    [Header("Playback")]
    public bool ShowPlaybackButtons = true;
    public bool DisablePlaybackButtons = false;
    public Button PlayButton;
    public Button ResetButton;
    public GameObject PlayFeedback;
    public GameObject ResetFeedback;

    [Header("Debug")]
    [Tooltip("Enable logging")]
    public bool EnableDebugLogging = false;

    protected ScoreGateKeeper _marbleScoreKeeper;
    protected GameEventChannel _eventsChannel;
    protected int _scoreAtLevelStart;

    // WebGL safety flags
    private bool _isInitialized = false;
    private bool _isScoreInitialized = false;

    //Public API for State Handlers
    public int ScoreAtLevelStart => _scoreAtLevelStart;
    public bool IsInitialized => _isInitialized;
    public bool IsScoreInitialized => _isScoreInitialized;

    #region Initialization
    private void Awake() {

      try {
        if (_marbleScoreKeeper == null) _marbleScoreKeeper = ServiceLocator.Get<ScoreGateKeeper>();
        if (_eventsChannel == null) _eventsChannel = ServiceLocator.Get<GameEventChannel>();

        // Validate critical components
        if (!ValidateComponents()) {
          Debug.LogError("[Hud] Critical components missing - falling back to safe mode");
          return;
        }

        SetupScoreSystem();
        SetupButtons();
        _isInitialized = true;

        if (EnableDebugLogging) Debug.Log("[Hud] Initialization complete");
      }
      catch (System.Exception ex) {
        Debug.LogError($"[Hud] Initialization failed: {ex.Message}");
      }
      Debug.Log($"Hud: OnReady {this}");
    }

    private bool ValidateComponents() {
      bool isValid = true;

      if (_eventsChannel == null) {
        LogError("GameEventChannel is null");
        isValid = false;
      }

      if (PlayButton == null || ResetButton == null) {
        LogError("Play or Reset button is null");
        isValid = false;
      }

      return isValid;
    }

    private void SetupScoreSystem() {
      if (_isScoreInitialized) return;
      if (_marbleScoreKeeper != null) {
        if (_marbleScoreKeeper.HasStrategy()) {
          _isScoreInitialized = true;
          return;
        }
        var scoreStrategy = gameObject.AddComponent<TimeBasedScoreCalculator>();
        _marbleScoreKeeper.SetStrategy(scoreStrategy, transform);
        _isScoreInitialized = true;
        UpdateScoreText(_marbleScoreKeeper.GetScore());
      }
      else {
        LogWarning($"Score keeper could not initialized. Will try again in next frame");
      }
    }

    private void SetupButtons() {
      if (PlayButton != null) {
        PlayButton.onClick.AddListener(HandlePlayButtonClick);
      }
      if (ResetButton != null) {
        ResetButton.onClick.AddListener(HandleResetButtonClick);
      }
    }

    #endregion

    #region Events Handling

    private void HandlePlayButtonClick() {
      Debug.Log(this);
      LogDebug("Play button clicked");
      if (_eventsChannel != null) {
        _eventsChannel.Raise(GameEventType.LevelPlay);
        LogDebug("LevelPlay event raised");
      }
      else {
        LogError("EventsChannel is null - cannot raise LevelPlay");
      }
    }

    private void HandleResetButtonClick() {
      LogDebug("Reset button clicked");
      if (_eventsChannel != null) {
        _eventsChannel.Raise(GameEventType.LevelReset);
        LogDebug("LevelReset event raised");
      }
      else {
        LogError("EventsChannel is null - cannot raise LevelReset");
      }
    }

    private void UpdateScoreText(int newScore) {
      if (scoreText != null) {
        scoreText.text = $"{newScore:D5}";
      }
    }

    #endregion

    #region Update loop and UI

    private void Update() {
      if (!_isInitialized) {
        LogDebug("failed to initialize. Will retry on next frame");
        return;
      }

      //try to setup score system if it wasn't done during start
      //this might happen on WebGL builds
      if (!_isScoreInitialized) SetupScoreSystem();
      if (_marbleScoreKeeper != null) {
        _marbleScoreKeeper.PauseScore(PauseScore);
        if (!PauseScore) {
          UpdateScoreText(_marbleScoreKeeper.GetScore());
        }
      }
    }

    public void UpdateUIFromGameState(GameStates gameState) {

      switch (gameState) {
        case GameStates.Preparation:
          PlayButton.enabled = true;
          PlayButton.interactable = true;
          ResetButton.enabled = false;
          ResetButton.interactable = false;
          PauseScore = true;
          // cache the score at level loading to restore in case of reset
          if (_marbleScoreKeeper != null) {
            _scoreAtLevelStart = _marbleScoreKeeper.GetScore();
          }
          // update score to level start, just for safety
          UpdateScoreText(_scoreAtLevelStart);
          break;

        case GameStates.Playing:
          PlayButton.enabled = false;
          PlayButton.interactable = false;
          ResetButton.enabled = true;
          ResetButton.interactable = true;
          PauseScore = false;
          break;

        case GameStates.Reset:
          PlayButton.enabled = true;
          PlayButton.interactable = true;
          ResetButton.enabled = true;
          ResetButton.interactable = true;
          PauseScore = true;

          // reseting makes player loose any score obtained during the run.
          // player score is set to value when level was loaded.
          if (_marbleScoreKeeper != null) {
            _marbleScoreKeeper.SetScore(_scoreAtLevelStart);
            UpdateScoreText(_scoreAtLevelStart);
          }
          //after updating UI for reset we signal we're ready to start the level again
          if (_eventsChannel != null) {
            _eventsChannel.Raise(GameEventType.LevelStart);
          }
          break;
        case GameStates.Pause:
        case GameStates.Options:
          PlayButton.enabled = false;
          PlayButton.interactable = false;
          ResetButton.enabled = false;
          ResetButton.interactable = false;
          PauseScore = true;
          break;
      }
    }

    #endregion

    #region Feedbacks API (not implemented yet)

    public void PlayLevelPlayFeedback() { }

    public void PlayLevelResetFeedback() { }

    #endregion


    private void OnDestroy() {
      if (PlayButton != null) {
        PlayButton.onClick.RemoveListener(HandlePlayButtonClick);
      }
      if (ResetButton != null) {
        ResetButton.onClick.RemoveListener(HandleResetButtonClick);
      }
    }

    private void LogDebug(string message) { if (EnableDebugLogging) Debug.Log($"[Hud] {message}"); }
    private void LogError(string message) { if (EnableDebugLogging) Debug.LogError($"[Hud] {message}"); }
    private void LogWarning(string message) { if (EnableDebugLogging) Debug.LogWarning($"[Hud] {message}"); }


  }
}
