using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ameba;

namespace GMTK {

  public enum GameStates { Start, Preparation, Playing, Reset, LevelComplete, Gameover, Pause, Options }

  [CreateAssetMenu(menuName = "GMTK/Game State Machine")]
  public class GameStateMachine : StateMachine<GameStates> {

    [Header("Event Integration")]
    [Tooltip("Reference to the GameEventChannel")]
    [SerializeField] protected GameEventChannel _eventChannel;
    [Tooltip("Events that automatically trigger state changes")]
    [SerializeField] private List<EventToStateMapping> _eventMappings = new();

    [Header("Handler Management")]
    [Tooltip("Automatically scan for handlers on Scene load")]
    public bool AutoScanOnSceneLoad = true;
    [Tooltip("Handler discovery settings")]
    public string[] IncludeTags = new string[0];
    public string[] ExcludeTags = new string[0];

    [Header("History & Debug")]
    [Tooltip("keeps history of recent status changes. Change the history length with HistoryLength. Keep in mind a longer history might impact performance")]
    [SerializeField] protected LinkedList<GameStates> _history = new();
    [Tooltip("how many game states back the history contains")]
    public int HistoryLength = 3;

    /* 
     * These flags control the initialization behavior of the state machine.
     * Enabling 'Clean' flags is useful during development to ensure a fresh setup each time.
     * In production, only the start scene should have them enabled, to avoid unnecessary overhead.
     */
    [Header("Initialization")]
    [Tooltip("If true, the state machine will clear all transitions and re-add default ones on OnEnable. If false, default transitions are added only if they are missing")]
    public bool CleanTransitions = false;
    [Tooltip("if true, the state machine will clear all event-to-state mappings and re-add default ones on OnEnable. If false, default mappings are added only if they are missing")]
    public bool CleanEventToState = false;
    [Tooltip("if true, the state machine will clear all discovered handlers and re-scan for them on OnEnable. If false, handlers are scanned only if none were found")]
    public bool CleanHandlersDiscovery = false;

    [Header("Debugging")]
    public bool EnableDebugLogging = false;

    public bool IsInitialized => _isInitialized;

    // Self-contained collections
    private List<GameStateHandler> _handlers = new();
    private Dictionary<GameEventType, GameStates> _eventToStateMap = new();

    // Initialization flags
    // If you change the initialization logic, make sure these flags are set correctly
    // to avoid re-initializing or re-adding things multiple times
    private bool _isInitialized = false;
    private bool _areDefaultTransitionsAdded = false;
    private bool _areMappingsBuilt = false;
    private bool _isSubcribedToEvents = false;
    private bool _areHandlersDiscovered = false;

    #region Initialization
    protected override void OnEnable() {
      base.OnEnable();
      StartingState = (StartingState == default) ? GameStates.Start : StartingState;
      AddDefaultTransitions();
    }

    protected override void OnDisable() {
      base.OnDisable();
      _isInitialized = false;
      _areDefaultTransitionsAdded = false;
      _areMappingsBuilt = false;
      _isSubcribedToEvents = false;
      _areHandlersDiscovered = false;
    }

    protected virtual void AddDefaultTransitions() {
      if (_isInitialized && _areDefaultTransitionsAdded) return;

      if (CleanTransitions) {
        ClearAllTransitions();
        _areDefaultTransitionsAdded = false;
      }
      //Add here any transition between states the game should consider valid
      AddTransition(GameStates.Start, GameStates.Preparation);
      AddTransition(GameStates.Start, GameStates.Options);
      AddTransition(GameStates.Preparation, GameStates.Playing);
      AddTransition(GameStates.Preparation, GameStates.Reset);
      AddTransition(GameStates.Preparation, GameStates.Pause);
      AddTransition(GameStates.Preparation, GameStates.Options);
      AddTransition(GameStates.Playing, GameStates.Reset);
      AddTransition(GameStates.Playing, GameStates.LevelComplete);
      AddTransition(GameStates.Playing, GameStates.Pause);
      AddTransition(GameStates.Playing, GameStates.Options);
      AddTransition(GameStates.Reset, GameStates.Preparation);
      AddTransition(GameStates.Reset, GameStates.Pause);
      AddTransition(GameStates.Reset, GameStates.Options);
      AddTransition(GameStates.LevelComplete, GameStates.Preparation);
      AddTransition(GameStates.Pause, GameStates.Preparation);
      AddTransition(GameStates.Pause, GameStates.Playing);
      AddTransition(GameStates.Pause, GameStates.Reset);
      AddTransition(GameStates.Options, GameStates.Start);
      AddTransition(GameStates.Options, GameStates.Preparation);
      AddTransition(GameStates.Options, GameStates.Playing);
      AddTransition(GameStates.Options, GameStates.Reset);
      //Gameover will be a default exit for all
      AddTransition(GameStates.Start, GameStates.Gameover);
      AddTransition(GameStates.Preparation, GameStates.Gameover);
      AddTransition(GameStates.Playing, GameStates.Gameover);
      AddTransition(GameStates.Reset, GameStates.Gameover);
      AddTransition(GameStates.LevelComplete, GameStates.Gameover);
      LogDebug($"{name} Default transitions added");
      _areDefaultTransitionsAdded = true;
    }

