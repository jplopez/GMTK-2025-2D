using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ameba.Events {

  /// <summary>
  /// Component that allows designers to listen to events from an EnhancedEventChannel.
  /// Can be used on any GameObject to react to events with various payloads.
  /// </summary>
  [AddComponentMenu("Ameba/Events/Event Listener")]
  public class EnhancedEventListener : MonoBehaviour {

    [Header("Event Channel")]
    [SerializeField] private EnhancedEventChannel _eventChannel;

    [Header("Event Configuration")]
    [SerializeField] private EventIdentifierType _identifierType = EventIdentifierType.String;
    
    [SerializeField] private int _intIdentifier = 0;
    [SerializeField] private string _stringIdentifier = "MyEvent";
    [SerializeField] private string _enumTypeName = "";
    [SerializeField] private string _enumValueName = "";

    [Header("Payload Configuration")]
    [SerializeField] private PayloadConfiguration _expectedPayload = PayloadConfiguration.Void;

    [Header("Unity Events")]
    [SerializeField] private UnityEvent _onVoidEvent = new();
    [SerializeField] private UnityEvent<int> _onIntEvent = new();
    [SerializeField] private UnityEvent<bool> _onBoolEvent = new();
    [SerializeField] private UnityEvent<float> _onFloatEvent = new();
    [SerializeField] private UnityEvent<string> _onStringEvent = new();
    [SerializeField] private UnityEvent<string> _onEventArgsEvent = new(); // EventArgs message

    [Header("Settings")]
    [SerializeField] private bool _autoSubscribeOnStart = true;
    [SerializeField] private bool _autoUnsubscribeOnDestroy = true;
    [SerializeField] private bool _enableDebugLogging = false;

    // Cached event identifier
    private EventIdentifier? _cachedEventIdentifier;
    private bool _isSubscribed = false;

    private enum PayloadConfiguration {
      Void,
      Int,
      Bool,
      Float,
      String,
      EventArgs
    }

    #region Unity Lifecycle

    private void Start() {
      if (_autoSubscribeOnStart) {
        Subscribe();
      }
    }

    private void OnDestroy() {
      if (_autoUnsubscribeOnDestroy) {
        Unsubscribe();
      }
    }

    private void OnValidate() {
      // Clear cached identifier when configuration changes
      _cachedEventIdentifier = null;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Subscribes to the configured event.
    /// </summary>
    [ContextMenu("Subscribe")]
    public void Subscribe() {
      if (_eventChannel == null) {
        LogWarning("Cannot subscribe: Event Channel is not assigned");
        return;
      }

      if (_isSubscribed) {
        LogWarning("Already subscribed to event");
        return;
      }

      var eventId = GetEventIdentifier();

      try {
        switch (_expectedPayload) {
          case PayloadConfiguration.Void:
            _eventChannel.AddListener(eventId, OnVoidEventReceived);
            break;
            
          case PayloadConfiguration.Int:
            _eventChannel.AddListener<int>(eventId, OnIntEventReceived);
            break;
            
          case PayloadConfiguration.Bool:
            _eventChannel.AddListener<bool>(eventId, OnBoolEventReceived);
            break;
            
          case PayloadConfiguration.Float:
            _eventChannel.AddListener<float>(eventId, OnFloatEventReceived);
            break;
            
          case PayloadConfiguration.String:
            _eventChannel.AddListener<string>(eventId, OnStringEventReceived);
            break;
            
          case PayloadConfiguration.EventArgs:
            _eventChannel.AddListener<GenericEventArgs>(eventId, OnEventArgsReceived);
            break;
        }

        _isSubscribed = true;
        Log($"Subscribed to {_expectedPayload} event: {eventId}");
      }
      catch (Exception ex) {
        LogError($"Error subscribing to event {eventId}: {ex.Message}");
      }
    }

    /// <summary>
    /// Unsubscribes from the configured event.
    /// </summary>
    [ContextMenu("Unsubscribe")]
    public void Unsubscribe() {
      if (_eventChannel == null || !_isSubscribed) {
        return;
      }

      var eventId = GetEventIdentifier();

      try {
        switch (_expectedPayload) {
          case PayloadConfiguration.Void:
            _eventChannel.RemoveListener(eventId, OnVoidEventReceived);
            break;
            
          case PayloadConfiguration.Int:
            _eventChannel.RemoveListener<int>(eventId, OnIntEventReceived);
            break;
            
          case PayloadConfiguration.Bool:
            _eventChannel.RemoveListener<bool>(eventId, OnBoolEventReceived);
            break;
            
          case PayloadConfiguration.Float:
            _eventChannel.RemoveListener<float>(eventId, OnFloatEventReceived);
            break;
            
          case PayloadConfiguration.String:
            _eventChannel.RemoveListener<string>(eventId, OnStringEventReceived);
            break;
            
          case PayloadConfiguration.EventArgs:
            _eventChannel.RemoveListener<GenericEventArgs>(eventId, OnEventArgsReceived);
            break;
        }

        _isSubscribed = false;
        Log($"Unsubscribed from {_expectedPayload} event: {eventId}");
      }
      catch (Exception ex) {
        LogError($"Error unsubscribing from event {eventId}: {ex.Message}");
      }
    }

    /// <summary>
    /// Sets the event channel at runtime.
    /// </summary>
    public void SetEventChannel(EnhancedEventChannel eventChannel) {
      if (_isSubscribed) {
        Unsubscribe();
      }
      
      _eventChannel = eventChannel;
      _cachedEventIdentifier = null;
      
      if (_autoSubscribeOnStart && eventChannel != null) {
        Subscribe();
      }
    }

    /// <summary>
    /// Sets the event identifier at runtime.
    /// </summary>
    public void SetEventIdentifier(string identifier) {
      if (_isSubscribed) {
        Unsubscribe();
      }
      
      _identifierType = EventIdentifierType.String;
      _stringIdentifier = identifier;
      _cachedEventIdentifier = null;
      
      if (_autoSubscribeOnStart && _eventChannel != null) {
        Subscribe();
      }
    }

    /// <summary>
    /// Sets the event identifier at runtime.
    /// </summary>
    public void SetEventIdentifier(int identifier) {
      if (_isSubscribed) {
        Unsubscribe();
      }
      
      _identifierType = EventIdentifierType.Int;
      _intIdentifier = identifier;
      _cachedEventIdentifier = null;
      
      if (_autoSubscribeOnStart && _eventChannel != null) {
        Subscribe();
      }
    }

    #endregion

    #region Event Handlers

    private void OnVoidEventReceived() {
      Log("Void event received");
      _onVoidEvent.Invoke();
    }

    private void OnIntEventReceived(int payload) {
      Log($"Int event received with payload: {payload}");
      _onIntEvent.Invoke(payload);
    }

    private void OnBoolEventReceived(bool payload) {
      Log($"Bool event received with payload: {payload}");
      _onBoolEvent.Invoke(payload);
    }

    private void OnFloatEventReceived(float payload) {
      Log($"Float event received with payload: {payload}");
      _onFloatEvent.Invoke(payload);
    }

    private void OnStringEventReceived(string payload) {
      Log($"String event received with payload: {payload}");
      _onStringEvent.Invoke(payload);
    }

    private void OnEventArgsReceived(GenericEventArgs eventArgs) {
      Log($"EventArgs received with message: {eventArgs.Message}");
      _onEventArgsEvent.Invoke(eventArgs.Message);
    }

    #endregion

    #region Private Methods

    private EventIdentifier GetEventIdentifier() {
      if (_cachedEventIdentifier.HasValue) {
        return _cachedEventIdentifier.Value;
      }

      EventIdentifier eventId = _identifierType switch {
        EventIdentifierType.Int => new EventIdentifier(_intIdentifier),
        EventIdentifierType.String => new EventIdentifier(_stringIdentifier),
        EventIdentifierType.Enum => CreateEnumIdentifier(),
        _ => new EventIdentifier(_stringIdentifier)
      };

      _cachedEventIdentifier = eventId;
      return eventId;
    }

    private EventIdentifier CreateEnumIdentifier() {
      if (string.IsNullOrEmpty(_enumTypeName) || string.IsNullOrEmpty(_enumValueName)) {
        LogWarning("Enum type name or value name is empty, falling back to string identifier");
        return new EventIdentifier(_stringIdentifier);
      }

      try {
        var enumType = Type.GetType(_enumTypeName);
        if (enumType == null || !enumType.IsEnum) {
          LogWarning($"Could not find enum type {_enumTypeName}, falling back to string identifier");
          return new EventIdentifier(_stringIdentifier);
        }

        var enumValue = Enum.Parse(enumType, _enumValueName);
        return new EventIdentifier((Enum)enumValue);
      }
      catch (Exception ex) {
        LogWarning($"Error creating enum identifier: {ex.Message}, falling back to string identifier");
        return new EventIdentifier(_stringIdentifier);
      }
    }

    #endregion

    #region Debug and Logging

    private void Log(string message) {
      if (_enableDebugLogging) {
        Debug.Log($"[{gameObject.name}] {message}");
      }
    }

    private void LogWarning(string message) {
      if (_enableDebugLogging) {
        Debug.LogWarning($"[{gameObject.name}] {message}");
      }
    }

    private void LogError(string message) {
      Debug.LogError($"[{gameObject.name}] {message}");
    }

    #endregion

    #region Context Menu Actions

    [ContextMenu("Test Listener Configuration")]
    private void TestListenerConfiguration() {
      Debug.Log("=== Listener Configuration Test ===");
      Debug.Log($"Event Channel: {(_eventChannel != null ? _eventChannel.name : "Not Assigned")}");
      Debug.Log($"Identifier Type: {_identifierType}");
      Debug.Log($"Event Identifier: {GetEventIdentifier()}");
      Debug.Log($"Expected Payload: {_expectedPayload}");
      Debug.Log($"Is Subscribed: {_isSubscribed}");
      Debug.Log("=== End Test ===");
    }

    #endregion
  }
}