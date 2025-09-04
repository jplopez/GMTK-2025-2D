using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ameba;


namespace GMTK {


  /// <summary>
  /// Scene-specific controller that configures the scene using LevelService data
  /// Can be extended for special scene types
  /// </summary>
  [AddComponentMenu("GMTK/Scenes/Scene Controller")]
  public class SceneController : MonoBehaviour {

    public enum ConfigSource { Preset, Manual }

    [Header("Scene Identification")]
    [Tooltip("Source of configuration for this scene")]
    public ConfigSource ConfigurationSource = ConfigSource.Preset;

    [Tooltip("Scene name to use for level lookup (auto-detected if empty)")]
    public string SceneName;

    [Header("Level Configuration")]
    [Tooltip("Manual configuration for this scene (used if ConfigSource is Manual)")]
    [SerializeField] private LevelConfig _manualConfig = new();

    // Cached config
    [HideInInspector]
    [SerializeField] protected LevelConfig _presetConfig;

    [HideInInspector]
    [SerializeField] protected LevelConfig _effectiveConfig;

    [Header("On Scene Load")]
    [Tooltip("Events to raise when scene loads")]
    public GameEventType[] OnSceneLoadEvents;
    [Tooltip("if true, the GameStateMachine scans for StateHandlers in the scene")]
    public bool AutoScanForHandlers = true;

    [Header("Debug")]
    public bool EnableDebugLogging = false;

    // Services
    protected LevelService _levelService;
    protected GameEventChannel _eventChannel;
    protected GameStateMachine _stateMachine;
    protected bool _isInitialized = false;
    protected List<ISceneConfigExtension> _configExtensions = new();

    public bool ChangesGameStateOnLoad {
      get {
        return _effectiveConfig != null && _effectiveConfig.SetStateOnLoad;
      }
    }

    public GameStates GetGameStateOnLoad() {
      return _effectiveConfig != null ? _effectiveConfig.InitialGameState : GameStates.Preparation;
    }

    private void Awake() => Initialize();

    protected virtual void Initialize() {
      if (_isInitialized) return;
      // Auto-detect scene name if not set
      if (string.IsNullOrEmpty(SceneName)) {
        SceneName = gameObject.scene.name;
      }

      // Get services
      _levelService = Services.Get<LevelService>();
      _eventChannel = Services.Get<GameEventChannel>();
      _stateMachine = Services.Get<GameStateMachine>();

      // Validate services
      if (!ValidateServices()) {
        Debug.LogError($"[SceneController] Failed to get required services for {SceneName}");
        return;
      }

      // Load configuration
      LoadLevelConfig();
      // Load Config extensions (ISceneConfigExtension) 
      LoadConfigExtensions();

      if (AutoScanForHandlers) {
        if (Services.TryGet<GameStateMachine>(out var handler)) {
          handler.AutoScanOnSceneLoad = AutoScanForHandlers;
          handler.DiscoverHandlers();
          LogDebug($"GameStateHandlers scanned {handler.HandlerCount} handlers ");
        }
      }
      _isInitialized = true;
    }

    private void Start() {
      // Initialize scene after all Awake calls are complete
      InitializeScene();
    }

    /// <summary>
    /// Load level configuration from LevelService
    /// </summary>
    protected virtual void LoadLevelConfig() {
      _presetConfig = _levelService.GetLevelConfig(SceneName);

      if (_presetConfig == null) {
        LogWarning($"No level configuration found for scene: {SceneName}");
        _presetConfig = new LevelConfig {
          SceneName = SceneName,
          DisplayName = SceneName,
          Type = SceneType.Level,
          InitialGameState = GameStates.Preparation,
          SetStateOnLoad = true,
        };
      }
      UpdateEffectiveConfig();
    }

    private void UpdateEffectiveConfig() {
      // Use preset or manual config as effective config
      _effectiveConfig = ConfigurationSource == ConfigSource.Preset
        ? _presetConfig
        : _manualConfig;
    }

