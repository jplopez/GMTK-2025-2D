using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ameba.Testing {

  /// <summary>
  /// Test utilities for EventChannel to programmatically trigger events, 
  /// mock payloads, and track raised events for assertions.
  /// </summary>
  /// <typeparam name="TEnum">The enum type used by the EventChannel</typeparam>
  public class EventChannelTestUtils<TEnum> where TEnum : Enum {

    private EventChannel<TEnum> _eventChannel;
    private Dictionary<TEnum, List<object>> _raisedEvents = new Dictionary<TEnum, List<object>>();
    private Dictionary<TEnum, Action> _voidListeners = new Dictionary<TEnum, Action>();
    private Dictionary<TEnum, List<Action<object>>> _payloadListeners = new Dictionary<TEnum, List<Action<object>>>();

    /// <summary>
    /// Creates a new test utils instance for the given EventChannel.
    /// </summary>
    /// <param name="eventChannel">The EventChannel to test</param>
    public EventChannelTestUtils(EventChannel<TEnum> eventChannel) {
      _eventChannel = eventChannel ?? throw new ArgumentNullException(nameof(eventChannel));
    }

    /// <summary>
    /// Starts tracking events raised on the EventChannel.
    /// Call this before the code under test runs.
    /// </summary>
    public void StartTracking() {
      _raisedEvents.Clear();
    }

    /// <summary>
    /// Stops tracking events and cleans up listeners.
    /// </summary>
    public void StopTracking() {
      // Remove all void listeners
      foreach (var kvp in _voidListeners) {
        _eventChannel.RemoveListener(kvp.Key, kvp.Value);
      }
      _voidListeners.Clear();

      // Remove all payload listeners
      foreach (var kvp in _payloadListeners) {
        foreach (var listener in kvp.Value) {
          // Note: This requires reflection or a RemoveListener overload that accepts Action<object>
          // For now, we'll track but cleanup may need enhancement
        }
      }
      _payloadListeners.Clear();
    }

    /// <summary>
    /// Adds a listener to track when a void event is raised.
    /// </summary>
    /// <param name="eventType">The event type to track</param>
    public void TrackEvent(TEnum eventType) {
      if (!_voidListeners.ContainsKey(eventType)) {
        Action listener = () => RecordEvent(eventType, null);
        _voidListeners[eventType] = listener;
        _eventChannel.AddListener(eventType, listener);
      }
    }

    /// <summary>
    /// Adds a listener to track when an event with payload is raised.
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="eventType">The event type to track</param>
    public void TrackEvent<TPayload>(TEnum eventType) {
      Action<TPayload> listener = (payload) => RecordEvent(eventType, payload);
      _eventChannel.AddListener(eventType, listener);
      
      if (!_payloadListeners.ContainsKey(eventType)) {
        _payloadListeners[eventType] = new List<Action<object>>();
      }
      // Store as Action<object> for cleanup tracking
      _payloadListeners[eventType].Add(obj => { });
    }

    /// <summary>
    /// Records an event that was raised.
    /// </summary>
    private void RecordEvent(TEnum eventType, object payload) {
      if (!_raisedEvents.ContainsKey(eventType)) {
        _raisedEvents[eventType] = new List<object>();
      }
      _raisedEvents[eventType].Add(payload);
    }

    /// <summary>
    /// Programmatically raises a void event on the EventChannel.
    /// </summary>
    /// <param name="eventType">The event type to raise</param>
    public void RaiseEvent(TEnum eventType) {
      _eventChannel.Raise(eventType);
    }

    /// <summary>
    /// Programmatically raises an event with a payload on the EventChannel.
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="eventType">The event type to raise</param>
    /// <param name="payload">The payload to send with the event</param>
    public void RaiseEvent<TPayload>(TEnum eventType, TPayload payload) {
      _eventChannel.Raise(eventType, payload);
    }

    /// <summary>
    /// Gets the number of times an event was raised.
    /// </summary>
    /// <param name="eventType">The event type to check</param>
    /// <returns>The number of times the event was raised</returns>
    public int GetEventRaisedCount(TEnum eventType) {
      return _raisedEvents.ContainsKey(eventType) ? _raisedEvents[eventType].Count : 0;
    }

    /// <summary>
    /// Checks if an event was raised at least once.
    /// </summary>
    /// <param name="eventType">The event type to check</param>
    /// <returns>True if the event was raised at least once</returns>
    public bool WasEventRaised(TEnum eventType) {
      return GetEventRaisedCount(eventType) > 0;
    }

    /// <summary>
    /// Gets all payloads for a specific event type.
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="eventType">The event type</param>
    /// <returns>List of all payloads received for this event</returns>
    public List<TPayload> GetEventPayloads<TPayload>(TEnum eventType) {
      var result = new List<TPayload>();
      if (_raisedEvents.ContainsKey(eventType)) {
        foreach (var payload in _raisedEvents[eventType]) {
          if (payload != null && payload is TPayload typedPayload) {
            result.Add(typedPayload);
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Gets the most recent payload for an event.
    /// </summary>
    /// <typeparam name="TPayload">The payload type</typeparam>
    /// <param name="eventType">The event type</param>
    /// <returns>The most recent payload, or default if none</returns>
    public TPayload GetLastEventPayload<TPayload>(TEnum eventType) {
      if (_raisedEvents.ContainsKey(eventType) && _raisedEvents[eventType].Count > 0) {
        var payload = _raisedEvents[eventType][_raisedEvents[eventType].Count - 1];
        if (payload is TPayload typedPayload) {
          return typedPayload;
        }
      }
      return default;
    }

    /// <summary>
    /// Clears all tracked events.
    /// </summary>
    public void ClearTrackedEvents() {
      _raisedEvents.Clear();
    }

    /// <summary>
    /// Gets all event types that have been raised.
    /// </summary>
    /// <returns>List of event types that were raised</returns>
    public List<TEnum> GetRaisedEventTypes() {
      return new List<TEnum>(_raisedEvents.Keys);
    }
  }
}