    public void SetEventChannelAndInitialize(GameEventChannel eventChannel) {
      LogDebug($"{name} Setting EventChannel and initializing GameStateMachine...");
      if (eventChannel == null) {
        LogWarning($"{name} Cannot initialize GameStateMachine: provided GameEventChannel is null");
        return;
      }
      // Avoid re-initialization if the channel is the same
      if (_isInitialized && eventChannel.Equals(_eventChannel)) return;
      _isInitialized = false;
      _eventChannel = eventChannel;
      InitializeEventDrivenSystem();
      LogDebug($"{name} GameStateMachine initialized with provided EventChannel");
      _isInitialized = true;
    }


    private void InitializeEventDrivenSystem() {

      // Get event channel from Services if not set
      if (_eventChannel == null) {
        if (Services.TryGet<GameEventChannel>(out var channel)) {
          _eventChannel = channel;
        }
        else {
          LogWarning($"{name} GameEventChannel not found in Services");
          return;
        }
      }

      // Build event-to-state mapping
      BuildEventMappings();

      // Subscribe to GameStateevents
      SubscribeToEvents();

      // Discover and register handlers
      if (AutoScanOnSceneLoad) {
        DiscoverHandlers();
      }

      LogDebug($"{name} Event-driven system initialized");
    }

    protected virtual void BuildEventMappings() {
      if (_isInitialized && _areMappingsBuilt) return;

      if (CleanEventToState) {
        _eventToStateMap.Clear();
        _areMappingsBuilt = false;
      }
      // Build from serialized mappings
      foreach (var mapping in _eventMappings) {
        AddUniqueEventToStateMap(mapping.EventType, mapping.TargetState);
      }

      // Add default mappings if not overridden
      AddUniqueEventToStateMap(GameEventType.GameStarted, GameStates.Start);
      AddUniqueEventToStateMap(GameEventType.LevelStart, GameStates.Preparation);
      AddUniqueEventToStateMap(GameEventType.LevelPlay, GameStates.Playing);
      AddUniqueEventToStateMap(GameEventType.LevelReset, GameStates.Reset);
      // ... etc

      _areMappingsBuilt = true;
    }

    private void AddUniqueEventToStateMap(GameEventType eventType, GameStates targetState) {
      if (!_eventToStateMap.ContainsKey(eventType)) {
        _eventToStateMap[eventType] = targetState;
      }
    }

    #endregion

    #region Event Subscription

    /// <summary>
    /// Subscribe the GameStateMachine as listener to the GameEvents that should trigger Game State changes
    /// </summary>
    protected virtual void SubscribeToEvents() {
      if (_isInitialized && _isSubcribedToEvents) return;
      foreach (var eventType in _eventToStateMap.Keys) {
        _eventChannel.AddListener(eventType, () => HandleGameEvent(eventType));
      }
      _isSubcribedToEvents = true;
    }

    /// <summary>
    /// Wrapper to handle GameEvents that trigger a state change
    /// </summary>
    /// <param name="eventType"></param>
    private void HandleGameEvent(GameEventType eventType) {
      if (_eventToStateMap.TryGetValue(eventType, out var targetState)) {
        LogDebug($"Event {eventType} triggering state change to {targetState}");
        ChangeState(targetState);
      }
    }

    #endregion

    #region State Handlers

    /// <summary>
    /// Searches for all GameStateHandler present in the current scene. 
    /// If CleanHandlersDiscovery is true, clears existing handlers before scanning.
    /// If AutoScanOnSceneLoad is true, scans automatically when the scene is loaded.
    /// </summary>
    public virtual void DiscoverHandlers() {
      if (_isInitialized && _areHandlersDiscovered && !AutoScanOnSceneLoad) return;
      LogDebug($"{name} Discovering state handlers...");

      if (CleanHandlersDiscovery) {
        _handlers.Clear();
        _areHandlersDiscovered = false;
      }

      var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
      var filteredObjects = FilterGameObjectsByTags(allGameObjects);

      foreach (var go in filteredObjects) {
        var handlers = go.GetComponents<GameStateHandler>();
        foreach (var handler in handlers) {
          RegisterHandler(handler);
        }
      }
      // Sort by priority
      _handlers = _handlers.OrderBy(h => h.Priority).ToList();
      LogDebug($"{name} Discovered {_handlers.Count} handlers");

      _areHandlersDiscovered = true;
    }