    protected void LoadConfigExtensions() {
      _configExtensions = GetComponents<MonoBehaviour>().OfType<ISceneConfigExtension>().ToList();
      LogDebug($"Found {_configExtensions.Count} ISceneConfigExtension components");
    }

    /// <summary>
    /// Initialize the scene with loaded configuration
    /// </summary>
    protected virtual void InitializeScene() {
      LogDebug($"Initializing scene: {SceneName}");
      if (!_isInitialized) Initialize(); //try to initialize if Awake failed
      if (!_isInitialized) {
        Debug.LogError("Cant initialize scene due to missing services");
        QuitGame();
      }

      // Apply configuration
      ApplyLevelConfiguration();

      // Set initial game state
      SetInitialGameState();

      // Raise scene load events
      RaiseSceneLoadEvents();

      // Custom scene initialization
      OnSceneInitialized();

      LogDebug($"Scene initialization complete: {SceneName}");
    }

    /// <summary>
    /// Apply the level configuration to the scene
    /// </summary>
    protected virtual void ApplyLevelConfiguration() {
      if (_effectiveConfig == null) return;

      LogDebug($"Applying configuration: InitialState={_effectiveConfig.InitialGameState}, Type={_effectiveConfig.Type}");

      SetCurrentLevelBySceneType();

      ApplyConfigExtensions();
    }

    private void SetCurrentLevelBySceneType() {
      // Setting up the current level based on the config type
      switch (_effectiveConfig.Type) {
        //Start scene always points to the first level
        case SceneType.Start:
          var firstLevel = _levelService.GetLevelConfig(0).SceneName;
          _levelService.SetCurrentLevel(firstLevel);
          break;
        //End scene always points to the Scene marks as Start
        //This assumes there is one Start scene 
        case SceneType.End:
          foreach (var level in _levelService.Levels) {
            if (level.Type == SceneType.Start) {
              _levelService.SetCurrentLevel(level.SceneName);
              break;
            }
          }
          break;
        // Actual gameplay levels and special levels set themselves as current
        // this is the most common behaviour
        case SceneType.Level: //actual gameplay levels
        case SceneType.Special: //LevelDesigner
          _levelService.SetCurrentLevel(SceneName); break;
        // Transition levels ignore the next level, they 
        // always assume the next level is who called them.
        case SceneType.Transition:
          _levelService.SetCurrentLevel(_effectiveConfig.PreviousSceneName); break;
      }
    }

    private void ApplyConfigExtensions() {
      foreach (var extension in _configExtensions) {
        if (extension as ISceneConfigExtension is not null && extension.CanApplyOnType(GetSceneType())) {
          try {
            extension.ApplyConfig(this);
            LogDebug($"Applied configuration extension: {extension.GetType().Name}");
          }
          catch (Exception ex) {
            //errors are only logged, to avoid breaking the scene
            LogError($"Error applying configuration extension: {extension.GetType().Name}");
            LogError($"Exception: {ex}");
          }
        }
      }
    }

    /// <summary>
    /// Set the initial game state for the scene
    /// </summary>
    protected virtual void SetInitialGameState() {
      if (_effectiveConfig == null) return;

      GameStates targetState = _effectiveConfig.SetStateOnLoad
        ? _effectiveConfig.InitialGameState
        : GameStates.Preparation;

      float delay = _effectiveConfig.LoadDelay;

      if (delay > 0f) {
        StartCoroutine(SetInitialGameStateDelayed(targetState, delay));
      }
      else {
        _stateMachine.ChangeState(targetState);
      }
    }

    private IEnumerator SetInitialGameStateDelayed(GameStates state, float delay) {
      yield return new WaitForSeconds(delay);
      _stateMachine.ChangeState(state);
      LogDebug($"Set delayed initial state: {state}");
    }

