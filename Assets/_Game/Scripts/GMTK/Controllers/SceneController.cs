using System.Collections;
using UnityEngine;
using Ameba;

namespace GMTK {

  /// <summary>
  /// Scene-specific controller that configures the scene using LevelService data
  /// Can be extended for special scene types
  /// </summary>
  [AddComponentMenu("GMTK/Scenes/Scene Controller")]
  public class SceneController : MonoBehaviour {

    [Header("Scene Configuration")]
    [Tooltip("Scene name to use for level lookup (auto-detected if empty)")]
    public string SceneName;

    //[Header("Local Overrides")]
    [Tooltip("Override the initial game state from LevelService")]
    public bool OverrideInitialState = false;
    [SerializeField] private GameStates _overrideInitialState = GameStates.Preparation;

    [Tooltip("Override scene load behavior")]
    public bool OverrideLoadBehavior = false;
    [SerializeField] private float _overrideLoadDelay = 0f;

    [Header("Scene Events")]
    [Tooltip("Events to raise when scene loads")]
    public GameEventType[] OnSceneLoadEvents;

    [Header("Debug")]
    public bool EnableDebugLogging = false;

    // Services
    protected LevelService _levelService;
    protected GameEventChannel _eventChannel;
    protected GameStateMachine _stateMachine;

    // Current configuration
    protected LevelService.LevelConfig _levelConfig;

    private void Awake() {
      // Auto-detect scene name if not set
      if (string.IsNullOrEmpty(SceneName)) {
        SceneName = gameObject.scene.name;
      }

      LogDebug($"SceneController initializing for scene: {SceneName}");

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
      LoadLevelConfiguration();
    }

    private void Start() {
      // Initialize scene after all Awake calls are complete
      InitializeScene();
    }

    /// <summary>
    /// Load level configuration from LevelService
    /// </summary>
    protected virtual void LoadLevelConfiguration() {
      _levelConfig = _levelService.GetLevelConfig(SceneName);

      if (_levelConfig == null) {
        LogWarning($"No level configuration found for scene: {SceneName}");
        CreateDefaultConfiguration();
        return;
      }

      LogDebug($"Loaded configuration for {SceneName}: {_levelConfig.DisplayName}");

      //// Update LevelService current level
      //_levelService.SetCurrentLevel(SceneName);
    }

    /// <summary>
    /// Initialize the scene with loaded configuration
    /// </summary>
    protected virtual void InitializeScene() {
      LogDebug($"Initializing scene: {SceneName}");

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
      if (_levelConfig == null) return;

      LogDebug($"Applying configuration: InitialState={_levelConfig.InitialGameState}, Type={_levelConfig.Type}");

      // Setting up the current level based on the config type
      switch (_levelConfig.Type) {
        //Start scene always points to the first level
        case LevelService.SceneType.Start:
          var firstLevel = _levelService.GetLevelConfig(0).SceneName;
          _levelService.SetCurrentLevel(firstLevel);
          break;
        //End scene always points to the Scene marks as Start
        //This assumes there is one Start scene 
        case LevelService.SceneType.End:
          foreach (var level in _levelService.Levels) {
            if (level.Type == LevelService.SceneType.Start) {
              _levelService.SetCurrentLevel(level.SceneName);
              break;
            }
          }
          break;
        // Actual gameplay levels and special levels set themselves as current
        // this is the most common behaviour
        case LevelService.SceneType.Level: //actual gameplay levels
        case LevelService.SceneType.Special: //LevelDesigner
          _levelService.SetCurrentLevel(SceneName); break;
        // Transition levels ignore the next level, they 
        // always assume the next level is who called them.
        case LevelService.SceneType.Transition:
          _levelService.SetCurrentLevel(_levelConfig.PreviousSceneName); break;
      }
    }

    /// <summary>
    /// Set the initial game state for the scene
    /// </summary>
    protected virtual void SetInitialGameState() {
      GameStates targetState;

      // Use override if specified
      if (OverrideInitialState) {
        targetState = _overrideInitialState;
        LogDebug($"Using override initial state: {targetState}");
      }
      // Use configuration if available
      else if (_levelConfig != null && _levelConfig.SetStateOnLoad) {
        targetState = _levelConfig.InitialGameState;
        LogDebug($"Using configured initial state: {targetState}");
      }
      // Default fallback
      else {
        targetState = GameStates.Preparation;
        LogDebug($"Using default initial state: {targetState}");
      }

      // Apply delay if specified
      float delay = OverrideLoadBehavior ? _overrideLoadDelay : (_levelConfig?.LoadDelay ?? 0f);

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
    /// Create a default configuration when none is found
    /// </summary>
    protected virtual void CreateDefaultConfiguration() {
      LogDebug("Creating default level configuration");

      _levelConfig = new LevelService.LevelConfig {
        SceneName = SceneName,
        DisplayName = SceneName,
        Type = LevelService.SceneType.Level,
        InitialGameState = GameStates.Preparation,
        SetStateOnLoad = true,
        IsUnlocked = true
      };
    }

    /// <summary>
    /// Validate that all required services are available
    /// </summary>
    protected virtual bool ValidateServices() {
      return _levelService != null && _eventChannel != null && _stateMachine != null;
    }

    // Public API for scene management
    public LevelService.LevelConfig GetLevelConfig() => _levelConfig;
    public string GetSceneName() => SceneName;
    public LevelService.SceneType GetSceneType() => _levelConfig?.Type ?? LevelService.SceneType.Level;


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
      if (_levelConfig == null) {
        LoadLevelConfiguration();
      }
      if (_levelConfig == null) {
        LogError($"[SceneController:ReloadCurrentLevel] Can't read LevelConfig. Resolving to reload active Scene");
        ReloadCurrentScene();
        return;
      }
      else {
        if (_levelConfig.CanRestart) {
          UnityEngine.SceneManagement.SceneManager.LoadScene(_levelConfig.SceneName);
        }
        else {
          LogDebug($"[SceneController:ReloadCurrentLevel] LevelConfig for '{_levelConfig.SceneName}' does not allow this operation. Set CanRestart to 'true' to enable it");
          return;
        }
      }
    }

    public virtual void LoadNextLevel() {
      //sanity check of current level config
      if (_levelConfig == null) {
        LoadLevelConfiguration();
      }
      _levelConfig ??= _levelService.CurrentLevel;
      if (_levelConfig == null) {
        LogError($"[SceneController:LoadNextLevel] Can't obtain LevelConfig. Resolving to reload active Scene");
        ReloadCurrentScene();
        return;
      }

      //try get next level and load if is unlocked
      if (_levelService.TryGetNextLevel(out LevelService.LevelConfig nextLevel)) {
        if (nextLevel.IsUnlocked) {
          UnityEngine.SceneManagement.SceneManager.LoadScene(nextLevel.SceneName);
        }
        else {
          LogDebug($"[SceneController:LoadNextLevel] Next Level is not accessible. ");
        }
      }
      else {
        LogError($"[SceneController:LoadNextLevel] Current Level doesn't have next level. Will Reload to Start ");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start");
      }
    }

    public void QuitGame() {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    // Logging helpers
    protected void LogDebug(string message) {
      if (EnableDebugLogging) {
        Debug.Log($"[SceneController:{SceneName}] {message}");
      }
    }

    protected void LogWarning(string message) {
      Debug.LogWarning($"[SceneController:{SceneName}] {message}");
    }

    protected void LogError(string message) {
      Debug.LogError($"[SceneController:{SceneName}] {message}");
    }
  }
}
