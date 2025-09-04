using Ameba;

using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This Config component initializes the <see cref="Services"/> class 
  /// and wires the events for GameState changes
  /// </summary>
  [AddComponentMenu("GMTK/Scene Configurations/Services Initialization")]
  public class ServicesInitConfig : MonoBehaviour, ISceneConfigExtension {

    [Tooltip("If true, this component initialize all services, regardless if they were initialized before")]
    public bool ForceReinitialize = false;

    public bool EnableDebugLogging = true;

    //private SceneController controller;
    //private bool eventListenersInitialized = false;

    private void Awake() => InitializeAllServices();

    public void ApplyConfig(SceneController controller) {
      InitializeGameStates();
    }

    public bool CanApplyOnType(SceneType sceneType) => true;

    private void InitializeAllServices() {
      if (!Services.IsInitialized || ForceReinitialize) {
        LogDebug($"Initializing services");
        Services.Clear();
        if (Services.TryInitialize()) {
          LogDebug($"Services initialized successfully");
        }
        else {
          LogError($"Services cannot be initialized");
        }
      }
    }

    /// <summary>
    /// Initializes the GameStateMachine and wires the default event listeners.
    /// GameStateMachine depends on GameEventChannel, so this method needs to run after all services have been loaded. 
    /// </summary>
    private void InitializeGameStates() {

      if (!Services.IsInitialized) return;

      if(Services.TryGet<GameEventChannel>(out var eventChannel)) {
        LogDebug("GameEventChannel service found");
        if (Services.TryGet<GameStateMachine>(out var stateMachine)) {
          stateMachine.SetEventChannelAndInitialize(eventChannel);
          LogDebug("GameStateMachine initialized successfully");
        } else {
          LogError("GameStateMachine service not found");
        }
      }
      else {
        LogError("GameEventChannel service not found");
      }

    }
    //private void WireDefaultEventListeners() {
    //  LogDebug($"Wiring default event listeners for Scene {controller.SceneName}");
    //  if (eventListenersInitialized || !ForceReinitialize) return;

    //  var _eventChannel = Services.Get<GameEventChannel>();
    //  var stateMachine = Services.Get<GameStateMachine>();
    //  var handlerRegistry = Services.Get<GameStateHandlerRegistry>();

    //  if (_eventChannel == null || stateMachine == null) {
    //    LogError($"Cannot wire listeners - missing core services for Scene {controller.SceneName}");
    //    return;
    //  }

    //  //Centralized Handler for GameState changes
    //  stateMachine.RemoveListener(handlerRegistry.HandleStateChange);
    //  stateMachine.AddListener(handlerRegistry.HandleStateChange);
      
    //  // remove the GameState listener to ensure there are no duplicates
    //  CleanGameStateListeners(_eventChannel, stateMachine);
    //  AddGameStateListeners(_eventChannel, stateMachine);

    //  eventListenersInitialized = true;
    //  LogDebug("Default event listeners wired successfully");
    //}

    //private void AddGameStateListeners(GameEventChannel _eventChannel, GameStateMachine stateMachine) {
    //  //Add here any GameEvent that should trigger a GameStateChange
    //  _eventChannel.AddListener(GameEventType.GameStarted, stateMachine.HandleStartGame);
    //  _eventChannel.AddListener(GameEventType.LevelStart, stateMachine.HandleLevelStart);
    //  _eventChannel.AddListener(GameEventType.LevelPlay, stateMachine.HandleLevelPlay);
    //  _eventChannel.AddListener(GameEventType.LevelReset, stateMachine.HandleLevelReset);
    //  _eventChannel.AddListener(GameEventType.LevelObjectiveCompleted, stateMachine.HandleLevelComplete);
    //  _eventChannel.AddListener(GameEventType.GameOver, stateMachine.HandleGameOver);
    //  _eventChannel.AddListener(GameEventType.EnterOptions, stateMachine.HandleEnterOptions);
    //  _eventChannel.AddListener(GameEventType.ExitOptions, stateMachine.HandleEnterOptions);
    //  _eventChannel.AddListener(GameEventType.EnterPause, stateMachine.HandleEnterPause);
    //  _eventChannel.AddListener(GameEventType.ExitPause, stateMachine.HandleExitPause);
    //}

    //private void CleanGameStateListeners(GameEventChannel _eventChannel, GameStateMachine stateMachine) {

    //  _eventChannel.RemoveListener(GameEventType.GameStarted, stateMachine.HandleStartGame);
    //  _eventChannel.RemoveListener(GameEventType.LevelStart, stateMachine.HandleLevelStart);
    //  _eventChannel.RemoveListener(GameEventType.LevelPlay, stateMachine.HandleLevelPlay);
    //  _eventChannel.RemoveListener(GameEventType.LevelReset, stateMachine.HandleLevelReset);
    //  _eventChannel.RemoveListener(GameEventType.LevelObjectiveCompleted, stateMachine.HandleLevelComplete);
    //  _eventChannel.RemoveListener(GameEventType.GameOver, stateMachine.HandleGameOver);
    //  _eventChannel.RemoveListener(GameEventType.EnterOptions, stateMachine.HandleEnterOptions);
    //  _eventChannel.RemoveListener(GameEventType.ExitOptions, stateMachine.HandleEnterOptions);
    //  _eventChannel.RemoveListener(GameEventType.EnterPause, stateMachine.HandleEnterPause);
    //  _eventChannel.RemoveListener(GameEventType.ExitPause, stateMachine.HandleExitPause);
    //}


    private void LogDebug(string message) {
      if (EnableDebugLogging) {
        Debug.Log($"[ServicesInitConfig] {message}");
      }
    }

    private void LogError(string message) {
      Debug.LogError($"[ServicesInitConfig] {message}");
    }
  }
}