    /// <summary>
    /// Raise configured scene load events
    /// </summary>
    protected virtual void RaiseSceneLoadEvents() {
      if (_effectiveConfig == null) return;

      foreach (var eventType in OnSceneLoadEvents) {
        _eventChannel.Raise(eventType);
        LogDebug($"Raised scene load event: {eventType}");
      }
    }

    /// <summary>
    /// Called after scene initialization is complete
    /// Override in derived classes for custom behavior
    /// </summary>
    protected virtual void OnSceneInitialized() {
      // Override in derived classes
    }

    /// <summary>
    /// Validate that all required services are available
    /// </summary>
    protected virtual bool ValidateServices() {
      return _levelService != null && _eventChannel != null && _stateMachine != null;
    }

    // Public API for scene management
    public LevelConfig GetLevelConfig() => _effectiveConfig;

    public void SetLevelConfig(LevelConfig config) {
      if (config != null) {
        _effectiveConfig = config;
      }
    }
    public string GetSceneName() => SceneName;
    public SceneType GetSceneType() => _effectiveConfig?.Type ?? SceneType.Level;

    /// <summary>
    /// Reload the scene specified in the SceneName field.
    /// If SceneName is null or empty, this method resolves to load the scene specified in <seealso cref="UnityEngine.SceneManagement.SceneManager.GetActiveScene()"/>.
    /// </summary>
    public virtual void ReloadCurrentScene() {
      if (string.IsNullOrEmpty(SceneName)) {
        SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LogError($"[SceneController] SceneName is empty or null. Resolving to SceneManager's active scene: {SceneName} ");
      }
      UnityEngine.SceneManagement.SceneManager.LoadScene(SceneName);
    }

    /// <summary>
    /// Reloads the Level specified in the Scene's LevelConfig.
    /// If LevelConfig is unavailable, this method resolves to <seealso cref="ReloadCurrentScene"/>
    /// </summary>
    public virtual void ReloadCurrentLevel() {
      if (_effectiveConfig == null) {
        LoadLevelConfig();
      }
      if (_effectiveConfig == null) {
        LogError($"[SceneController:ReloadCurrentLevel] Can't read LevelConfig. Resolving to reload active Scene");
        ReloadCurrentScene();
        return;
      }
      else {
        if (_effectiveConfig.CanRestart) {
          UnityEngine.SceneManagement.SceneManager.LoadScene(_effectiveConfig.SceneName);
        }
        else {
          LogDebug($"[SceneController:ReloadCurrentLevel] LevelConfig for '{_effectiveConfig.SceneName}' does not allow this operation. Set CanRestart to 'true' to enable it");
          return;
        }
      }
    }

    public virtual void LoadNextLevel() {
      if (_effectiveConfig == null) {
        LoadLevelConfig();
      }
      if (_effectiveConfig == null) {
        LogError($"[SceneController:LoadNextLevel] Can't obtain LevelConfig. Resolving to reload active Scene");
        ReloadCurrentScene();
        return;
      }

      // Try get next level and load if is unlocked
      var nextLevel = _levelService.GetLevelConfig(_effectiveConfig.NextSceneName);
      if (nextLevel != null) {
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevel.SceneName);
      }
      else {
        LogError($"[SceneController:LoadNextLevel] Next Level is not accessible or not found. Will Reload to Start ");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start");
      }
    }

    protected virtual bool TryGetNextLevelConfig(out LevelConfig nextLevel) => _levelService.TryGetNextLevel(out nextLevel);

    public void QuitGame() {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    // Logging helpers
    protected void LogDebug(string message) { if (EnableDebugLogging) Debug.Log($"[SceneController:{SceneName}] {message}"); }
    protected void LogWarning(string message) => Debug.LogWarning($"[SceneController:{SceneName}] {message}");
    protected void LogError(string message) => Debug.LogError($"[SceneController:{SceneName}] {message}");

  }
}
