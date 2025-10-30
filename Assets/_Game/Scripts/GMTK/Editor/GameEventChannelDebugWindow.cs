#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Ameba;
using Object = UnityEngine.Object;

namespace GMTK.Editor {

  /// <summary>
  /// Unity Editor window for debugging GameEventChannel events and listeners in real-time.
  /// Shows fired events, registered listeners, and allows manual event triggering.
  /// </summary>
  [EditorWindowTitle(icon ="gear", title ="Game Event Channel Debug")]
  public class GameEventChannelDebugWindow : EditorWindow {

    #region Window Management

    [MenuItem("GMTK/Debug/Game Event Channel Debug")]
    public static void ShowWindow() {
      var window = GetWindow<GameEventChannelDebugWindow>();
      window.titleContent = new GUIContent("Event Channel Debug");
      window.minSize = new Vector2(600, 400);
      window.Show();
    }

    #endregion

    #region Private Fields

    private GameEventChannel _gameEventChannel;
    private Vector2 _eventsScrollPosition;
    private Vector2 _listenersScrollPosition;
    private Vector2 _triggerScrollPosition;

    // Event logging
    private List<EventLogEntry> _eventLog = new List<EventLogEntry>();
    private const int MaxLogEntries = 100;
    private bool _isLogging = true;
    private bool _autoScroll = true;

    // UI State
    private int _selectedTabIndex = 0;
    private readonly string[] _tabNames = { "Events Log", "Active Listeners", "Manual Trigger" };
    private bool _showSystemEvents = true;
    private bool _showPlayableElementEvents = true;
    private bool _showInputEvents = true;
    private GameEventType _selectedEventToTrigger = GameEventType.GameStarted;
    private string _payloadString = "";

    // Search functionality
    private string _eventsSearchPattern = "";
    private string _listenersSearchPattern = "";
    private string _triggerSearchPattern = "";
    private Regex _eventsRegex;
    private Regex _listenersRegex;
    private Regex _triggerRegex;
    private bool _isEventsRegexValid = true;
    private bool _isListenersRegexValid = true;
    private bool _isTriggerRegexValid = true;

    // Reflection cache
    private Dictionary<GameEventChannel, Dictionary<GameEventType, List<ListenerInfo>>> _listenersCache =
      new Dictionary<GameEventChannel, Dictionary<GameEventType, List<ListenerInfo>>>();
    private FieldInfo _callbacksField;
    private bool _reflectionInitialized = false;

    // Styles
    private GUIStyle _logEntryStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _columnHeaderStyle;
    private GUIStyle _evenRowStyle;
    private GUIStyle _oddRowStyle;
    private GUIStyle _searchBoxStyle;
    private GUIStyle _errorSearchBoxStyle;
    private bool _stylesInitialized = false;

    #endregion

    #region Unity Editor Callbacks

    private void OnEnable() {
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
      InitializeReflection();
      FindGameEventChannel();
    }

    private void OnDisable() {
      EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
      UnsubscribeFromEvents();
    }

    private void OnGUI() {
      if (!_stylesInitialized) {
        InitializeStyles();
      }

      DrawHeader();
      DrawTabs();

      switch (_selectedTabIndex) {
        case 0:
          DrawEventsLog();
          break;
        case 1:
          DrawActiveListeners();
          break;
        case 2:
          DrawManualTrigger();
          break;
      }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state) {
      if (state == PlayModeStateChange.EnteredPlayMode) {
        FindGameEventChannel();
        _eventLog.Clear();
      }
      else if (state == PlayModeStateChange.ExitingPlayMode) {
        UnsubscribeFromEvents();
      }
    }

    #endregion

    #region Initialization

    private void InitializeStyles() {
      _headerStyle = new GUIStyle(EditorStyles.boldLabel) {
        fontSize = 14,
        normal = { textColor = Color.white }
      };

      _columnHeaderStyle = new GUIStyle(EditorStyles.boldLabel) {
        fontSize = 10,
        normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black },
        alignment = TextAnchor.MiddleLeft,
        padding = new RectOffset(5, 5, 2, 2)
      };

      _logEntryStyle = new GUIStyle(EditorStyles.label) {
        fontSize = 11,
        wordWrap = false,
        richText = true
      };

