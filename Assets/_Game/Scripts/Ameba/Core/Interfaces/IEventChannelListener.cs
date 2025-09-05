using System;

namespace Ameba {

  public interface IEventChannelListener<T> where T : Enum {
    public bool IsTriggering { get; }
    public PayloadType[] Payloads { get; }
    public T[] AllowedEventTypes { get; }
    public void Trigger<TPayload>(T type, TPayload payload = default);

    public void SubscribeToEventChannel(EventChannel<T> channel);
    public void UnsubscribeFromEventChannel(EventChannel<T> channel);

  }

  public abstract class EventChannelListenerBase<T> : IEventChannelListener<T> where T : Enum {
    public virtual bool IsSubscribed { get => _isSubscribed; }
    public virtual bool IsTriggering { get => _isTriggering; }
    public virtual PayloadType[] Payloads { get => _defaultPayloads; }
    public virtual T[] AllowedEventTypes { get => _allowedEventTypes; }

    protected bool _isSubscribed = false;
    protected bool _isTriggering = false;
    protected PayloadType[] _defaultPayloads = new PayloadType[] { PayloadType.Void };
    protected T[] _allowedEventTypes = Array.Empty<T>();

    public EventChannelListenerBase() { }

    public EventChannelListenerBase(T[] allowedEventTypes, PayloadType[] payloadTypes) {
      _allowedEventTypes = allowedEventTypes ?? Array.Empty<T>();
      _defaultPayloads = payloadTypes ?? new PayloadType[] { PayloadType.Void };
    }

    public EventChannelListenerBase(T[] allowedEventTypes) {
      _allowedEventTypes = allowedEventTypes ?? Array.Empty<T>();
      _defaultPayloads = new PayloadType[] { PayloadType.Void };
    }

    public EventChannelListenerBase(PayloadType[] payloadTypes) {
      _allowedEventTypes = Array.Empty<T>();
      _defaultPayloads = payloadTypes ?? new PayloadType[] { PayloadType.Void };
    }

    public EventChannelListenerBase(T allowedEventType, PayloadType payloadType) {
      _allowedEventTypes = new T[] { allowedEventType };
      _defaultPayloads = new PayloadType[] { payloadType };
    }

    public EventChannelListenerBase(T allowedEventType) {
      _allowedEventTypes = new T[] { allowedEventType };
      _defaultPayloads = new PayloadType[] { PayloadType.Void };
    }

    public EventChannelListenerBase(PayloadType payloadType) {
      _allowedEventTypes = Array.Empty<T>();
      _defaultPayloads = new PayloadType[] { payloadType };
    }


    public void AddAllowedEventType(T type) {
      if (Array.Exists(_allowedEventTypes, t => t.Equals(type))) return;
      var tempList = new System.Collections.Generic.List<T>(_allowedEventTypes) { type };
      _allowedEventTypes = tempList.ToArray();
    }
    public void AddPayloadType(PayloadType type) {
      if (Array.Exists(_defaultPayloads, t => t == type)) return;
      var tempList = new System.Collections.Generic.List<PayloadType>(_defaultPayloads) { type };
      _defaultPayloads = tempList.ToArray();
    }

    public void RemoveAllowedEventType(T type) {
      if (!Array.Exists(_allowedEventTypes, t => t.Equals(type))) return;
      var tempList = new System.Collections.Generic.List<T>(_allowedEventTypes);
      tempList.Remove(type);
      _allowedEventTypes = tempList.ToArray();
    }

    public void RemovePayloadType(PayloadType type) {
      if (!Array.Exists(_defaultPayloads, t => t == type)) return;
      var tempList = new System.Collections.Generic.List<PayloadType>(_defaultPayloads);
      tempList.Remove(type);
      _defaultPayloads = tempList.ToArray();
    }

    public void ClearAllowedEventTypes() => _allowedEventTypes = Array.Empty<T>();
    public void ClearPayloadTypes() => _defaultPayloads = Array.Empty<PayloadType>();

    public bool CanTriggerEventType(T type) => Array.Exists(_allowedEventTypes, t => t.Equals(type));

    public bool CanTriggerPayloadType(Type payloadType) {
      if (payloadType == typeof(void) && _defaultPayloads.Length == 0) {
        return true;
      }
      if (payloadType == typeof(int)) {
        return _defaultPayloads.Length == 0 || Array.Exists(_defaultPayloads, t => t == PayloadType.Int);
      }
      if (payloadType == typeof(bool)) {
        return _defaultPayloads.Length == 0 || Array.Exists(_defaultPayloads, t => t == PayloadType.Bool);
      }
      if (payloadType == typeof(float)) {
        return _defaultPayloads.Length == 0 || Array.Exists(_defaultPayloads, t => t == PayloadType.Float);
      }
      if (payloadType == typeof(string)) {
        return _defaultPayloads.Length == 0 || Array.Exists(_defaultPayloads, t => t == PayloadType.String);
      }
      if (payloadType == typeof(EventArgs) || payloadType.IsSubclassOf(typeof(EventArgs))) {
        return _defaultPayloads.Length == 0 || Array.Exists(_defaultPayloads, t => t == PayloadType.EventArg);
      }
      return false;
    }

