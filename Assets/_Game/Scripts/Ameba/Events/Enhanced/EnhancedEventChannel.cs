using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ameba.Events {

  /// <summary>
  /// Enhanced event channel that eliminates the need for generics by using EventIdentifier.
  /// Allows usage of int, strings, and enums as event identifiers with various payload types.
  /// Game designers can create this as a ScriptableObject and use with EnhancedEventTrigger/EnhancedEventListener components.
  /// </summary>
  [CreateAssetMenu(fileName = "EnhancedEventChannel", menuName = "Ameba/Events/Event Channel", order = 1)]
  public class EnhancedEventChannel : EventChannel<EventIdentifierType> {

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLogging = false;

    protected Dictionary<int, List<IEventCallback>> _intCallbacks = new();
    protected Dictionary<string, List<IEventCallback>> _stringCallbacks = new();
    protected Dictionary<EventIdentifierType, List<IEventCallback>> _enumCallbacks = new();


    #region Public API - Convenience Methods

    /// <summary>
    /// Adds a listener for void events using EventIdentifier.
    /// </summary>
    public void AddListener(EventIdentifier eventId, Action callback) {
      CommonAddListener(eventId, new VoidEventCallback(callback));
      LogDebug($"Added void listener for event: {eventId}");
    }

    /// <summary>
    /// Adds a listener for events with generic payload using EventIdentifier.
    /// </summary>
    public void AddListener<T>(EventIdentifier eventId, Action<T> callback) {
      AddListener(eventId.Type, callback);
      LogDebug($"Added {typeof(T).Name} listener for event: {eventId}");
    }

    private void CommonAddListener(EventIdentifier eventId, IEventCallback callback) {
      switch (eventId.Type) {
        case EventIdentifierType.Int:
          int id = eventId.ToInt();
          if (!_intCallbacks.ContainsKey(id)) _intCallbacks[id] = new();
          _intCallbacks[id].Add(callback);
          break;
        case EventIdentifierType.String:
          string strId = eventId.ToString();
          if (!_stringCallbacks.ContainsKey(strId)) _stringCallbacks[strId] = new();
          _stringCallbacks[strId].Add(callback);
          break;
          case EventIdentifierType.Enum:
            LogDebug("Enum type not supported in this method");
          break;
        default: break;   
      }
    }

    private void CommonAddListener<T>(EventIdentifier eventId, Action<T> action) {
      var callback = new EventCallback<T>(action);
      CommonAddListener(eventId, callback);
    }

    /// <summary>
    /// Removes a listener for void events using EventIdentifier.
    /// </summary>
    public void RemoveListener(EventIdentifier eventId, Action callback) {
      RemoveListener(eventId.Type, callback);
      LogDebug($"Removed void listener for event: {eventId}");
    }

    /// <summary>
    /// Removes a listener for events with generic payload using EventIdentifier.
    /// </summary>
    public void RemoveListener<T>(EventIdentifier eventId, Action<T> callback) {
      RemoveListener(eventId.Type, callback);
      LogDebug($"Removed {typeof(T).Name} listener for event: {eventId}");
    }

    /// <summary>
    /// Raises a void event using EventIdentifier.
    /// </summary>
    public void Raise(EventIdentifier eventId) {
      Raise(eventId.Type);
      LogDebug($"Raised void event: {eventId}");
    }

    /// <summary>
    /// Raises an event with payload using EventIdentifier.
    /// </summary>
    public void Raise<T>(EventIdentifier eventId, T payload) {
      Raise(eventId.Type, payload);
      LogDebug($"Raised {typeof(T).Name} event: {eventId} with payload: {payload}");
    }

    #endregion


    #region Utility Methods

    /// <summary>
    /// Gets the number of registered listeners for a specific event.
    /// </summary>
    public int GetListenerCount(EventIdentifier eventId) {
      // Access the protected callbacks dictionary through reflection or make it accessible
      // For now, we'll provide a placeholder implementation
      return 0; // TODO: Implement when callbacks dictionary is accessible
    }

    /// <summary>
    /// Checks if there are any listeners registered for a specific event.
    /// </summary>
    public bool HasListeners(EventIdentifier eventId) {
      return GetListenerCount(eventId) > 0;
    }

    /// <summary>
    /// Enables or disables debug logging.
    /// </summary>
    public void SetDebugLogging(bool enabled) {
      _enableDebugLogging = enabled;
    }

    #endregion

    #region Debug and Logging

    private void LogDebug(string message) {
      if (_enableDebugLogging) {
        Debug.Log($"[{name}] {message}");
      }
    }

    #endregion

    #region Context Menu Actions

    [ContextMenu("Test Enhanced Event Channel")]
    private void TestEnhancedEventChannel() {
      Debug.Log("=== Enhanced Event Channel Test ===");
      Debug.Log($"Channel Name: {name}");
      Debug.Log($"Debug Logging: {_enableDebugLogging}");

      // Test with different identifier types
      var intId = new EventIdentifier(42);
      var stringId = new EventIdentifier("TestEvent");

      Debug.Log($"Int Identifier: {intId}");
      Debug.Log($"String Identifier: {stringId}");

      Debug.Log("=== End Test ===");
    }

    [ContextMenu("Clear All Listeners")]
    private void ClearAllListenersContextMenu() {
      RemoveAllListeners();
      Debug.Log($"[{name}] All listeners cleared");
    }

    public List<object> GetRegisteredEvents() {
      
      throw new NotImplementedException();
    }

    #endregion
  }

  /// <summary>
  /// Generic EventArgs implementation for custom payloads.
  /// </summary>
  //[Serializable]
  //public class GenericEventArgs : EventArgs {
  //  public string Message { get; }
  //  public object Data { get; }

  //  public GenericEventArgs(string message, object data = null) {
  //    Message = message ?? string.Empty;
  //    Data = data;
  //  }

  //  public override string ToString() {
  //    return $"GenericEventArgs(Message: {Message}, Data: {Data})";
  //  }
  //}
}