      _evenRowStyle = new GUIStyle() {
        normal = { background = MakeTexture(Color.gray * 0.1f) }
      };

      _oddRowStyle = new GUIStyle() {
        normal = { background = MakeTexture(Color.gray * 0.05f) }
      };

      _searchBoxStyle = new GUIStyle(EditorStyles.textField) {
        fontSize = 11
      };

      _errorSearchBoxStyle = new GUIStyle(EditorStyles.textField) {
        fontSize = 11,
        normal = { textColor = Color.red },
        focused = { textColor = Color.red }
      };

      _stylesInitialized = true;
    }

    private void InitializeReflection() {
      try {
        Type eventChannelType = typeof(EventChannel<GameEventType>);
        _callbacksField = eventChannelType.GetField("callbacks", BindingFlags.NonPublic | BindingFlags.Instance);
        _reflectionInitialized = (_callbacksField != null);

        if (!_reflectionInitialized) {
          Debug.LogWarning("[EventChannelDebug] Could not initialize reflection for EventChannel callbacks");
        }
      }
      catch (Exception ex) {
        Debug.LogError($"[EventChannelDebug] Error initializing reflection: {ex.Message}");
        _reflectionInitialized = false;
      }
    }

    private void FindGameEventChannel() {
      UnsubscribeFromEvents();

      if (Application.isPlaying) {
        // Try to get from ServiceLocator first
        _gameEventChannel = ServiceLocator.Get<GameEventChannel>();

        // Fallback: search in scene
        if (_gameEventChannel == null) {
          var channels = FindObjectsByType<GameEventChannel>(FindObjectsSortMode.None);
          if (channels.Length > 0) {
            _gameEventChannel = channels[0];
          }
        }
      }
      else {
        // In edit mode, find ScriptableObject assets
        string[] guids = AssetDatabase.FindAssets("t:GameEventChannel");
        if (guids.Length > 0) {
          string path = AssetDatabase.GUIDToAssetPath(guids[0]);
          _gameEventChannel = AssetDatabase.LoadAssetAtPath<GameEventChannel>(path);
        }
      }

      if (_gameEventChannel != null) {
        SubscribeToEvents();
        RefreshListenersCache();
      }
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents() {
      if (_gameEventChannel == null || !Application.isPlaying) return;

      // Subscribe to all GameEventTypes to capture fired events
      foreach (GameEventType eventType in Enum.GetValues(typeof(GameEventType))) {
        try {
          _gameEventChannel.AddListener(eventType, () => LogEvent(eventType, null, "void"));
          _gameEventChannel.AddListener<PlayableElementEventArgs>(eventType, (args) => LogEvent(eventType, args, typeof(PlayableElementEventArgs).Name));
          _gameEventChannel.AddListener<InputActionEventArgs>(eventType, (args) => LogEvent(eventType, args, typeof(InputActionEventArgs).Name));
          _gameEventChannel.AddListener<StateMachineEventArg<GameStates>>(eventType, (args) => LogEvent(eventType, args, typeof(StateMachineEventArg<GameStates>).Name));
        }
        catch (Exception ex) {
          Debug.LogWarning($"[EventChannelDebug] Could not subscribe to {eventType}: {ex.Message}");
        }
      }
    }

    private void UnsubscribeFromEvents() {
      if (_gameEventChannel == null || !Application.isPlaying) return;

      // Note: In a production system, you'd want to store references to the actual callback methods
      // For debugging purposes, we'll just clear our log when unsubscribing
      _eventLog.Clear();
    }

    #endregion

    #region Event Logging

    private void LogEvent(GameEventType eventType, object payload, string payloadType) {
      if (!_isLogging) return;

      var logEntry = new EventLogEntry {
        EventType = eventType,
        Payload = payload,
        PayloadType = payloadType,
        Timestamp = DateTime.Now,
        FrameCount = Time.frameCount
      };

      _eventLog.Insert(0, logEntry); // Add to front for reverse chronological order

      // Limit log size
      if (_eventLog.Count > MaxLogEntries) {
        _eventLog.RemoveAt(_eventLog.Count - 1);
      }

      // Auto-refresh listeners cache periodically
      if (_eventLog.Count % 10 == 0) {
        RefreshListenersCache();
      }

      Repaint();
    }

    #endregion

    #region Listeners Cache

    private void RefreshListenersCache() {
      _listenersCache.Clear();

      if (_gameEventChannel == null || !_reflectionInitialized) return;

      try {
        // Get the private callbacks dictionary via reflection, return if is null or invalid
        if (_callbacksField.GetValue(_gameEventChannel) is not Dictionary<GameEventType, List<IEventCallback>> callbacks) return;

        var listenersForChannel = new Dictionary<GameEventType, List<ListenerInfo>>();

        foreach (var kvp in callbacks) {
          var eventType = kvp.Key;
          var callbackList = kvp.Value;

          var listeners = new List<ListenerInfo>();

          foreach (var callback in callbackList) {
            var listenerInfo = ExtractListenerInfo(callback);
            if (listenerInfo != null) {
              listeners.Add(listenerInfo);
            }
          }

          if (listeners.Count > 0) {
            listenersForChannel[eventType] = listeners;
          }
        }

        if (listenersForChannel.Count > 0) {
          _listenersCache[_gameEventChannel] = listenersForChannel;
        }
      }
      catch (Exception ex) {
        Debug.LogError($"[EventChannelDebug] Error refreshing listeners cache: {ex.Message}");
      }
    }

    private ListenerInfo ExtractListenerInfo(IEventCallback callback) {
      try {
        var listenerInfo = new ListenerInfo {
          PayloadType = callback.PayloadType.Name,
          CallbackType = callback.GetType().Name
        };

        // Try to get more specific information through reflection
        if (callback is VoidEventCallback voidCallback) {
          PropertyInfo actionField = typeof(VoidEventCallback).GetProperty("Action");
          if (actionField != null) {
            if (actionField.GetValue(voidCallback) is Action action) {
              listenerInfo.TargetObject = action.Target?.ToString() ?? "Static";
              listenerInfo.MethodName = action.Method.Name;
              listenerInfo.DeclaringType = action.Method.DeclaringType?.Name ?? "Unknown";
            }
          }
        }
        else {
          // For generic EventCallback<T>, we need to use reflection to get the Action
          PropertyInfo actionProperty = callback.GetType().GetProperty("Action");
          if (actionProperty != null) {
            var actionValue = actionProperty.GetValue(callback);
            if (actionValue != null) {
              var methodInfo = actionValue.GetType().GetMethod("get_Method");
              if (methodInfo != null) {
                var method = methodInfo.Invoke(actionValue, null) as MethodInfo;
                if (method != null) {
                  listenerInfo.MethodName = method.Name;
                  listenerInfo.DeclaringType = method.DeclaringType?.Name ?? "Unknown";

                  PropertyInfo targetProperty = actionValue.GetType().GetProperty("Target");
                  if (targetProperty != null) {
                    var target = targetProperty.GetValue(actionValue);
                    listenerInfo.TargetObject = target?.ToString() ?? "Static";
                  }
                }
              }
            }
          }
        }

        return listenerInfo;
      }
      catch (Exception ex) {
        Debug.LogWarning($"[EventChannelDebug] Error extracting listener info: {ex.Message}");
        return new ListenerInfo {
          PayloadType = callback.PayloadType.Name,
          CallbackType = callback.GetType().Name,
          TargetObject = "Unknown",
          MethodName = "Unknown",
          DeclaringType = "Unknown"
        };
      }
    }

    #endregion

    #region Search/Regex Functionality

    private void UpdateEventsRegex(string pattern) {
      try {
        if (string.IsNullOrEmpty(pattern)) {
          _eventsRegex = null;
          _isEventsRegexValid = true;
        }
        else {
          _eventsRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
          _isEventsRegexValid = true;
        }
      }
      catch (ArgumentException) {
        _eventsRegex = null;
        _isEventsRegexValid = false;
      }
    }

    private void UpdateListenersRegex(string pattern) {
      try {
        if (string.IsNullOrEmpty(pattern)) {
          _listenersRegex = null;
          _isListenersRegexValid = true;
        }
        else {
          _listenersRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
          _isListenersRegexValid = true;
        }
      }
      catch (ArgumentException) {
        _listenersRegex = null;
        _isListenersRegexValid = false;
      }
    }

    private void UpdateTriggerRegex(string pattern) {
      try {
        if (string.IsNullOrEmpty(pattern)) {
          _triggerRegex = null;
          _isTriggerRegexValid = true;
        }
        else {
          _triggerRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
          _isTriggerRegexValid = true;
        }
      }
      catch (ArgumentException) {
        _triggerRegex = null;
        _isTriggerRegexValid = false;
      }
    }

    private bool EventMatchesSearch(EventLogEntry entry) {
      if (_eventsRegex == null) return true;

      // Search in event type, payload type, and payload preview
      string searchText = $"{entry.EventType} {entry.PayloadType} {GetPayloadPreview(entry.Payload)}";
      return _eventsRegex.IsMatch(searchText);
    }

    private bool ListenerMatchesSearch(GameEventType eventType, ListenerInfo listener) {
      if (_listenersRegex == null) return true;

      // Search in event type, declaring type, method name, target object, and payload type
      string searchText = $"{eventType} {listener.DeclaringType} {listener.MethodName} {listener.TargetObject} {listener.PayloadType}";
      return _listenersRegex.IsMatch(searchText);
    }

    private bool EventTypeMatchesTriggerSearch(GameEventType eventType) {
      if (_triggerRegex == null) return true;

      return _triggerRegex.IsMatch(eventType.ToString());
    }

    #endregion

    #region GUI Drawing

    private void DrawHeader() {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      EditorGUILayout.LabelField("Game Event Channel Debugger", _headerStyle);

      EditorGUILayout.BeginHorizontal();

      // Event Channel Status
      string status = _gameEventChannel != null ?
        $"Connected: {_gameEventChannel.name}" :
        "No Event Channel Found";
      EditorGUILayout.LabelField($"Status: {status}");

      if (GUILayout.Button("Refresh", GUILayout.Width(100))) {
        FindGameEventChannel();
      }

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.BeginHorizontal();

      // Logging controls
      bool wasLogging = _isLogging;
      _isLogging = EditorGUILayout.Toggle("Enable Logging", _isLogging);

      if (_isLogging != wasLogging && _gameEventChannel != null) {
        if (_isLogging) {
          SubscribeToEvents();
        }
        else {
          UnsubscribeFromEvents();
        }
      }

      _autoScroll = EditorGUILayout.Toggle("Auto Scroll", _autoScroll);

      if (GUILayout.Button("Clear Log", GUILayout.Width(100))) {
        _eventLog.Clear();
      }

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.EndVertical();
    }

    private void DrawTabs() {
      _selectedTabIndex = GUILayout.Toolbar(_selectedTabIndex, _tabNames);
      EditorGUILayout.Space();
    }

    private void DrawSearchBox(string label, ref string searchPattern, ref bool isRegexValid, System.Action<string> updateRegexAction) {
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField(label, GUILayout.Width(50));
      
      var style = isRegexValid ? _searchBoxStyle : _errorSearchBoxStyle;
      string newPattern = EditorGUILayout.TextField(searchPattern, style);
      
      if (newPattern != searchPattern) {
        searchPattern = newPattern;
        updateRegexAction(searchPattern);
      }

      if (GUILayout.Button("Clear", GUILayout.Width(50))) {
        searchPattern = "";
        updateRegexAction(searchPattern);
      }

      EditorGUILayout.EndHorizontal();

      if (!isRegexValid) {
        EditorGUILayout.HelpBox("Invalid regex pattern", MessageType.Warning);
      }
    }

    const int F = 20;
    const int XS = F*3;
    const int S = F*4;
    const int M = F*6;
    const int L = F*8;
    const int XL = F*10;

    private void DrawEventsLogColumnHeaders(int[] widths) {
      EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
      
      EditorGUILayout.LabelField("Time", _columnHeaderStyle, GUILayout.Width(widths[0]));
      EditorGUILayout.LabelField("Frame", _columnHeaderStyle, GUILayout.Width(widths[1]));
      EditorGUILayout.LabelField("Event Type", _columnHeaderStyle, GUILayout.Width(widths[2]));
      EditorGUILayout.LabelField("Payload Type", _columnHeaderStyle, GUILayout.Width(widths[3]));
      EditorGUILayout.LabelField("Payload Preview", _columnHeaderStyle);
      
      EditorGUILayout.EndHorizontal();
    }

    private void DrawEventsLog() {
      int[] width = { XS, S, M, L, XL };
      EditorGUILayout.BeginVertical();

      // Search box for events
      DrawSearchBox("Search:", ref _eventsSearchPattern, ref _isEventsRegexValid, UpdateEventsRegex);
      EditorGUILayout.Space(5);

      // Event filters
      EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
      EditorGUILayout.LabelField("Filters:", GUILayout.Width(S));
      _showSystemEvents = EditorGUILayout.ToggleLeft("System", _showSystemEvents, GUILayout.Width(M));
      _showPlayableElementEvents = EditorGUILayout.ToggleLeft("Elements", _showPlayableElementEvents, GUILayout.Width(M));
      _showInputEvents = EditorGUILayout.ToggleLeft("Input", _showInputEvents, GUILayout.Width(M));
      EditorGUILayout.EndHorizontal();

      var filteredEvents = GetFilteredEvents();
      EditorGUILayout.LabelField($"Events ({filteredEvents.Count}/{_eventLog.Count}/{MaxLogEntries})", EditorStyles.boldLabel);

      // Draw column headers
      DrawEventsLogColumnHeaders(width);

      _eventsScrollPosition = EditorGUILayout.BeginScrollView(_eventsScrollPosition);

      if (filteredEvents.Count == 0) {
        string message = _eventLog.Count == 0 ? 
          "No events logged yet. Events will appear here when the game is running and events are fired." :
          "No events match the current search pattern and filters.";
        EditorGUILayout.HelpBox(message, MessageType.Info);
      }
      else {
        for (int i = 0; i < filteredEvents.Count; i++) {
          var entry = filteredEvents[i];
          var style = (i % 2 == 0) ? _evenRowStyle : _oddRowStyle;

          EditorGUILayout.BeginHorizontal(style);

          // Timestamp
          EditorGUILayout.LabelField(entry.Timestamp.ToString("HH:mm:ss.fff"), GUILayout.Width(width[0]));

          // Frame count
          EditorGUILayout.LabelField($"F{entry.FrameCount}", GUILayout.Width(width[1]));

          // Event Type with color coding
          string eventTypeDisplay = GetColoredEventType(entry.EventType);
          EditorGUILayout.LabelField(eventTypeDisplay, _logEntryStyle, GUILayout.Width(width[2]));

          // Payload Type
          EditorGUILayout.LabelField(entry.PayloadType, GUILayout.Width(width[3]));

          // Payload Preview
          string payloadPreview = GetPayloadPreview(entry.Payload);
          EditorGUILayout.LabelField(payloadPreview, _logEntryStyle);

          EditorGUILayout.EndHorizontal();
        }
      }

      EditorGUILayout.EndScrollView();

      if (_autoScroll && _eventLog.Count > 0) {
        _eventsScrollPosition.y = 0; // Keep at top since we insert new events at the beginning
      }

      EditorGUILayout.EndVertical();
    }

    private void DrawListenersColumnHeaders(int[] widths) {
      EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
      
      EditorGUILayout.LabelField("Class", _columnHeaderStyle, GUILayout.Width(widths[0]));
      EditorGUILayout.LabelField("Method", _columnHeaderStyle, GUILayout.Width(widths[1]));
      EditorGUILayout.LabelField("Payload", _columnHeaderStyle, GUILayout.Width(widths[2]));
      EditorGUILayout.LabelField("Target Object", _columnHeaderStyle);
      
      EditorGUILayout.EndHorizontal();
    }

    private void DrawActiveListeners() {
      EditorGUILayout.LabelField("Active Listeners", EditorStyles.boldLabel);

      if (_gameEventChannel == null) {
        EditorGUILayout.HelpBox("No GameEventChannel found. Listeners information is not available.", MessageType.Warning);
        return;
      }

      if (!Application.isPlaying) {
        EditorGUILayout.HelpBox("Listener information is only available during play mode.", MessageType.Info);
        return;
      }

      // Search box for listeners
      DrawSearchBox("Search:", ref _listenersSearchPattern, ref _isListenersRegexValid, UpdateListenersRegex);
      EditorGUILayout.Space(5);

      _listenersScrollPosition = EditorGUILayout.BeginScrollView(_listenersScrollPosition);

      if (_listenersCache.ContainsKey(_gameEventChannel)) {
        var listeners = _listenersCache[_gameEventChannel];

        if (listeners.Count == 0) {
          EditorGUILayout.HelpBox("No active listeners found.", MessageType.Info);
        }
        else {
          int totalListeners = 0;
          int visibleListeners = 0;

          foreach (var kvp in listeners.OrderBy(x => x.Key.ToString())) {
            var eventType = kvp.Key;
            var listenerList = kvp.Value;
            totalListeners += listenerList.Count;

            // Filter listeners based on search
            var filteredListeners = listenerList.Where(listener => ListenerMatchesSearch(eventType, listener)).ToList();
            
            if (filteredListeners.Count == 0) continue;
            
            visibleListeners += filteredListeners.Count;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Event Type Header
            string headerText = $"{eventType} ({filteredListeners.Count}/{listenerList.Count} listeners)";
            EditorGUILayout.LabelField(headerText, EditorStyles.boldLabel);

            // Column headers for listeners
            int[] width = { XL, XL, XL };
            EditorGUI.indentLevel++;
            DrawListenersColumnHeaders(width);

            // Listeners for this event type
            foreach (var listener in filteredListeners) {
              EditorGUILayout.BeginHorizontal();

              EditorGUILayout.LabelField(listener.DeclaringType, GUILayout.Width(width[0]));
              EditorGUILayout.LabelField(listener.MethodName, GUILayout.Width(width[1]));
              EditorGUILayout.LabelField($"({listener.PayloadType})", GUILayout.Width(width[2]));
              EditorGUILayout.LabelField(listener.TargetObject, _logEntryStyle);

              EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
          }

          if (visibleListeners == 0 && totalListeners > 0) {
            EditorGUILayout.HelpBox("No listeners match the current search pattern.", MessageType.Info);
          }
        }
      }
      else {
        if (GUILayout.Button("Refresh Listeners")) {
          RefreshListenersCache();
        }
        EditorGUILayout.HelpBox("Click 'Refresh Listeners' to scan for active listeners.", MessageType.Info);
      }

      EditorGUILayout.EndScrollView();
    }

    private void DrawManualTrigger() {
      EditorGUILayout.LabelField("Manual Event Triggering", EditorStyles.boldLabel);

      if (_gameEventChannel == null) {
        EditorGUILayout.HelpBox("No GameEventChannel found. Cannot trigger events.", MessageType.Warning);
        return;
      }

      if (!Application.isPlaying) {
        EditorGUILayout.HelpBox("Events can only be triggered during play mode.", MessageType.Info);
        return;
      }

      _triggerScrollPosition = EditorGUILayout.BeginScrollView(_triggerScrollPosition);

      // Search box for event triggering
      DrawSearchBox("Search:", ref _triggerSearchPattern, ref _isTriggerRegexValid, UpdateTriggerRegex);
      EditorGUILayout.Space(5);

      // Event selection with search filtering
      var availableEvents = Enum.GetValues(typeof(GameEventType)).Cast<GameEventType>()
        .Where(EventTypeMatchesTriggerSearch).ToArray();

      if (availableEvents.Length == 0) {
        EditorGUILayout.HelpBox("No events match the search pattern.", MessageType.Info);
      }
      else {
        // Show filtered enum popup
        int currentIndex = Array.IndexOf(availableEvents, _selectedEventToTrigger);
        if (currentIndex == -1) currentIndex = 0;
        
        string[] eventNames = availableEvents.Select(e => e.ToString()).ToArray();
        int newIndex = EditorGUILayout.Popup("Event Type", currentIndex, eventNames);
        _selectedEventToTrigger = availableEvents[newIndex];

        // Payload input (basic string that we'll try to convert)
        EditorGUILayout.LabelField("Payload (optional):");
        _payloadString = EditorGUILayout.TextArea(_payloadString, GUILayout.Height(60));

        EditorGUILayout.Space();

        // Trigger buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Trigger Void Event")) {
          _gameEventChannel.Raise(_selectedEventToTrigger);
        }

        if (GUILayout.Button("Trigger with String Payload")) {
          _gameEventChannel.Raise(_selectedEventToTrigger, _payloadString);
        }

        EditorGUILayout.EndHorizontal();

        // Quick trigger buttons for common events
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Triggers:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Game Started")) {
          _gameEventChannel.Raise(GameEventType.GameStarted);
        }
        if (GUILayout.Button("Game Over")) {
          _gameEventChannel.Raise(GameEventType.GameOver);
        }
        EditorGUILayout.EndHorizontal();
      }

      EditorGUILayout.EndScrollView();
    }

