using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Ameba;

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

    [Header("WebGL Debug")]
    [Tooltip("Enable WebGL-specific logging")]
    public bool EnableWebGLDebug = false;

    protected HUD _hud;
    protected ScoreGateKeeper _marbleScoreKeeper;
    protected GameEventChannel _eventsChannel;
    protected int _scoreAtLevelStart;

    // WebGL safety flags
    private bool _isInitialized = false;
    private bool _webglSafeMode = false;

    private void Awake() {
      // Detect WebGL and enable safe mode
#if UNITY_WEBGL && !UNITY_EDITOR
            _webglSafeMode = true;
            if (EnableWebGLDebug) Debug.Log("[HudController] WebGL safe mode enabled");
#endif

      InitializationManager.WaitForInitialization(this, OnReady);
    }

    private void OnReady() {
      try {
        if (_hud == null) _hud = Services.Get<HUD>();
        if (_marbleScoreKeeper == null) _marbleScoreKeeper = Services.Get<ScoreGateKeeper>();
        if (_eventsChannel == null) _eventsChannel = Services.Get<GameEventChannel>();

        // Validate critical components
        if (!ValidateComponents()) {
          Debug.LogError("[HudController] Critical components missing - falling back to safe mode");
          _webglSafeMode = true;
          return;
        }

        SetupScoreSystem();
        SetupButtons();
        _isInitialized = true;

        if (EnableWebGLDebug) Debug.Log("[HudController] Initialization complete");
      }
      catch (System.Exception ex) {
        Debug.LogError($"[HudController] Initialization failed: {ex.Message}");
        _webglSafeMode = true;
      }
      Debug.Log($"HudController: OnReady {this}");
    }

    private bool ValidateComponents() {
      bool isValid = true;

      if (_eventsChannel == null) {
        Debug.LogError("[HudController] GameEventChannel is null");
        isValid = false;
      }

      //if (Game.Context == null) {
      //  Debug.LogError("[HudController] Game.Context is null");
      //  isValid = false;
      //}

      if (PlayButton == null || ResetButton == null) {
        Debug.LogError("[HudController] Play or Reset button is null");
        isValid = false;
      }

      return isValid;
    }

    private void SetupScoreSystem() {
      if (_marbleScoreKeeper != null) {
        var scoreStrategy = gameObject.AddComponent<TimeBasedScoreCalculator>();
        _marbleScoreKeeper.SetStrategy(scoreStrategy, transform);
        UpdateScoreText(_marbleScoreKeeper.GetScore());
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

    private void OnDestroy() {
      if (PlayButton != null) {
        PlayButton.onClick.RemoveListener(HandlePlayButtonClick);
      }
      if (ResetButton != null) {
        ResetButton.onClick.RemoveListener(HandleResetButtonClick);
      }
    }

    private void Update() {
      if (!_isInitialized || _webglSafeMode) {
        if (EnableWebGLDebug) Debug.Log("HudController: failed to initialize. Will retry on next frame");
        return;
      }
      
      if (_marbleScoreKeeper != null) {
        _marbleScoreKeeper.PauseScore(PauseScore);
        if (!PauseScore) {
          UpdateScoreText(_marbleScoreKeeper.GetScore());
        }
      }
    }

    public void UpdateUIFromGameState(GameStates gameState) {
      // Implementation remains the same
      switch (gameState) {
        case GameStates.Preparation:
          PlayButton.enabled = true;
          PlayButton.interactable = true;
          ResetButton.enabled = false;
          ResetButton.interactable = false;
          PauseScore = true;
          if (_marbleScoreKeeper != null) {
            _scoreAtLevelStart = _marbleScoreKeeper.GetScore();
          }
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
          if (_marbleScoreKeeper != null) {
            _marbleScoreKeeper.SetScore(_scoreAtLevelStart);
            UpdateScoreText(_scoreAtLevelStart);
          }
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

    private void HandlePlayButtonClick() {
      Debug.Log(this);
      if (EnableWebGLDebug) Debug.Log("[HudController] Play button clicked");
      if (_eventsChannel != null) {
        _eventsChannel.Raise(GameEventType.LevelPlay);
        if (EnableWebGLDebug) Debug.Log("[HudController] LevelPlay event raised");
      }
      else {
        Debug.LogError("[HudController] EventsChannel is null - cannot raise LevelPlay");
      }
      //try {
      //  // WebGL-safe transition checking
      //  if (CanTransitionToPlayingSafely()) {

      //  }
      //  else {
      //    if (EnableWebGLDebug) Debug.Log("[HudController] Cannot transition to Playing state");
      //  }
      //}
      //catch (System.Exception ex) {
      //  Debug.LogError($"[HudController] Play button click failed: {ex.Message}");
      //  // Fallback: try to raise event directly
      //  TryRaiseEventDirectly(GameEventType.LevelPlay);
      //}
    }

    private void HandleResetButtonClick() {
      if (EnableWebGLDebug) Debug.Log("[HudController] Reset button clicked");
      if (_eventsChannel != null) {
        _eventsChannel.Raise(GameEventType.LevelReset);
        if (EnableWebGLDebug) Debug.Log("[HudController] LevelReset event raised");
      }
      else {
        Debug.LogError("[HudController] EventsChannel is null - cannot raise LevelReset");
      }
      //try {
      //  // WebGL-safe transition checking
      //  if (CanTransitionToResetSafely()) {

      //  }
      //  else {
      //    if (EnableWebGLDebug) Debug.Log("[HudController] Cannot transition to Reset state");
      //  }
      //}
      //catch (System.Exception ex) {
      //  Debug.LogError($"[HudController] Reset button click failed: {ex.Message}");
      //  // Fallback: try to raise event directly
      //  TryRaiseEventDirectly(GameEventType.LevelReset);
      //}
    }

    /// <summary>
    /// WebGL-safe method to check if we can transition to Playing state
    /// </summary>
    //private bool CanTransitionToPlayingSafely() {
    //  try {
    //    // Direct null checks first
    //    if (Game.Context == null) {
    //      if (EnableWebGLDebug) Debug.Log("[HudController] Game.Context is null");
    //      return _webglSafeMode; // In WebGL safe mode, allow transitions
    //    }

    //    if (Game.StateMachine == null) {
    //      if (EnableWebGLDebug) Debug.Log("[HudController] Game.StateMachine is null");
    //      return _webglSafeMode;
    //    }

    //    // Try the normal transition check
    //    return Game.Context.CanTransitionTo(GameStates.Playing);
    //  }
    //  catch (System.Exception ex) {
    //    if (EnableWebGLDebug) Debug.LogWarning($"[HudController] Transition check failed: {ex.Message}");
    //    return _webglSafeMode; // In WebGL, allow transition if we're in safe mode
    //  }
    //}

    ///// <summary>
    ///// WebGL-safe method to check if we can transition to Reset state
    ///// </summary>
    //private bool CanTransitionToResetSafely() {
    //  try {
    //    if (Game.Context == null || Game.StateMachine == null) {
    //      return _webglSafeMode;
    //    }
    //    return Game.Context.CanTransitionTo(GameStates.Reset);
    //  }
    //  catch (System.Exception ex) {
    //    if (EnableWebGLDebug) Debug.LogWarning($"[HudController] Reset transition check failed: {ex.Message}");
    //    return _webglSafeMode;
    //  }
    //}

    ///// <summary>
    ///// Fallback method to raise events directly when interface calls fail
    ///// </summary>
    //private void TryRaiseEventDirectly(GameEventType eventType) {
    //  try {
    //    if (_eventsChannel != null) {
    //      _eventsChannel.Raise(eventType);
    //      Debug.Log($"[HudController] Direct event raise successful: {eventType}");
    //    }
    //    else {
    //      Debug.LogError("[HudController] Cannot raise event - EventsChannel is null");
    //    }
    //  }
    //  catch (System.Exception ex) {
    //    Debug.LogError($"[HudController] Direct event raise failed: {ex.Message}");
    //  }
    //}

    private void UpdateScoreText(int newScore) {
      if (scoreText != null) {
        scoreText.text = $"{newScore:D5}";
      }
    }
  }
}
