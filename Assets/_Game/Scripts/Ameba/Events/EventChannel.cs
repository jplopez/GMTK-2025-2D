using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ameba {

  public enum PayloadType {
    Void,
    Int,
    Bool,
    Float,
    String,
    EventArg
  }

  /// <summary>
  /// Type-safe event channel using generics and interfaces.
  /// </summary>
  /// <typeparam name="Tenum"></typeparam>
  public partial class EventChannel<Tenum> : ScriptableObject where Tenum : Enum {

    private Dictionary<Tenum, List<IEventCallback>> callbacks = new();


    // Unified Add/Remove methods
    public void AddListener<T>(Tenum type, Action<T> callback) {
      if (!callbacks.ContainsKey(type)) callbacks[type] = new();
      callbacks[type].Add(new EventCallback<T>(callback));
    }

    public void AddListener(Tenum type, Action callback) {
      if (!callbacks.ContainsKey(type)) callbacks[type] = new();
      callbacks[type].Add(new VoidEventCallback(callback));
    }

    public void RemoveListener<T>(Tenum type, Action<T> callback) {
      if (!callbacks.TryGetValue(type, out var callbackList)) return;
      callbackList.RemoveAll(c => c is EventCallback<T> ec && ec.Action == callback);
      if (callbackList.Count == 0) callbacks.Remove(type);
    }

    public void RemoveListener(Tenum type, Action callback) {
      if (!callbacks.TryGetValue(type, out var callbackList)) return;
      callbackList.RemoveAll(c => c is VoidEventCallback vc && vc.Action == callback);
      if (callbackList.Count == 0) callbacks.Remove(type);
    }

    public void Raise(Tenum type) {
      if (!callbacks.TryGetValue(type, out var callbackList)) return;

      foreach (var callback in callbackList) {
        if (callback.PayloadType == typeof(void)) {
          callback.Invoke(null);
        }
      }
    }

    // Unified Raise method
    public void Raise<T>(Tenum type, T payload = default) {
      if (!callbacks.TryGetValue(type, out var callbackList)) return;

      foreach (var callback in callbackList) {
        if (typeof(T) == typeof(void) && callback.PayloadType == typeof(void)) {
          callback.Invoke(null);
        }
        else if (callback.PayloadType == typeof(T)) {
          callback.Invoke(payload);
        }
      }
    }

    public void RemoveAllListeners() => callbacks?.Clear();
  }
}