    #endregion

    #region Helper Methods

    private List<EventLogEntry> GetFilteredEvents() {
      return _eventLog.Where(entry => {
        // Apply category filters
        if (!_showSystemEvents && IsSystemEvent(entry.EventType)) return false;
        if (!_showPlayableElementEvents && IsPlayableElementEvent(entry.EventType)) return false;
        if (!_showInputEvents && IsInputEvent(entry.EventType)) return false;
        
        // Apply search filter
        if (!EventMatchesSearch(entry)) return false;
        
        return true;
      }).ToList();
    }

    private bool IsSystemEvent(GameEventType eventType) {
      return eventType == GameEventType.GameStarted || eventType == GameEventType.GameOver;
    }

    private bool IsPlayableElementEvent(GameEventType eventType) {
      return eventType == GameEventType.PlayableElementEvent ||
             eventType == GameEventType.PlayableElementInternalEvent ||
             eventType == GameEventType.ElementSelected ||
             eventType == GameEventType.ElementDropped ||
             eventType == GameEventType.ElementHovered ||
             eventType == GameEventType.ElementUnhovered;
    }

    private bool IsInputEvent(GameEventType eventType) {
      return eventType == GameEventType.InputSelected ||
             eventType == GameEventType.InputSecondary ||
             eventType == GameEventType.InputPointerPosition ||
             eventType == GameEventType.InputRotateCW ||
             eventType == GameEventType.InputRotateCCW ||
             eventType == GameEventType.InputFlippedX ||
             eventType == GameEventType.InputFlippedY;
    }