    public virtual void RegisterHandler(GameStateHandler handler) {
      if (!_handlers.Contains(handler)) {
        _handlers.Add(handler);
        _handlers = _handlers.OrderBy(h => h.Priority).ToList();
        LogDebug($"{name} Registered handler: {handler.HandlerName}");
      }
    }

    public virtual void UnregisterHandler(GameStateHandler handler) {
      _handlers.Remove(handler);
      LogDebug($"{name} Unregistered handler: {handler.HandlerName}");
    }

    #endregion

    #region Change State 
    public override bool ChangeState(GameStates newState) {
      var fromState = Current;

      if (base.ChangeState(newState)) {
        AddStateChangeToHistory(newState);
        NotifyHandlers(fromState, newState);
        return true;
      }
      return false;
    }

    private void NotifyHandlers(GameStates fromState, GameStates toState) {
      var eventArg = new StateMachineEventArg<GameStates>(fromState, toState);

      foreach (var handler in _handlers) {
        if (!handler.IsEnabled) continue;

        try {
          handler.HandleStateChange(eventArg);
          LogDebug($"{name} ✓ {handler.HandlerName} handled state change");
        }
        catch (System.Exception ex) {
          LogError($"{name} ✗ Handler '{handler.HandlerName}' failed: {ex.Message}");
          Debug.LogException(ex);
        }
      }
    }

    #endregion

    #region Utilities

    private GameObject[] FilterGameObjectsByTags(GameObject[] allObjects) {
      if (IncludeTags.Length == 0 && ExcludeTags.Length == 0) {
        return allObjects;
      }

      return allObjects.Where(go => {
        if (ExcludeTags.Length > 0 && ExcludeTags.Contains(go.tag)) {
          return false;
        }
        if (IncludeTags.Length > 0 && !IncludeTags.Contains(go.tag)) {
          return false;
        }
        return true;
      }).ToArray();
    }

    #endregion

    #region State Change history

    /// <summary>
    /// The count of state changes currently in history
    /// </summary>
    public int CurrentHistoryCount() => _history.Count;

    /// <summary>
    /// Returns a readonly list of all state changes currently in history
    /// </summary>
    public IReadOnlyList<GameStates> GetStateChangesHistory() => _history.ToList();

    /// <summary>
    /// Returns the most recent state change added to history (i.e. Pop)
    /// </summary>
    public GameStates LastHistoryStateChange() => _history.Last();

    /// <summary>
    /// Returns the oldest state change currently in history
    /// </summary>
    public GameStates OldestHistoryStateChange() => _history.First();

    /// <summary>
    /// <para>Returns the state change at position <c>index</c>.</para>
    /// <para>Returns <c>null</c> if the index is out of bounds.</para>
    /// </summary>
    /// <param name="index">Must be positive and less than HistoryCount</param>
    public GameStates GetHistoryStateChangeAtIndex(int index) =>
      (index < 0 || index >= _history.Count) ? default : _history.ElementAt(index);
    public bool ContainsHistoryStateChange(GameStates gameState) => _history.Contains(gameState);

    protected virtual void AddStateChangeToHistory(GameStates gameStates) {
      _history.AddLast(gameStates);
      if (_history.Count > HistoryLength) {
        _history.RemoveFirst();
      }
    }

    #endregion

    #region Context-Dependent State Changes

    // Special handling for context-dependent state changes
    public virtual void HandleExitOptions() {
      if (Current != GameStates.Options) return;
      var previousState = GetHistoryStateChangeAtIndex(CurrentHistoryCount() - 2);
      if (previousState != default) {
        ChangeState(previousState);
      }
    }

    public void HandleExitPause() {
      if (Current != GameStates.Pause) return;
      var previousState = GetHistoryStateChangeAtIndex(CurrentHistoryCount() - 2);
      if (previousState != default) {
        ChangeState(previousState);
      }
    }

    #endregion

    #region Logging and Debugging 
    // Logging helpers
    private void LogDebug(string message) {
      if (EnableDebugLogging) Debug.Log($"[GameStateMachine] {message}");
    }
    private void LogWarning(string message) => Debug.LogWarning($"[GameStateMachine] {message}");
    private void LogError(string message) => Debug.LogError($"[GameStateMachine] {message}");

    // Inspector helpers
    public int HandlerCount => _handlers?.Count ?? 0;

    [ContextMenu("Force Scan Handlers")]
    private void ForceScan() => DiscoverHandlers();

    #endregion
  }

  [System.Serializable]
  public class EventToStateMapping {
    public GameEventType EventType;
    public GameStates TargetState;
  }
}