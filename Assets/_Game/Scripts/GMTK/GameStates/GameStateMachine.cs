using Ameba;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GMTK.GameStateMachine;

namespace GMTK {

  public enum GameStates { Start, Preparation, Playing, Reset, LevelComplete, Gameover, Pause, Options }

  [CreateAssetMenu(menuName = "GMTK/Game State Machine")]
  public class GameStateMachine : StateMachine<GameStates> {

    public enum InitializationModes { Clean, Incremental, Once }

    public enum HandlerDiscoveryModes { AfterSceneLoad, Lazy }

    [Header("Initialization")]
    [Tooltip("The strategy to initialize the internal systems. Incremental (defaults): adds system founds on the scene that aren't already added. Clean: clears all internal systems before scanning the scene. Once: initializes all systems only once and keeps them for the lifetime of the game.")]
    public InitializationModes InitializationMode = InitializationModes.Incremental;

    [Tooltip("The strategy to discover GameStateHandlers. AfterSceneLoad (default and recommended): automatically after scene load. Lazy: when the first GameState change occurs.")]
    public HandlerDiscoveryModes HandlerDiscoveryMode = HandlerDiscoveryModes.AfterSceneLoad;
    [Tooltip("GameState Handlers discovery settings")]
    public string[] IncludeTags = new string[0];
    public string[] ExcludeTags = new string[0];

    [Header("Events Integration")]
    [Tooltip("Events that automatically trigger state changes")]
    [SerializeField] protected List<EventToStateMapping> _eventMappings = new();


    [Header("History & Debug")]
    [Tooltip("keeps history of recent status changes. Change the history length with HistoryLength. Keep in mind a longer history might impact performance")]
    [SerializeField] protected LinkedList<GameStates> _history = new();
    [Tooltip("how many game states back the history contains")]
    public int HistoryLength = 3;

    [Header("Internal Event Channel")]
    [Tooltip("Internal event channel used by the state machine to manage state changes. This channel is created automatically if not assigned")]
    [SerializeField] protected GameStateMachineEventChannel _internalEventChannel;

    [Header("Debugging")]
    public bool EnableDebugLogging = false;

    // Public API to access internal fields (read-only)
    public bool IsInitialized => _isInitialized;
    public bool IsSubscribedToExternalEvents => _isSubcribedToExternalEvents;
    public bool IsSubscribedToInternalEvents => _isSubcribedToInternalEvents;
    public bool AreHandlersDiscovered => _areHandlersDiscovered;
    public GameStateMachineEventChannel EventChannel => _internalEventChannel;

    // Self-contained collections
    private List<GameStateHandler> _handlers = new();
    private Dictionary<GameEventType, GameStates> _eventToStateMap = new();
    private GameEventChannel _externalEventChannel;

    // Initialization flags
    // If you change the initialization logic, make sure these flags are set correctly
    // to avoid re-initializing or re-adding things multiple times
    private bool _isInitialized = false;
    private bool _areDefaultTransitionsAdded = false;
    private bool _areMappingsBuilt = false;
    private bool _isSubcribedToExternalEvents = false;
    private bool _isSubcribedToInternalEvents = false;
    private bool _areHandlersDiscovered = false;

    #region Initialization and Cleanup
    protected override void OnEnable() {
      base.OnEnable();
      StartingState = (StartingState == default) ? GameStates.Start : StartingState;
      AddDefaultTransitions();
      InitializeInternalSystems();

      // Subscribe to scene loaded events for automatic handler discovery
      if (HandlerDiscoveryMode == HandlerDiscoveryModes.AfterSceneLoad) {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LogDebug("Subscribed to SceneManager.sceneLoaded for automatic handler discovery");
      }
    }

    protected override void OnDisable() {
      base.OnDisable();
      //event to state mapping arent reseted because they are built in the editor
      ResetInitializationFlags(mappingsBuilt:true);
      ClearAll(eventToStateMap:false);
    }

    /// <summary>
    /// Called automatically when a scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
      LogDebug($"Scene loaded: {scene.name} (mode: {mode})");

      // Only discover handlers if mode is set to AfterSceneLoad
      if (HandlerDiscoveryMode == HandlerDiscoveryModes.AfterSceneLoad) {
        LogDebug("Starting automatic handler discovery after scene load");
        DiscoverHandlers();
      }
    }