    private string GetColoredEventType(GameEventType eventType) {
      if (IsSystemEvent(eventType)) {
        return $"<color=cyan>{eventType}</color>";
      }
      if (IsPlayableElementEvent(eventType)) {
        return $"<color=yellow>{eventType}</color>";
      }
      if (IsInputEvent(eventType)) {
        return $"<color=green>{eventType}</color>";
      }
      return eventType.ToString();
    }

    private string GetPayloadPreview(object payload) {
      if (payload == null) return "null";

      try {
        if (payload is PlayableElementEventArgs peArgs) {
          return $"Element: {peArgs.Element?.name ?? "null"}, Type: {peArgs.EventType}";
        }
        if (payload is InputActionEventArgs iaArgs) {
          return $"Input: {iaArgs.InputEvent}, Phase: {iaArgs.Phase}";
        }

        string str = payload.ToString();
        return str.Length > 50 ? str.Substring(0, 50) + "..." : str;
      }
      catch {
        return payload.GetType().Name;
      }
    }

    private Texture2D MakeTexture(Color color) {
      var texture = new Texture2D(1, 1);
      texture.SetPixel(0, 0, color);
      texture.Apply();
      return texture;
    }

    #endregion

    #region Data Structures

    private class EventLogEntry {
      public GameEventType EventType { get; set; }
      public object Payload { get; set; }
      public string PayloadType { get; set; }
      public DateTime Timestamp { get; set; }
      public int FrameCount { get; set; }
    }

    private class ListenerInfo {
      public string PayloadType { get; set; }
      public string CallbackType { get; set; }
      public string TargetObject { get; set; }
      public string MethodName { get; set; }
      public string DeclaringType { get; set; }
    }

    #endregion
  }
}
#endif