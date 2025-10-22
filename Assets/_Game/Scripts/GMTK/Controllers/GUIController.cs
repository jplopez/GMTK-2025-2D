using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ameba;

namespace GMTK {

  /// <summary>
  /// GUI controller that handles HUD elements and tutorial boxes.
  /// </summary>
  public class GUIController : MonoBehaviour {

    [Header("Score")]
    [SerializeField] protected TMP_Text scoreText;
    public bool PauseScore = false;
    [Tooltip("How the score will be calculated: elapsed time, distance, etc.")]
    public ScoreCalculationStrategy ScoreStrategy;

    [Header("Playback")]
    public bool ShowPlaybackButtons = true;
    public bool DisablePlaybackButtons = false;
    public Button PlayButton;
    public Button ResetButton;
    public GameObject PlayFeedback;
    public GameObject ResetFeedback;

    [Header("Tutorial")]
    public Button TutorialButton;
    public GameObject TutorialFeedback;
    [Tooltip("List of tutorial boxes to toggle")]
    public List<Transform> tutorialBoxes = new();
    [Tooltip("Show tutorial boxes when level starts")]
    public bool ShowTutorialOnStart = false;

    [Header("Debug")]
    [Tooltip("Enable logging")]
    public bool EnableDebugLogging = false;

    // Score system
    protected ScoreGateKeeper _marbleScoreKeeper;
    protected GameEventChannel _eventsChannel;
    protected int _scoreAtLevelStart;

    // Tutorial state
    private bool _showingTutorialBoxes = false;

    // WebGL safety flags
    private bool _isInitialized = false;
    private bool _isScoreInitialized = false;

    //Public API for State Handlers
    public int ScoreAtLevelStart => _scoreAtLevelStart;
    public bool IsInitialized => _isInitialized;
    public bool IsScoreInitialized => _isScoreInitialized;

    #region Initialization

    private void Awake() {

      //the camera gets lost on canvas when the LevelGUI prefab is first added to the scene
      //this method ensures the main camera is assigned to the canvas
      EnsureCameraOnCanvas();
      
      try {
        if (_marbleScoreKeeper == null) _marbleScoreKeeper = ServiceLocator.Get<ScoreGateKeeper>();
        if (_eventsChannel == null) _eventsChannel = ServiceLocator.Get<GameEventChannel>();

        // Validate critical components
        if (!ValidateComponents()) {
          this.LogError("Critical components missing - falling back to safe mode");
          return;
        }

        SetupScoreSystem();
        SetupButtons();
        _isInitialized = true;

        this.Log("Initialization complete");
      }
      catch (System.Exception ex) {
        this.LogError($"Initialization failed: {ex.Message}");
      }
      this.Log($"OnReady {this}");
    }

    private void Start() {
      // Initialize tutorial state
      ToggleTutorialBoxes(ShowTutorialOnStart);
    }

    private bool ValidateComponents() {
      bool isValid = true;

      if (_eventsChannel == null) {
        this.LogError("GameEventChannel is null");
        isValid = false;
      }

      if (PlayButton == null || ResetButton == null) {
        this.LogError("Play or Reset button is null");
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
        if (ScoreStrategy == null) {
          ScoreStrategy = gameObject.AddComponent<TimeBasedScoreCalculator>(); //default strategy
          ScoreStrategy.gameObject.transform.parent = transform;
        }

        _marbleScoreKeeper.SetStrategy(ScoreStrategy, transform);
        _isScoreInitialized = true;
        UpdateScoreText(_marbleScoreKeeper.GetScore());
      }
      else {
        this.LogWarning($"Score keeper could not initialized. Will try again in next frame");
      }
    }

    private void SetupButtons() {
      if (PlayButton != null) {
        PlayButton.onClick.AddListener(HandlePlayButtonClick);
      }
      if (ResetButton != null) {
        ResetButton.onClick.AddListener(HandleResetButtonClick);
      }
      if (TutorialButton != null) {
        TutorialButton.onClick.AddListener(ToggleTutorial);
      }
    }
    private void EnsureCameraOnCanvas() {
      Canvas canvas = GetComponentInChildren<Canvas>();
      if (canvas != null && canvas.worldCamera == null) {
        Camera camera = Camera.main;
        if (camera != null) {
          canvas.worldCamera = camera;
        }
        else {
          this.LogWarning("No main camera found for Canvas");
        }
      }
    }


    #endregion

    #region Events Handling

    private void HandlePlayButtonClick() {
      Debug.Log(this);
      this.Log("Play button clicked");
      if (_eventsChannel != null) {
        _eventsChannel.Raise(GameEventType.LevelPlay);
        this.Log("LevelPlay event raised");
      }
      else {
        this.LogError("_eventChannel is null - cannot raise LevelPlay");
      }
    }

    private void HandleResetButtonClick() {
      this.Log("Reset button clicked");
      if (_eventsChannel != null) {
        _eventsChannel.Raise(GameEventType.LevelReset);
        this.Log("LevelReset event raised");
      }
      else {
        this.LogError("_eventChannel is null - cannot raise LevelReset");
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
        this.Log("failed to initialize. Will retry on next frame");
        return;
      }

      //try to setup score system if it wasn't done during start
      //this might happen on WebGL builds
      if (!_isScoreInitialized) SetupScoreSystem();
      if (_marbleScoreKeeper != null) {
        _marbleScoreKeeper.PauseScore(PauseScore);
        if (!PauseScore) {
          _marbleScoreKeeper.Tick(Time.deltaTime); // score is updated only when not paused
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
          // Show tutorial boxes if enabled on component
          ToggleTutorialBoxes(ShowTutorialOnStart);
          break;

        case GameStates.Playing:
          PlayButton.enabled = false;
          PlayButton.interactable = false;
          ResetButton.enabled = true;
          ResetButton.interactable = true;
          PauseScore = false;
          // hide tutorial boxes when gameplay starts
          ToggleTutorialBoxes(false);
          break;

        case GameStates.Reset:
          PlayButton.enabled = true;
          PlayButton.interactable = true;
          ResetButton.enabled = true;
          ResetButton.interactable = true;
          PauseScore = true;
          //hide tutorial boxes when gameplay starts
          ToggleTutorialBoxes(false);
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
          // hide tutorial boxes when pausing or entering options
          ToggleTutorialBoxes(false);
          break;
      }
    }

    #endregion

    #region Tutorial Functionality

    [ContextMenu("Toggle Tutorial Boxes")]
    public void ToggleTutorial() => ToggleTutorialBoxes(!_showingTutorialBoxes);

    public void ToggleTutorialBoxes(bool state) {
      foreach (var box in tutorialBoxes) {
        if (box != null) {
          box.gameObject.SetActive(state);
        }
      }
      _showingTutorialBoxes = state;
      this.Log($"Tutorial boxes toggled: {state}");
    }

    public bool IsTutorialVisible => _showingTutorialBoxes;

    #endregion

    #region Feedbacks API (not implemented yet)

    public void PlayLevelPlayFeedback() { }

    public void PlayLevelResetFeedback() { }

    public void PlayTutorialFeedback() { }

    #endregion

    #region Cleanup

    private void OnDestroy() {
      if (PlayButton != null) {
        PlayButton.onClick.RemoveListener(HandlePlayButtonClick);
      }
      if (ResetButton != null) {
        ResetButton.onClick.RemoveListener(HandleResetButtonClick);
      }
      if (TutorialButton != null) {
        TutorialButton.onClick.RemoveListener(ToggleTutorial);
      }
    }

    #endregion


  }
}