    public virtual void Trigger(T type) {
      if (!CanTriggerEventType(type)) {
        UnityEngine.Debug.LogWarning($"[{GetType().Name}] Cannot trigger event type {type}. It is not in the allowed event types.");
        return;
      }
      try {
        _isTriggering = true;
        // UnityEngine.Debug.Log($"[{GetType().Name}] Triggering event type {type}.");
        DoTrigger(type);
      }
      catch (Exception ex) {
        UnityEngine.Debug.LogError($"[{GetType().Name}] Error triggering event type {type}: {ex}");
        UnityEngine.Debug.LogException(ex);
#if UNITY_EDITOR
        throw ex;
#else 
        return;
#endif
      }
      finally {
        _isTriggering = false;
      }
    }

    public virtual void Trigger<P>(T type, P payload = default) {
      if (!CanTriggerEventType(type)) {
        UnityEngine.Debug.LogWarning($"[{GetType().Name}] Cannot trigger event type {type}. It is not in the allowed event types.");
        return;
      }
      if (!CanTriggerPayloadType(typeof(P))) {
        UnityEngine.Debug.LogWarning($"[{GetType().Name}] Cannot trigger payload type {typeof(P).Name}. It is not in the allowed payload types.");
        return;
      }
      try {
        _isTriggering = true;
        // UnityEngine.Debug.Log($"[{GetType().Name}] Triggering event type {type} with payload {payload}.");
        DoTrigger(type, payload);
      }
      catch (Exception ex) {
        UnityEngine.Debug.LogError($"[{GetType().Name}] Error triggering event type {type} with payload {payload}: {ex}");
        UnityEngine.Debug.LogException(ex);
      }
      finally {
        _isTriggering = false;
      }
    }

    protected abstract void DoTrigger(T type);
    protected abstract void DoTrigger<TPayload>(T type, TPayload payload = default);

    public virtual void SubscribeToEventChannel(EventChannel<T> channel) {
      //subscribes to every supported event type and payload
      //SubscribeVoidTrigger(channel);

      foreach (var type in AllowedEventTypes) {
        foreach(var payload in Payloads) {
          if (!CanTriggerEventType(type)) continue;
          if (!CanTriggerPayloadType(payload.GetType())) continue;
          if (IsSubscribed) {
            UnityEngine.Debug.LogWarning($"[{GetType().Name}] Already subscribed to event channel {channel.name} for event type {type} and payload {payload}. Skipping subscription.");
            return;
          }
          // UnityEngine.Debug.Log($"[{GetType().Name}] Subscribing to event channel {channel.name} for event type {type} and payload {payload}.");
          //subscribe based on payload type
          switch (payload) {
            case PayloadType.Void:
              channel.AddListener(type, () => Trigger(type));
              break;
            default:
              channel.AddListener<int>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.Bool:
              channel.AddListener<bool>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.Float:
              channel.AddListener<float>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.String:
              channel.AddListener<string>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.EventArg:
              channel.AddListener<EventArgs>(type, (p) => Trigger(type, p));
              break;
          }
          _isSubscribed = true;
        }
      }
    }
    public virtual void UnsubscribeFromEventChannel(EventChannel<T> channel) {
      if (channel == null) return;
      foreach (var type in AllowedEventTypes) {
        foreach (var payload in Payloads) {
          if (!CanTriggerEventType(type)) continue;
          if (!CanTriggerPayloadType(payload.GetType())) continue;
          // UnityEngine.Debug.Log($"[{GetType().Name}] Unsubscribing from event channel {channel.name} for event type {type} and payload {payload}.");
          //unsubscribe based on payload type
          switch (payload) {
            case PayloadType.Void:
              channel.RemoveListener(type, () => Trigger(type));
              break;
            default:
              channel.RemoveListener<int>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.Bool:
              channel.RemoveListener<bool>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.Float:
              channel.RemoveListener<float>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.String:
              channel.RemoveListener<string>(type, (p) => Trigger(type, p));
              break;
            case PayloadType.EventArg:
              channel.RemoveListener<EventArgs>(type, (p) => Trigger(type, p));
              break;
          }
          _isSubscribed = false;
        }
      }
    }

  }
}