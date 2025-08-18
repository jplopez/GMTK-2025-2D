using Ameba;
using Ameba.Input;
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
    [Tooltip("the GameStateMachine handles the game states")]
    [SerializeField] protected GameStateMachine _gameStateMachine;
    [Tooltip("if true, the scene loading this manager changes the gamestate on Start. See 'StartingState' field")]
    public bool ChangeGameStateOnStart = false;
    [Tooltip("If 'ChangeGameStateOnStart' is true, this is the GameState this scene will assign in the Always method.")]
    public GameStates StartingState = GameStates.Start;
    [Tooltip("if true, the gamestate is changed right before loading the next scene. See 'EndingState' field")]
    public bool ChangeGameStateOnEnd = false;
    [Tooltip("If 'ChangeGameStateOnEnd' is true, this is the GameState assigned before loading the next scene.")]
    public GameStates EndingState = GameStates.Gameover;
    [Tooltip("The HandlerRegistry knows the changes needed in the game upon a game state change")]
    public GameStateHandlerRegistry HandlerRegistry;

    public enum SetCurrentSceneMode { InitialScene, Always, Never }

    [Header("Scene Management")]
    public string StartSceneName = "Start";
    public string LoadingSceneName = "Loading";
    public string GameOverSceneName = "GameOver";
    [Tooltip("The default behaviour of scenes to set themselfs as the CurrentScene. InitialScene: means is the first scene of the game. Only one scene should have this value. Always: (default) the scene sets itself as the current scene at their OnStart method. Never: for transitional scenes who want to skip setting themselves as current scene. For example: victory screen, pause menu, etc.")]
    public SetCurrentSceneMode DefaultCurrentSceneAssignment = SetCurrentSceneMode.Always;
    [Tooltip("Source of truth for level sequences. Can resolve the next scene based on the current one")]
    [SerializeField] protected LevelSequence _levelSequence;

    [Header("Player Inputs")]
    [Tooltip("Disable input controls for the scene. Useful for cinematics or UI specific scenes")]
    public bool DisableInputs = false;
    [Tooltip("The Input Registry from where player Inputs will be resolved")]
    public string InputRegistryName = "GameplayRegistry";
    [Tooltip("Reference to the InputActionRegistry from where player inputs are resolved. If specified, the InputRegistryName will be ignored")]
    [SerializeField] protected InputActionRegistry _inputRegistry;

    [Header("Events")]
    [Tooltip("Reference to the EventChannel instance to handle game events like 'Play button pressed', 'level start', etc")]
    [SerializeField] protected GameEventChannel _eventsChannel;

    [Header("Heads-Up Display (HUD)")]
    [Tooltip("Reference to the HUD scriptable object instance to manage score, playback buttons, help toggle and game menu button")]
    [SerializeField] protected HUD _hud;

    [Header("Score")]
    [Tooltip("ScoreGateKeeper instance to calculate marble's score in the HUD. This component lives inside the HUD")]
    [SerializeField] protected ScoreGateKeeper _marbleScoreKeeper;
    [Tooltip("If true, this scene will reset the score to zero. Typically, the start scene is the only one needing this as true")]
    public bool ResetScoreOnLoad = false;

    public GameStateMachine StateMachine => _gameStateMachine;
    public HUD Hud => _hud;
    public LevelSequence LevelSequence => _levelSequence;
    public GameEventChannel EventsChannel => _eventsChannel;
    public ScoreGateKeeper MarbleScoreKeeper => _marbleScoreKeeper;
    public InputActionRegistry InputHandler => _inputRegistry;
    protected virtual void Awake() {
      EnsureComponents();
      UpdateCurrentScene();
      AddGameEventListeners();
      ChangeToOnStartGameState();
    }

    private void OnDestroy() {
      RemoveGameEventListeners();
    }

    private void EnsureComponents() {
      string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

      // GameStateMachine : if missing, scene won't be able to change gamestates
      _gameStateMachine = LoadIfNull("GameStateMachine", _gameStateMachine,
        $"There is no GameStateMachine in scene {sceneName}. The scene won't be able to change game states");

      // _levelSequence: if missing, scene won't be able to resolve next levels
      _levelSequence = Resources.Load<LevelSequence>("LevelSequence");
      if (_levelSequence == null) {
        Debug.LogWarning($"There is no LevelSequence in scene {sceneName}. The scene won't be able to resolve the next scene");
      }

      //If inputs aren't disable we check if InputActionRegistry was provided, if not, try with InputRegistryName.
      if (!DisableInputs) {
        if (_inputRegistry == null) {
          if (string.IsNullOrEmpty(InputRegistryName)) {
            Debug.LogWarning($"There is no InputRegistryName in scene {sceneName}. The player controls might not respond");
          }
          else {
            try {
              _inputRegistry = (InputActionRegistry)Resources.Load(InputRegistryName);
            }
            catch (Exception e) {
              Debug.LogError($"Failed to load InputActionRegistry '{InputRegistryName}': {e.Message}");
#if UNITY_EDITOR
              Debug.LogWarning(e.StackTrace);
              throw e;
#endif
            }
          }
        }
      }
      else {
        Debug.Log($"InputAction are disabled scene '{sceneName}'");
      }

      if (_eventsChannel == null) {
        try {
          _eventsChannel = Resources.Load<GameEventChannel>("GameEventChannel");
        }
        catch (Exception e) {
          Debug.LogError($"Failed to load GameEventChannel: {e.Message}");
#if UNITY_EDITOR
          Debug.LogWarning(e.StackTrace);
          throw e;
#endif
        }
      }

      _marbleScoreKeeper = Resources.Load<ScoreGateKeeper>("MarbleScoreKeeper");
      if (_marbleScoreKeeper == null) {
        Debug.LogWarning($"Failed to load ScoreGateKeeper. The player's score might not be updated");
      }
      var scoreStrategy = gameObject.AddComponent<TimeBasedScoreCalculator>();
      scoreStrategy.PointsScoredInterval = 0.25f;
      scoreStrategy.PointsPerInterval = 50;

      _marbleScoreKeeper.SetStrategy(scoreStrategy, transform);
      if (ResetScoreOnLoad) {
        _marbleScoreKeeper.ResetScore();
      }

      HandlerRegistry = Resources.Load<GameStateHandlerRegistry>("GameStateHandlerRegistry");
      if (HandlerRegistry == null) {
        Debug.LogWarning($"There is no GameStateHandlerRegistry in scene {sceneName}. The scene won't be able to respond to GameState changes");

      }
      HandlerRegistry.Initialize();

    }

    #region Scene Management Methods

    public virtual void UpdateCurrentScene() {
      switch (DefaultCurrentSceneAssignment) {
        case SetCurrentSceneMode.Never: break;
        case SetCurrentSceneMode.InitialScene:
          //Initial scene keeps current as null, to force the loading of the
          //first scene in the sequence
          _levelSequence.CurrentScene = null; break;
        case SetCurrentSceneMode.Always:
          string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
          if (_levelSequence.CurrentScene != activeSceneName)
            _levelSequence.SetCurrentScene(activeSceneName);
          break;
      }
    }

    public virtual void LoadNextScene() {

      //If currentScene is null, LoadNextScene
      //goes for the first scene in the _levelSequence
      if (_levelSequence.CurrentScene == null) {
        string firstScene = _levelSequence.LevelSceneNames[0];
        if (string.IsNullOrEmpty(firstScene)) {
          Debug.LogError("LoadNextScene: First level scene name is empty in _levelSequence");
          return;
        }
        else {
          UnityEngine.SceneManagement.SceneManager.LoadScene(firstScene);
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
        if (_levelSequence.HasNextLevel()) {
          UnityEngine.SceneManagement.SceneManager.LoadScene(_levelSequence.GetNextLevel());
        }
        else {
          string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
          Debug.Log($"There's no next level for '{_levelSequence.CurrentScene}'. Reloading current scene: '{activeSceneName}'");
          UnityEngine.SceneManagement.SceneManager.LoadScene(activeSceneName);
        }
      }

      if (_levelSequence.HasNextLevel()) {
        UnityEngine.SceneManagement.SceneManager.LoadScene(_levelSequence.GetNextLevel());
      }
      else {
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"There's no next level for '{_levelSequence.CurrentScene}'. Reloading current scene: '{activeSceneName}'");
        UnityEngine.SceneManagement.SceneManager.LoadScene(activeSceneName);
      }
    }

    public void QuitGame() {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    #endregion

    #region GameState Changes

    public GameStates CurrentGameState => _gameStateMachine.Current;

    public bool CanTransitionTo(GameStates gameStates) => _gameStateMachine.TestTransition(_gameStateMachine.Current, gameStates);

    public void AddStateChangeListener(Action<StateMachineEventArg<GameStates>> action) => _gameStateMachine.AddListener(action);

    public void RemoveStateChangeListener(Action<StateMachineEventArg<GameStates>> action) => _gameStateMachine.RemoveListener(action);

    protected virtual void ChangeToOnStartGameState() {
      if (ChangeGameStateOnStart
        && (_gameStateMachine.Current != StartingState)) {
        _gameStateMachine.ChangeState(StartingState);
      }
    }

    protected virtual void ChangeToOnEndGameState() {
      if (ChangeGameStateOnEnd
        && (_gameStateMachine.Current != EndingState)) {
        _gameStateMachine.ChangeState(EndingState);
      }
    }

    #endregion

    #region GameState Change Events

    private void AddGameEventListeners() {

      //Centralized Handler for GameState changes
      _gameStateMachine.AddListener(HandlerRegistry.HandleStateChange);

      //Add here any GameEvent that should trigger a GameStateChange
      _eventsChannel.AddListener(GameEventType.GameStarted, _gameStateMachine.HandleStartGame);
      _eventsChannel.AddListener(GameEventType.LevelStart, _gameStateMachine.HandleLevelStart);
      _eventsChannel.AddListener(GameEventType.LevelPlay, _gameStateMachine.HandleLevelPlay);
      _eventsChannel.AddListener(GameEventType.LevelReset, _gameStateMachine.HandleLevelReset);
      _eventsChannel.AddListener(GameEventType.LevelCompleted, _gameStateMachine.HandleLevelComplete);
      _eventsChannel.AddListener(GameEventType.GameOver, _gameStateMachine.HandleGameOver);
      _eventsChannel.AddListener(GameEventType.EnterOptions, _gameStateMachine.HandleEnterOptions);
      _eventsChannel.AddListener(GameEventType.ExitOptions, _gameStateMachine.HandleEnterOptions);
      _eventsChannel.AddListener(GameEventType.EnterPause, _gameStateMachine.HandleEnterPause);
      _eventsChannel.AddListener(GameEventType.ExitPause, _gameStateMachine.HandleExitPause);
    }

    private void RemoveGameEventListeners() {

      //Centralized Handler for GameState changes
      _gameStateMachine.RemoveListener(HandlerRegistry.HandleStateChange);

      var eventChannel = Game.Context.EventsChannel;
      _eventsChannel.RemoveListener(GameEventType.GameStarted, _gameStateMachine.HandleStartGame);
      _eventsChannel.RemoveListener(GameEventType.LevelStart, _gameStateMachine.HandleLevelStart);
      _eventsChannel.RemoveListener(GameEventType.LevelPlay, _gameStateMachine.HandleLevelPlay);
      _eventsChannel.RemoveListener(GameEventType.LevelReset, _gameStateMachine.HandleLevelReset);
      _eventsChannel.RemoveListener(GameEventType.LevelCompleted, _gameStateMachine.HandleLevelComplete);
      _eventsChannel.RemoveListener(GameEventType.GameOver, _gameStateMachine.HandleGameOver);
      _eventsChannel.RemoveListener(GameEventType.EnterOptions, _gameStateMachine.HandleEnterOptions);
      _eventsChannel.RemoveListener(GameEventType.ExitOptions, _gameStateMachine.HandleEnterOptions);
      _eventsChannel.RemoveListener(GameEventType.EnterPause, _gameStateMachine.HandleEnterPause);
      _eventsChannel.RemoveListener(GameEventType.ExitPause, _gameStateMachine.HandleExitPause);
    }


    #endregion

    private T LoadIfNull<T>(string resourceName, T resource, string messageIfNotFound = "Not Found") where T : ScriptableObject {
      if (resource != null) return resource;
      if (string.IsNullOrEmpty(resourceName)) {
        Debug.LogWarning($"Can't load Resource with null or empty 'resourceName'");
        return default;
      }
      try {
        resource = Resources.Load<T>(resourceName);
        if (resource == null) {
          Debug.LogWarning($"Resource '{resourceName}': {messageIfNotFound}");
          return default;
        }
        return resource;
      }
      catch (Exception ex) {
        Debug.LogError($"Resource '{resourceName}': Exception thrown while loading => {ex.Message}");
#if UNITY_EDITOR
        Debug.LogException(ex);
#endif
        return default;
      }
    }

    [ContextMenu("Force Reinitialize GameStateHandlerRegistry")]
    private void ForceReinitialize() {
      _gameStateMachine.RemoveListener(HandlerRegistry.HandleStateChange);
      HandlerRegistry.Initialize();
    }
  }
}