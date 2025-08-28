using Ameba;
using System;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// <para>GameContext is a MonoBehaviour with the references to all must have 
  /// resources like ScriptableObjects used by Controllers and MonoBehaviours
  /// across the game.</para> 
  /// <para>By placing it in the scene, you can define the behaviour of the game on GameStates, Scene Management, Player Inputs, Events, HUD and Score.</para> 
  /// <para>The recommended way of using this controller is having a Prefab with all the game definitions in place, and place it on every scene.</para>
  /// </summary>
  public class GameContext : MonoBehaviour {

    [Header("GameState Settings")]
    [Tooltip("if true, the scene loading this manager changes the gamestate on Start. See 'StartingGameState' field")]
    public bool ChangeGameStateOnStart = false;
    [Tooltip("If 'ChangeGameStateOnStart' is true, this is the GameState this scene will assign in the Always method.")]
    public GameStates StartingGameState = GameStates.Start;
    [Tooltip("if true, the gamestate is changed right before loading the next scene. See 'EndingState' field")]
    public bool ChangeGameStateOnEnd = false;
    [Tooltip("If 'ChangeGameStateOnEnd' is true, this is the GameState assigned before loading the next scene.")]
    public GameStates EndingState = GameStates.Gameover;

    public enum CurrentSceneSetMode { InitialScene, Always, Never }

    [Header("Scene Management")]
    public string StartSceneName = "Start";
    public string LoadingSceneName = "Loading";
    public string GameOverSceneName = "GameOver";
    [Tooltip("The default behaviour of scenes to set themselfs as the CurrentScene. InitialScene: means is the first scene of the game. Only one scene should have this value. Always: (default) the scene sets itself as the current scene at their OnStart method. Never: for transitional scenes who want to skip setting themselves as current scene. For example: victory screen, pause menu, etc.")]
    public CurrentSceneSetMode CurrentSceneMode = CurrentSceneSetMode.Always;

    [Header("Scene-Specific Settings")]
    [Tooltip("DisableActionMap input controls for the scene. Useful for cinematics or UI specific scenes")]
    public bool DisableInputs = false;
    [Tooltip("If true, this scene will reset the score to zero. Typically, the start scene is the only one needing this as true")]
    public bool ResetScoreOnLoad = false;


    //Public Getters

    // Properties that delegate to Game's ScriptableObjects
    public GameStateMachine StateMachine => Game.StateMachine;
    public GameEventChannel EventsChannel => Game.EventChannel;
    public InputActionEventChannel InputEventsChannel => Game.InputEventChannel;
    public LevelSequence LevelSequence => Game.LevelSequence;
    public ScoreGateKeeper MarbleScoreKeeper => Game.ScoreKeeper;
    public GameStateHandlerRegistry HandlerRegistry => Game.HandlerRegistry;

    // Scene-specific state
    public GameStates CurrentGameState => (StateMachine != null) ? StateMachine.Current : GameStates.Start;


    // WebGL GC protection
    private static GameContext _protectedInstance;

    protected virtual void Awake() {
      // Prevent WebGL from unloading this critical object
      //DontDestroyOnLoad(gameObject);

      // Protect from garbage collection
      _protectedInstance = this;
      System.GC.KeepAlive(this);

      // Register this context with the Game IMMEDIATELY
      Game.SetContext(this);
      Debug.Log($"[GameContext] Registered GameContext name: '{name}'");

      // Wait for ScriptableObjects to be ready
      InitializationManager.WaitForInitialization(this, OnResourcesReady);
    }

    private void OnResourcesReady() {
      Debug.Log($"[GameContext] '{name}' : Resources Ready!");

      // Keep this object alive throughout the session
      System.GC.KeepAlive(this);
      System.GC.KeepAlive(gameObject);

      // Validate all required ScriptableObjects are available
      if (!ValidateResources()) {
        Debug.LogError("GameContext: Required ScriptableObjects not available!");
        return;
      }

      // Continue with scene-specific initialization
      UpdateCurrentScene();
      AddGameEventListeners();
      ApplySceneSettings();

      // Start continuous GC protection
      StartCoroutine(ProtectFromGarbageCollection());
    }


    /// <summary>
    /// Continuously protect this object from garbage collection
    /// </summary>
    private System.Collections.IEnumerator ProtectFromGarbageCollection() {
      while (this != null && gameObject != null) {
        // Keep references alive
        System.GC.KeepAlive(this);
        System.GC.KeepAlive(gameObject);
        System.GC.KeepAlive(_protectedInstance);

        // Ensure Game.Context reference is maintained
        if (Game.Context.name != name) {
          Debug.LogWarning($"[GameContext] Context reference lost, re-registering: {name}");
          Game.SetContext(this);
        }

        yield return new WaitForSeconds(1f); // Check every second
      }
    }

    private void OnDestroy() {
      // Clear protection when explicitly destroyed
      _protectedInstance = null;

      RemoveGameEventListeners();

      // Clear context reference if this was the active context
      if (Game.Context == this) {
        Game.SetContext(null);
      }
    }

    private bool ValidateResources() {
      return StateMachine != null &&
             EventsChannel != null &&
             (!DisableInputs || InputEventsChannel != null);
    }

    private void ApplySceneSettings() {
      if (ResetScoreOnLoad && MarbleScoreKeeper != null) {
        MarbleScoreKeeper.ResetScore();
      }

      if (ChangeGameStateOnStart 
            && StateMachine != null 
            && StateMachine.Current != StartingGameState) {
        StateMachine.ChangeState(StartingGameState);
      }
    }

    #region SceneManagement

    public virtual void UpdateCurrentScene() {
      switch (CurrentSceneMode) {
        case CurrentSceneSetMode.Never: break;
        case CurrentSceneSetMode.InitialScene:
          //Initial scene keeps current as null, to force the loading of the
          //first scene in the sequence
          LevelSequence.CurrentScene = null; break;
        case CurrentSceneSetMode.Always:
          string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
          if (LevelSequence.CurrentScene != activeSceneName)
            LevelSequence.SetCurrentScene(activeSceneName);
          break;
      }
    }

    public virtual void LoadCurrentScene() {

      var loadingScene = LevelSequence.CurrentScene;

      //If loadingScene is null, LoadNextScene
      //goes for the first scene in the _levelSequence
      if (string.IsNullOrEmpty(loadingScene)) {
        loadingScene = LevelSequence.LevelSceneNames[0];
        if (string.IsNullOrEmpty(loadingScene)) {
          Debug.LogError("LoadNextScene: First level scene name is empty in _levelSequence");
          return;
        }
      } 
      UnityEngine.SceneManagement.SceneManager.LoadScene(loadingScene);
    }

    public virtual void LoadNextScene() {

      var loadingScene = LevelSequence.CurrentScene;

      //If loadingScene is null, LoadNextScene
      //goes for the first scene in the _levelSequence
      if (string.IsNullOrEmpty(loadingScene)) {
        loadingScene = LevelSequence.LevelSceneNames[0];
        if (string.IsNullOrEmpty(loadingScene)) {
          Debug.LogError("LoadNextScene: First level scene name is empty in _levelSequence");
          return;
        }
      }
      // If currentScene is not null, we check if there is a next level
      // in the sequence and load that scene
      // if there are no more scenes in the sequence, we default to
      // current active scene.
      // TODO: define a 'default' scene for these cases or assume
      // the game is finished and go back to first scene.
      else {
        if (LevelSequence.HasNextLevel(loadingScene)) {
          loadingScene = LevelSequence.GetNextLevel(loadingScene);
        }
        else {
          loadingScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
          Debug.Log($"There's no next level for '{LevelSequence.CurrentScene}'. Reloading current scene: '{loadingScene}'");
        }
      }

      UnityEngine.SceneManagement.SceneManager.LoadScene(loadingScene);
    }

    public void QuitGame() {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    #endregion

    #region GameStates

    public bool CanTransitionTo(GameStates gameStates) => StateMachine.TestTransition(StateMachine.Current, gameStates);
    public void AddStateChangeListener(Action<StateMachineEventArg<GameStates>> action) => StateMachine.AddListener(action);
    public void RemoveStateChangeListener(Action<StateMachineEventArg<GameStates>> action) => StateMachine.RemoveListener(action);

    protected virtual void ApplyOnStartGameState() {
      if (ChangeGameStateOnStart
        && (StateMachine.Current != StartingGameState)) {
        StateMachine.ChangeState(StartingGameState);
      }
    }

    protected virtual void ChangeToOnEndGameState() {
      if (ChangeGameStateOnEnd
        && (StateMachine.Current != EndingState)) {
        StateMachine.ChangeState(EndingState);
      }
    }

    #endregion

    #region GameState Change Events

    private void AddGameEventListeners() {
      //Centralized Handler for GameState changes
      StateMachine.AddListener(HandlerRegistry.HandleStateChange);

      //Add here any GameEvent that should trigger a GameStateChange
      EventsChannel.AddListener(GameEventType.GameStarted, StateMachine.HandleStartGame);
      EventsChannel.AddListener(GameEventType.LevelStart, StateMachine.HandleLevelStart);
      EventsChannel.AddListener(GameEventType.LevelPlay, StateMachine.HandleLevelPlay);
      EventsChannel.AddListener(GameEventType.LevelReset, StateMachine.HandleLevelReset);
      EventsChannel.AddListener(GameEventType.LevelObjectiveCompleted, StateMachine.HandleLevelComplete);
      EventsChannel.AddListener(GameEventType.GameOver, StateMachine.HandleGameOver);
      EventsChannel.AddListener(GameEventType.EnterOptions, StateMachine.HandleEnterOptions);
      EventsChannel.AddListener(GameEventType.ExitOptions, StateMachine.HandleEnterOptions);
      EventsChannel.AddListener(GameEventType.EnterPause, StateMachine.HandleEnterPause);
      EventsChannel.AddListener(GameEventType.ExitPause, StateMachine.HandleExitPause);
    }

    private void RemoveGameEventListeners() {
      //Centralized Handler for GameState changes
      StateMachine.RemoveListener(HandlerRegistry.HandleStateChange);

      EventsChannel.RemoveListener(GameEventType.GameStarted, StateMachine.HandleStartGame);
      EventsChannel.RemoveListener(GameEventType.LevelStart, StateMachine.HandleLevelStart);
      EventsChannel.RemoveListener(GameEventType.LevelPlay, StateMachine.HandleLevelPlay);
      EventsChannel.RemoveListener(GameEventType.LevelReset, StateMachine.HandleLevelReset);
      EventsChannel.RemoveListener(GameEventType.LevelCompleted, StateMachine.HandleLevelComplete);
      EventsChannel.RemoveListener(GameEventType.GameOver, StateMachine.HandleGameOver);
      EventsChannel.RemoveListener(GameEventType.EnterOptions, StateMachine.HandleEnterOptions);
      EventsChannel.RemoveListener(GameEventType.ExitOptions, StateMachine.HandleEnterOptions);
      EventsChannel.RemoveListener(GameEventType.EnterPause, StateMachine.HandleEnterPause);
      EventsChannel.RemoveListener(GameEventType.ExitPause, StateMachine.HandleExitPause);
    }

    #endregion

  }
}