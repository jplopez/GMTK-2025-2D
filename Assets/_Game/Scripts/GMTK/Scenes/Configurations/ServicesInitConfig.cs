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

    private SceneController controller;
    private bool eventListenersInitialized = false;

    private void Awake() => InitializeAllServices();

    public void ApplyConfig(SceneController controller) {
      this.controller = controller;
      WireDefaultEventListeners();
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

    private void WireDefaultEventListeners() {
      LogDebug($"Wiring default event listeners for Scene {controller.SceneName}");
      if (eventListenersInitialized || !ForceReinitialize) return;

      var eventChannel = Services.Get<GameEventChannel>();
      var stateMachine = Services.Get<GameStateMachine>();
      var handlerRegistry = Services.Get<GameStateHandlerRegistry>();

      if (eventChannel == null || stateMachine == null) {
        LogError($"Cannot wire listeners - missing core services for Scene {controller.SceneName}");
        return;
      }

      //Centralized Handler for GameState changes
      stateMachine.RemoveListener(handlerRegistry.HandleStateChange);
      stateMachine.AddListener(handlerRegistry.HandleStateChange);
      
      // remove the GameState listener to ensure there are no duplicates
      CleanGameStateListeners(eventChannel, stateMachine);
      AddGameStateListeners(eventChannel, stateMachine);

      eventListenersInitialized = true;
      LogDebug("Default event listeners wired successfully");
    }

    private void AddGameStateListeners(GameEventChannel eventChannel, GameStateMachine stateMachine) {
      //Add here any GameEvent that should trigger a GameStateChange
      eventChannel.AddListener(GameEventType.GameStarted, stateMachine.HandleStartGame);
      eventChannel.AddListener(GameEventType.LevelStart, stateMachine.HandleLevelStart);
      eventChannel.AddListener(GameEventType.LevelPlay, stateMachine.HandleLevelPlay);
      eventChannel.AddListener(GameEventType.LevelReset, stateMachine.HandleLevelReset);
      eventChannel.AddListener(GameEventType.LevelObjectiveCompleted, stateMachine.HandleLevelComplete);
      eventChannel.AddListener(GameEventType.GameOver, stateMachine.HandleGameOver);
      eventChannel.AddListener(GameEventType.EnterOptions, stateMachine.HandleEnterOptions);
      eventChannel.AddListener(GameEventType.ExitOptions, stateMachine.HandleEnterOptions);
      eventChannel.AddListener(GameEventType.EnterPause, stateMachine.HandleEnterPause);
      eventChannel.AddListener(GameEventType.ExitPause, stateMachine.HandleExitPause);
    }

    private void CleanGameStateListeners(GameEventChannel eventChannel, GameStateMachine stateMachine) {

      eventChannel.RemoveListener(GameEventType.GameStarted, stateMachine.HandleStartGame);
      eventChannel.RemoveListener(GameEventType.LevelStart, stateMachine.HandleLevelStart);
      eventChannel.RemoveListener(GameEventType.LevelPlay, stateMachine.HandleLevelPlay);
      eventChannel.RemoveListener(GameEventType.LevelReset, stateMachine.HandleLevelReset);
      eventChannel.RemoveListener(GameEventType.LevelObjectiveCompleted, stateMachine.HandleLevelComplete);
      eventChannel.RemoveListener(GameEventType.GameOver, stateMachine.HandleGameOver);
      eventChannel.RemoveListener(GameEventType.EnterOptions, stateMachine.HandleEnterOptions);
      eventChannel.RemoveListener(GameEventType.ExitOptions, stateMachine.HandleEnterOptions);
      eventChannel.RemoveListener(GameEventType.EnterPause, stateMachine.HandleEnterPause);
      eventChannel.RemoveListener(GameEventType.ExitPause, stateMachine.HandleExitPause);
    }


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