    private void ResetInitializationFlags(bool defaultTransitions=false, bool mappingsBuilt=false,bool internalEvents=false, bool externalEvents=false,bool handlersDiscovered=false) {
      _isInitialized = false;
      _areDefaultTransitionsAdded = defaultTransitions;
      _areMappingsBuilt = mappingsBuilt;
      _isSubcribedToInternalEvents = internalEvents;
      _isSubcribedToExternalEvents = externalEvents;
      _areHandlersDiscovered = handlersDiscovered;
    }

    private void ClearAll(bool eventToStateMap=true, bool history=true, bool handlers=true, bool internalEvent=true, bool externalEvents=true) {
      ClearAllTransitions();
      if(eventToStateMap) _eventToStateMap.Clear();
      if (handlers) {
        _handlers.Clear();
        if(_handlers.Count > 0) {
          _handlers = new();
        }
      }
      if(history) _history.Clear();
      if (_internalEventChannel != null && internalEvent) {
        _internalEventChannel.RemoveAllListeners();
      }
      if (_externalEventChannel != null && externalEvents) {
        _externalEventChannel.RemoveAllListeners();
      }
    }

    protected virtual void AddDefaultTransitions() {
      // nothing to do here
      if (InitializationMode == InitializationModes.Once
          && _isInitialized && _areDefaultTransitionsAdded) return;
      // clean and re-add
      if (InitializationMode == InitializationModes.Clean) {
        ClearAllTransitions();
        _areDefaultTransitionsAdded = false;
      }

      //These are default transitions for a typical game flow
      //'AddTransition' ensures no duplicates are added,
      //so it's safe to call multiple times (ie: InitializationMode = Incremental)
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

    private void InitializeInternalSystems() {
      // Create internal event channel if it doesn't exist
      if (_internalEventChannel == null) {
        _internalEventChannel = CreateInstance<GameStateMachineEventChannel>();
        _internalEventChannel.name = "GameStateMachine_InternalEvents";
      }

      // Build event-to-state mapping
      BuildEventMappings();
      // Subscribe to internal events
      SubscribeToInternalEvents();
      // Discover and register handlers
      if(HandlerDiscoveryMode == HandlerDiscoveryModes.AfterSceneLoad) {
        if(SceneManager.GetActiveScene().isLoaded) {
          DiscoverHandlers();
        }
      }
      //DiscoverHandlers();
      //// Try to connect to external event channel (optional)
      TryConnectToExternalEvents();

      _isInitialized = true;
      LogDebug("Internal event system initialized");
    }

    protected virtual void BuildEventMappings() {
      if (InitializationMode == InitializationModes.Once
          && _isInitialized && _areMappingsBuilt) return;

      if (InitializationMode == InitializationModes.Clean) {
        _eventToStateMap.Clear();
        _areMappingsBuilt = false;
      }

      // Build from serialized mappings
      foreach (var mapping in _eventMappings) {
        AddUniqueEventToStateMap(mapping.EventType, mapping.TargetState);
      }

      // Add default mappings
      AddUniqueEventToStateMap(GameEventType.GameStarted, GameStates.Start);
      AddUniqueEventToStateMap(GameEventType.LevelStart, GameStates.Preparation);
      AddUniqueEventToStateMap(GameEventType.LevelPlay, GameStates.Playing);
      AddUniqueEventToStateMap(GameEventType.LevelReset, GameStates.Reset);
      AddUniqueEventToStateMap(GameEventType.LevelObjectiveCompleted, GameStates.LevelComplete);
      AddUniqueEventToStateMap(GameEventType.GameOver, GameStates.Gameover);
      AddUniqueEventToStateMap(GameEventType.EnterOptions, GameStates.Options);
      AddUniqueEventToStateMap(GameEventType.EnterPause, GameStates.Pause);

      LogDebug($"BuildEventMappings - Added {_eventMappings.Count} custom event-to-state mappings");
      _areMappingsBuilt = true;
    }

    private void AddUniqueEventToStateMap(GameEventType eventType, GameStates targetState) {
      if (!_eventToStateMap.ContainsKey(eventType)) _eventToStateMap[eventType] = targetState;
    }

    #endregion

    #region GameStateHandler Discovery 

    /// <summary>
    /// Discovers handlers in the current scene. Called automatically after scene initialization.
    /// Strategy is controlled by InitializationMode.
    /// </summary>
    public virtual void DiscoverHandlers() {
      LogDebug($"=== DiscoverHandlers START ===");
      LogDebug($"InitializationMode: {InitializationMode}");
      LogDebug($"HandlerDiscoveryMode: {HandlerDiscoveryMode}");
      LogDebug($"_areHandlersDiscovered: {_areHandlersDiscovered}");

      if (InitializationMode == InitializationModes.Once && _areHandlersDiscovered) {
        LogDebug("Skipping handler discovery - Once mode and already discovered");
        return;
      }

      if (InitializationMode == InitializationModes.Clean) {
        LogDebug("Clean mode: clearing existing handlers");
        _handlers.Clear();
        _areHandlersDiscovered = false;
      }

      // Apply tag filtering
      var filteredHandlers = FindAllStateHandlersFilteredByTag();
      LogDebug($"After tag filtering: {filteredHandlers.Length} handlers");


      // Register handlers
      int registeredCount = 0;
      foreach (var handler in filteredHandlers) {
        if (handler != null && RegisterHandler(handler)) {
          registeredCount++;
          LogDebug($"Registered: {handler.GetType().Name} (Priority: {handler.Priority})");
        }
      }

      // Sort by priority
      SortHandlers();

      LogDebug($"Handler discovery complete: {registeredCount} new handlers, {_handlers.Count} total");
      LogDebug($"=== DiscoverHandlers END ===");

      _areHandlersDiscovered = true;
    }

    /// <summary>
    /// Registers a handler if not already present (for Incremental mode)
    /// </summary>
    private bool RegisterHandler(GameStateHandler handler) {
      if (handler == null) return false;

      // In Incremental mode, check for duplicates
      if (InitializationMode == InitializationModes.Incremental && _handlers.Contains(handler)) {
        LogDebug($"Handler {handler.GetType().Name} already registered, skipping");
        return false;
      }

      _handlers.Add(handler);
      return true;
    }

    private void SortHandlers() {
      _handlers = _handlers.OrderBy(h => h.Priority).ToList();
      LogDebug($"Handlers sorted by priority");
    }

    public virtual void UnregisterHandler(GameStateHandler handler) {
      _handlers.Remove(handler);
      //_registeredHandlers.RemoveAll(h => h.Handler == handler);
      LogDebug($"Unregistered handler: {handler.HandlerName}");
    }

    private GameStateHandler[] FindAllStateHandlersFilteredByTag() {

      // Find all GameStateHandler components in the scene
      var allStateHandlers = FindObjectsByType<GameStateHandler>(FindObjectsSortMode.None);
      LogDebug($"Found {allStateHandlers.Length} GameStateHandler components in scene");
      if (IncludeTags.Length == 0 && ExcludeTags.Length == 0) return allStateHandlers;

      // Filter by tags
      LogDebug($"Applying tag filters - Include: [{string.Join(", ", IncludeTags)}], Exclude: [{string.Join(", ", ExcludeTags)}]");
      return allStateHandlers.Where(h => {
        if (ExcludeTags.Length > 0 && ExcludeTags.Contains(h.gameObject.tag)) { return false; }
        if (IncludeTags.Length > 0 && !IncludeTags.Contains(h.gameObject.tag)) { return false; }
        return true;
      }).ToArray();

    }

    #endregion

    #region Event Subscription

    public void ConnectToExternalEvents(GameEventChannel eventChannel) {
      LogDebug($"ConnectToExternalEvents - Setting external GameEventChannel");
      _externalEventChannel = eventChannel;
      TryConnectToExternalEvents();
      string message = (_isSubcribedToExternalEvents) ? "connected" : "not connected";
      LogDebug($"ConnectToExternalEvents - External GameEventChannel {message}");
    }

    private void SubscribeToInternalEvents() {

      if (InitializationMode == InitializationModes.Once
         && _isInitialized && _isSubcribedToInternalEvents) return;
      if (InitializationMode == InitializationModes.Clean) {
        // Unsubscribe all existing listeners
        _internalEventChannel.RemoveAllListeners();
        _isSubcribedToInternalEvents = false;
      }
      //AddListener ensures no duplicates are added, so it's safe to call multiple times
      foreach (var eventType in _eventToStateMap.Keys) {
        _internalEventChannel.AddListener(eventType, () => HandleGameEvent(eventType));
      }
      LogDebug($"SubscribeToInternalEvents - {_eventToStateMap.Count} internal GameEventChannel events");
      // Special handlers for context-dependent events
      _internalEventChannel.AddListener(GameEventType.ExitOptions, HandleExitOptions);
      _internalEventChannel.AddListener(GameEventType.ExitPause, HandleExitPause);
      LogDebug($"SubscribeToInternalEvents - Added Exit Options and Pause internal GameEventChannel events");
      _isSubcribedToInternalEvents = true;
    }

    private void TryConnectToExternalEvents() {

      if (InitializationMode == InitializationModes.Once
          && _isInitialized && _isSubcribedToExternalEvents) return;

      // Optional: Connect to external event channel if available
      // This allows other systems to trigger state changes through the global event channel
      if (ServiceLocator.TryGet<GameEventChannel>(out var channel)) {

        _externalEventChannel = channel;

        if (InitializationMode == InitializationModes.Clean) {
          _externalEventChannel.RemoveAllListeners();
          _isSubcribedToExternalEvents = false;
        }

        // Subscribe to external events and forward them to internal channel
        foreach (var eventType in _eventToStateMap.Keys) {
          _externalEventChannel.AddListener(eventType, () => {
            LogDebug($"TryConnectToExternalEvents - External event {eventType} forwarded to internal channel");
            HandleGameEvent(eventType);
          });
        }
        LogDebug($"TryConnectToExternalEvents - Subscribed to {_eventToStateMap.Count} external GameEventChannel events");

        _externalEventChannel.AddListener(GameEventType.ExitOptions, HandleExitOptions);
        _externalEventChannel.AddListener(GameEventType.ExitPause, HandleExitPause);
        LogDebug($"TryConnectToExternalEvents - Subscribed to Exit Options and Pause external GameEventChannel events");
        _isSubcribedToExternalEvents = true;
      }
      else {
        LogDebug("TryConnectToExternalEvents - No external GameEventChannel found - using internal events only");
        _isSubcribedToExternalEvents = false;
        return;
      }
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

    // Public API for external systems to trigger events
    public void TriggerEvent(GameEventType eventType) {
      _internalEventChannel.Raise(eventType);
    }

    public void TriggerEvent<T>(GameEventType eventType, T payload) {
      _internalEventChannel.Raise(eventType, payload);
    }

    #endregion

    #region State Changes

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
      // Ensure handlers are discovered before notifying
      if (!_areHandlersDiscovered) {
        LogDebug("Handlers not discovered yet, discovering now...");
        DiscoverHandlers();
      }

      var eventArg = new StateMachineEventArg<GameStates>(fromState, toState);
      LogDebug($"Notifying {_handlers.Count} handlers of state change: {fromState} -> {toState}");

      foreach (var handler in _handlers) {
        if (handler == null) {
          LogWarning("Found null handler in list");
          continue;
        }

        if (!handler.IsEnabled) continue;

        try {
          handler.HandleStateChange(eventArg);
          LogDebug($"✓ {handler.HandlerName} handled state change");
        }
        catch (System.Exception ex) {
          LogError($"✗ Handler '{handler.HandlerName}' failed: {ex.Message}");
          Debug.LogException(ex);
        }
      }
    }

    #endregion

    #region History

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

    [ContextMenu("Print Handlers")]
    private void PrintHandlers() {
      Debug.Log($"=== Registered Handlers ({HandlerCount}) ===");
      foreach (var handler in _handlers) {
        Debug.Log($"- {handler.HandlerName} (Priority: {handler.Priority}, Enabled: {handler.IsEnabled})");
      }
      Debug.Log("=== End of Handlers ===");
    }

    [ContextMenu("Clear Handlers")]
    private void ClearHandlers() {
      _handlers.Clear();
      _areHandlersDiscovered = false;
      LogDebug("Cleared all registered handlers");
    }

    [ContextMenu("Clear Event Mappings")]
    private void ClearEventMappings() {
      _eventToStateMap.Clear();
      _areMappingsBuilt = false;
      LogDebug("Cleared all event-to-state mappings");
    }

    [ContextMenu("Clear Internal Events")]
    private void RemoveAllInternalListeners() {
      _internalEventChannel.RemoveAllListeners();
      _isSubcribedToInternalEvents = false;
      LogDebug("Cleared all internal event handlers");
    }

    [ContextMenu("Clear External Events")]
    private void RemoveAllExternalListeners() {
      _externalEventChannel.RemoveAllListeners();
      _isSubcribedToExternalEvents = false;
      LogDebug("Cleared all external event handlers");
    }

    #endregion
  }

  [System.Serializable]
  public class EventToStateMapping {
    public GameEventType EventType;
    public GameStates TargetState;
  }
}