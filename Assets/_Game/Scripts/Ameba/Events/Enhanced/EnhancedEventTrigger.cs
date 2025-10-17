using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ameba.Events {

  /// <summary>
  /// Component that allows designers to trigger events on an EnhancedEventChannel.
  /// Can be used on any GameObject to trigger events with various payloads.
  /// </summary>
  [AddComponentMenu("Ameba/Events/Event Trigger")]
  public class EnhancedEventTrigger : MonoBehaviour {

    [Header("Event Channel")]
    [SerializeField] private EnhancedEventChannel _eventChannel;

    [Header("Event Configuration")]
    [SerializeField] private EventIdentifierType _identifierType = EventIdentifierType.String;
    
    [SerializeField] private int _intIdentifier = 0;
    [SerializeField] private string _stringIdentifier = "MyEvent";
    [SerializeField] private string _enumTypeName = "";
    [SerializeField] private string _enumValueName = "";

    [Header("Payload Configuration")]
    [SerializeField] private PayloadConfiguration _payloadConfig = PayloadConfiguration.Void;
    
    [SerializeField] private int _intPayload = 0;
    [SerializeField] private bool _boolPayload = false;
    [SerializeField] private float _floatPayload = 0f;
    [SerializeField] private string _stringPayload = "";

    [Header("Trigger Settings")]
    [SerializeField] private bool _triggerOnStart = false;
    [SerializeField] private bool _triggerOnEnable = false;
    [SerializeField] private float _delayBeforeTrigger = 0f;

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogging = false;

    [Header("Unity Events")]
    [SerializeField] private UnityEvent _onEventTriggered = new();

    // Cached event identifier
    private EventIdentifier? _cachedEventIdentifier;

    private enum PayloadConfiguration {
      Void,
      Int,
      Bool,
      Float,
      String,
      CustomEventArgs
    }

    #region Unity Lifecycle

    private void Start() {
      if (_triggerOnStart) {
        TriggerEventWithDelay();
      }
    }

    private void OnEnable() {
      if (_triggerOnEnable) {
        TriggerEventWithDelay();
      }
    }

    private void OnValidate() {
      // Clear cached identifier when configuration changes
      _cachedEventIdentifier = null;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Triggers the configured event immediately.
    /// </summary>
    [ContextMenu("Trigger Event")]
    public void TriggerEvent() {
      if (_eventChannel == null) {
        LogWarning("Cannot trigger event: Event Channel is not assigned");
        return;
      }

      var eventId = GetEventIdentifier();
      
      try {
        switch (_payloadConfig) {
          case PayloadConfiguration.Void:
            _eventChannel.Raise(eventId);
            Log($"Triggered void event: {eventId}");
            break;
            
          case PayloadConfiguration.Int:
            _eventChannel.Raise(eventId, _intPayload);
            Log($"Triggered int event: {eventId} with payload: {_intPayload}");
            break;
            
          case PayloadConfiguration.Bool:
            _eventChannel.Raise(eventId, _boolPayload);
            Log($"Triggered bool event: {eventId} with payload: {_boolPayload}");
            break;
            
          case PayloadConfiguration.Float:
            _eventChannel.Raise(eventId, _floatPayload);
            Log($"Triggered float event: {eventId} with payload: {_floatPayload}");
            break;
            
          case PayloadConfiguration.String:
            _eventChannel.Raise(eventId, _stringPayload);
            Log($"Triggered string event: {eventId} with payload: {_stringPayload}");
            break;
            
          case PayloadConfiguration.CustomEventArgs:
            var customArgs = new GenericEventArgs(_stringPayload);
            _eventChannel.Raise(eventId, customArgs);
            Log($"Triggered EventArgs event: {eventId} with message: {_stringPayload}");
            break;
        }

        _onEventTriggered.Invoke();
      }
      catch (Exception ex) {
        LogError($"Error triggering event {eventId}: {ex.Message}");
      }
    }

    /// <summary>
    /// Triggers the event with a custom int payload.
    /// </summary>
    public void TriggerEvent(int payload) {
      if (_eventChannel == null) return;
      
      var eventId = GetEventIdentifier();
      _eventChannel.Raise(eventId, payload);
      Log($"Triggered int event: {eventId} with custom payload: {payload}");
      _onEventTriggered.Invoke();
    }

    /// <summary>
    /// Triggers the event with a custom bool payload.
    /// </summary>
    public void TriggerEvent(bool payload) {
      if (_eventChannel == null) return;
      
      var eventId = GetEventIdentifier();
      _eventChannel.Raise(eventId, payload);
      Log($"Triggered bool event: {eventId} with custom payload: {payload}");
      _onEventTriggered.Invoke();
    }

    /// <summary>
    /// Triggers the event with a custom float payload.
    /// </summary>
    public void TriggerEvent(float payload) {
      if (_eventChannel == null) return;
      
      var eventId = GetEventIdentifier();
      _eventChannel.Raise(eventId, payload);
      Log($"Triggered float event: {eventId} with custom payload: {payload}");
      _onEventTriggered.Invoke();
    }

    /// <summary>
    /// Triggers the event with a custom string payload.
    /// </summary>
    public void TriggerEvent(string payload) {
      if (_eventChannel == null) return;
      
      var eventId = GetEventIdentifier();
      _eventChannel.Raise(eventId, payload ?? "");
      Log($"Triggered string event: {eventId} with custom payload: {payload}");
      _onEventTriggered.Invoke();
    }

    /// <summary>
    /// Triggers the event after the configured delay.
    /// </summary>
    public void TriggerEventWithDelay() {
      if (_delayBeforeTrigger > 0f) {
        Invoke(nameof(TriggerEvent), _delayBeforeTrigger);
      } else {
        TriggerEvent();
      }
    }

    /// <summary>
    /// Sets the event channel at runtime.
    /// </summary>
    public void SetEventChannel(EnhancedEventChannel eventChannel) {
      _eventChannel = eventChannel;
      _cachedEventIdentifier = null;
    }

    /// <summary>
    /// Sets the event identifier at runtime.
    /// </summary>
    public void SetEventIdentifier(string identifier) {
      _identifierType = EventIdentifierType.String;
      _stringIdentifier = identifier;
      _cachedEventIdentifier = null;
    }

    /// <summary>
    /// Sets the event identifier at runtime.
    /// </summary>
    public void SetEventIdentifier(int identifier) {
      _identifierType = EventIdentifierType.Int;
      _intIdentifier = identifier;
      _cachedEventIdentifier = null;
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

    [ContextMenu("Test Event Configuration")]
    private void TestEventConfiguration() {
      Debug.Log("=== Event Configuration Test ===");
      Debug.Log($"Event Channel: {(_eventChannel != null ? _eventChannel.name : "Not Assigned")}");
      Debug.Log($"Identifier Type: {_identifierType}");
      Debug.Log($"Event Identifier: {GetEventIdentifier()}");
      Debug.Log($"Payload Configuration: {_payloadConfig}");
      
      switch (_payloadConfig) {
        case PayloadConfiguration.Int:
          Debug.Log($"Int Payload: {_intPayload}");
          break;
        case PayloadConfiguration.Bool:
          Debug.Log($"Bool Payload: {_boolPayload}");
          break;
        case PayloadConfiguration.Float:
          Debug.Log($"Float Payload: {_floatPayload}");
          break;
        case PayloadConfiguration.String:
        case PayloadConfiguration.CustomEventArgs:
          Debug.Log($"String Payload: {_stringPayload}");
          break;
      }
      
      Debug.Log("=== End Test ===");
    }

    #endregion
  }

  /// <summary>
  /// Generic EventArgs implementation for custom payloads.
  /// </summary>
  [Serializable]
  public class GenericEventArgs : EventArgs {
    public string Message { get; }
    public DateTime Timestamp { get; }

    public GenericEventArgs(string message) {
      Message = message ?? string.Empty;
      Timestamp = DateTime.Now;
    }

    public override string ToString() {
      return $"GenericEventArgs(Message: {Message}, Timestamp: {Timestamp})";
    }
  }
}