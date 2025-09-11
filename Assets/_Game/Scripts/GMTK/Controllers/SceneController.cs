using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    public string SelectedConfigName;

    [Header("Level Configuration")]
    [Tooltip("Manual configuration for this scene (used if ConfigSource is Manual)")]
    [SerializeField] private LevelConfig _manualConfig = new();

    // Cached config
    [HideInInspector][SerializeField] protected LevelConfig _presetConfig;
    [HideInInspector][SerializeField] protected LevelConfig _effectiveConfig;

    [Header("On Scene Load")]
    [Tooltip("Events to raise when scene loads")]
    public GameEventType[] OnSceneLoadEvents;

    [Header("Loading")]
    [Tooltip("Optional loading screen prefab to instantiate during scene load delay")]
    public GameObject LoadingPrefab;
    [Tooltip("Parent transform for loading screen instance (defaults to this GameObject)")]
    public Transform LoadingParent;
    [Tooltip("Delay after scene load before hiding the loading screen (if any)")]
    public float LoadingHideDelay = 0.5f;

    [Header("Debug")]
    public bool EnableDebugLogging = false;

    //cached active scene name
    protected string _sceneName;

    // ServiceLocator
    protected LevelService _levelService;
    //protected LevelOrderManager _levelOrderManager;
    protected GameEventChannel _eventChannel;
    protected GameStateMachine _stateMachine;
    protected bool _isInitialized = false;
    protected List<ISceneConfigExtension> _configExtensions = new();

    //Loading prefab instance and showing status
    private GameObject _loadingInstance;
    public bool IsLoadingShowing => _loadingInstance != null && _loadingInstance.activeSelf;

    /// <summary>
    /// Whether the GameStateMachine will change state when the scene loads
    /// </summary>
    public bool ChangesGameStateOnLoad => _effectiveConfig != null && _effectiveConfig.SetStateOnLoad;

    /// <summary>
    /// The GameState to set when the scene loads, if ChangesGameStateOnLoad is true.
    /// If false, it will return current GameState or GameStates.Preparation if no state is set.
    /// </summary>
    public GameStates GetGameStateOnLoad() {
      if (_effectiveConfig != null) return _effectiveConfig.InitialGameState;
      if (_stateMachine != null) {
        return _stateMachine.Current;
      }
      else {
        return GameStates.Preparation;
      }
    }

    private void Awake() => Initialize();

    private void Start() {
      // Initialize scene after all Awake calls are complete
      InitializeScene();
    }

    #region Initialization

    protected virtual void Initialize() {
      if (_isInitialized) return;
      // Auto-detect scene name if not set
      _sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

      // Get services
      _levelService = ServiceLocator.Get<LevelService>();
      _eventChannel = ServiceLocator.Get<GameEventChannel>();
      _stateMachine = ServiceLocator.Get<GameStateMachine>();

      // Validate services
      if (!ValidateServices()) {
        this.LogError($"[SceneController] Failed to get required services for {_sceneName}");
        return;
      }

      // Load configuration
      LoadLevelConfig();
      // Load Config extensions (ISceneConfigExtension) 
      LoadConfigExtensions();

      InitializeLoading();

      _isInitialized = true;
    }

    /// <summary>
    /// Validate that all required services are available
    /// </summary>
    protected virtual bool ValidateServices() {
      return _levelService != null
              && _eventChannel != null
              && _stateMachine != null
              //&& _levelOrderManager != null
              ;
    }

    /// <summary>
    /// Load level configuration from LevelService
    /// </summary>
    protected virtual void LoadLevelConfig() {

      switch (ConfigurationSource) {
        case ConfigSource.Preset:
          if (string.IsNullOrEmpty(SelectedConfigName)) {
            SelectedConfigName = _presetConfig.ConfigName;
            this.LogWarning($"No SelectedConfigName set, defaulting to scene name: {SelectedConfigName}");
          }
          _presetConfig = _levelService.FindConfig(SelectedConfigName);
          break;
        case ConfigSource.Manual:
          //nothing to do, manual config is already set
          break;
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
      this.Log($"Found {_configExtensions.Count} ISceneConfigExtension components");
    }

    protected void InitializeLoading() {
      if (_loadingInstance != null) return;

      if (LoadingPrefab != null) {
        Transform parent = LoadingParent != null ? LoadingParent : this.transform;
        _loadingInstance = Instantiate(LoadingPrefab, parent);
        _loadingInstance.SetActive(false);
      }
    }

    /// <summary>
    /// Initialize the scene with loaded configuration
    /// </summary>
    protected virtual void InitializeScene() {
      this.Log($"Initializing scene: {_sceneName}");
      if (!_isInitialized) Initialize(); //try to initialize if Awake failed
      if (!_isInitialized) {
        this.LogError("Cant initialize scene due to missing services");
        QuitGame();
      }

      // Apply configuration
      ApplyLevelConfiguration();

      // Set initial game state
      SetInitialGameState();

      // Raise scene load events
      RaiseSceneLoadEvents();

      this.Log($"Scene initialization complete: {_sceneName}");
    }

    /// <summary>
    /// Apply the level configuration to the scene
    /// </summary>
    protected virtual void ApplyLevelConfiguration() {
      if (_effectiveConfig == null) return;

      foreach (var extension in _configExtensions) {
        if (extension as ISceneConfigExtension is not null) {
          try {
            extension.ApplyConfig(this);
            this.Log($"Applied configuration extension: {extension.GetType().Name}");
          }
          catch (Exception ex) {
            //errors are only logged, to avoid breaking the scene
            this.LogError($"Error applying configuration extension: {extension.GetType().Name}");
            this.LogError($"Exception: {ex}");
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
      this.Log($"Set delayed initial state: {state}");
    }

    #endregion

    /// <summary>
    /// Raise configured scene load events throug <see cref="GameEventChannel"/>
    /// </summary>
    protected virtual void RaiseSceneLoadEvents() {
      if (_effectiveConfig == null) return;

      foreach (var eventType in OnSceneLoadEvents) {
        _eventChannel.Raise(eventType);
        this.Log($"Raised scene load event: {eventType}");
      }
    }

    // Public API for scene management
    #region Public API

    public LevelConfig GetLevelConfig() => _effectiveConfig;

    public void SetLevelConfig(LevelConfig config) {
      if (config != null) {
        _effectiveConfig = config;
      }
    }

    /// <summary>
    /// Reloads the scene specified in <seealso cref="UnityEngine.SceneManagement.SceneManager.GetActiveScene()"/>.<br/>
    /// This is a safe operation that will always reload the current scene, regardless of LevelService state or LevelConfig settings. Useful for unity editor.
    /// </summary>
    [ContextMenu("Reload Current Scene")]
    public virtual void ReloadCurrentScene() {
      var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
      this.Log($"ReloadCurrentScene: Reloading scene '{scene}'");
      UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    /// <summary>
    /// Reloads the current playable Level, if the LevelConfig has 'CanRestart' true. 
    /// Current Level is looked up on <see cref="LevelService"/>.<br/>
    /// As fallback, this will call <see cref="ReloadCurrentScene"/>.
    /// </summary>
    [ContextMenu("Reload Current Level")]
    public virtual void ReloadCurrentLevel() {
      var currentPlayableLevel = _levelService.CurrentLevelSceneName;
      if (string.IsNullOrEmpty(currentPlayableLevel)) {
        this.Log($"ReloadCurrentLevel: current level is missing from LevelService. Falling back to 'ReloadCurrentScene'");
        ReloadCurrentScene();
      }
      // Load the determined playable level
      this.Log($"ReloadCurrentLevel: Reloading level '{currentPlayableLevel}'");
      UnityEngine.SceneManagement.SceneManager.LoadScene(currentPlayableLevel);
    }

    public string ComputeNextSceneName() {     
      var config = GetLevelConfig();
      var currentState = _stateMachine.Current;

      if(_levelService.TryComputeNextSceneName(_sceneName, config, currentState, out var nextScene)) {
        return nextScene;
      }
      return null;
    }

    /// <summary>
    /// Loads the next Scene, considering the rules defined on the LevelConfig and LevelService
    /// </summary>
    [ContextMenu("Load Next Scene")]
    public virtual void LoadNextScene() {
      if (_effectiveConfig == null) {
        LoadLevelConfig();
      }
      if (_effectiveConfig == null) {
        this.LogError($"LoadNextScene Can't obtain LevelConfig. Resolving to reload active Scene");
        ReloadCurrentScene();
        return;
      }

      string nextSceneToLoad = ComputeNextSceneName();

      // Load the determined scene
      if (string.IsNullOrEmpty(nextSceneToLoad)) {
        this.LogWarning($"LoadNextScene: Default Start scene is null or empty. Falling back to reload active Scene");
        ReloadCurrentScene();
      }
      else {
        this.Log($"LoadNextScene: Loading scene '{nextSceneToLoad}'");
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneToLoad);
      }
    }

    /// <summary>
    /// Signals the SceneController to advance to the next level in the LevelService.<br/>
    /// This method will call <see cref="LevelService.AdvanceToNextLevel"/> if there is a next level available.<br/>
    /// This method DOES NOT load the next level scene, it only updates the LevelService current level state.<br/> See <see cref="LoadNextScene"/> to load the next scene.
    /// </summary>
    [ContextMenu("Advance to Next Level")]
    public virtual void AdvanceToNextLevel() {
      if (_levelService == null) {
        this.LogError("LevelService is not available, cannot advance to next level");
        return;
      }

      if (_levelService.HasNextLevel()) {
        _levelService.AdvanceToNextLevel();
      }
      else {
        this.LogWarning("No next level available, Setting next level to Start");
        _levelService.MoveToFirstLevel();
      }
    }

    [ContextMenu("Load Start Scene")]
    public virtual void LoadStartScene() {

      if (_levelService == null) {
        this.LogError("LevelService is not available, cannot load Start scene");
        return;
      }
       _levelService.StartSceneName = string.IsNullOrEmpty(_levelService.StartSceneName) ? "Start" : _levelService.StartSceneName;
      this.Log($"Loading Start scene: '{_levelService.StartSceneName}'");
      UnityEngine.SceneManagement.SceneManager.LoadScene(_levelService.StartSceneName);
    }
    /// <summary>
    /// Check if there is a next level available
    /// </summary>
    public virtual bool HasNextLevel() {
      if (_levelService != null) {
        return _levelService.HasNextLevel();
      }
      return false;
    }

    protected virtual bool TryGetNextLevelConfig(out LevelConfig nextLevel) => _levelService.TryFindConfig(_sceneName, out nextLevel);

    public void QuitGame() {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    #endregion

  }
}
