using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ameba {

  public class EventChannel<Tenum> : ScriptableObject where Tenum : Enum {
    public enum PayloadType {
      Void,
      Int,
      Bool,
      Float,
      String
    }

    private Dictionary<Tenum, Action> voidEvents = new();
    private Dictionary<Tenum, Action<int>> intEvents = new();
    private Dictionary<Tenum, Action<bool>> boolEvents = new();
    private Dictionary<Tenum, Action<float>> floatEvents = new();
    private Dictionary<Tenum, Action<string>> stringEvents = new();

    // Add Listener
    public void AddListener(Tenum type, Action callback) {
      if (!voidEvents.ContainsKey(type)) voidEvents[type] = null;
      voidEvents[type] += callback;
    }

    public void AddListener(Tenum type, Action<int> callback) {
      if (!intEvents.ContainsKey(type)) intEvents[type] = null;
      intEvents[type] += callback;
    }

    public void AddListener(Tenum type, Action<bool> callback) {
      if (!boolEvents.ContainsKey(type)) boolEvents[type] = null;
      boolEvents[type] += callback;
    }

    public void AddListener(Tenum type, Action<float> callback) {
      if (!floatEvents.ContainsKey(type)) floatEvents[type] = null;
      floatEvents[type] += callback;
    }

    public void AddListener(Tenum type, Action<string> callback) {
      if (!stringEvents.ContainsKey(type)) stringEvents[type] = null;
      stringEvents[type] += callback;
    }

    // Remove Listener
    public void RemoveListener(Tenum type, Action callback) {
      if (voidEvents.ContainsKey(type)) voidEvents[type] -= callback;
    }

    public void RemoveListener(Tenum type, Action<int> callback) {
      if (intEvents.ContainsKey(type)) intEvents[type] -= callback;
    }

    public void RemoveListener(Tenum type, Action<bool> callback) {
      if (boolEvents.ContainsKey(type)) boolEvents[type] -= callback;
    }

    public void RemoveListener(Tenum type, Action<float> callback) {
      if (floatEvents.ContainsKey(type)) floatEvents[type] -= callback;
    }

    public void RemoveListener(Tenum type, Action<string> callback) {
      if (stringEvents.ContainsKey(type)) stringEvents[type] -= callback;
    }

    // Raise Event
    public void Raise(Tenum type) {
      voidEvents.TryGetValue(type, out var callback);
      callback?.Invoke();
    }

    public void Raise(Tenum type, int value) {
      intEvents.TryGetValue(type, out var callback);
      callback?.Invoke(value);
    }

    public void Raise(Tenum type, bool value) {
      boolEvents.TryGetValue(type, out var callback);
      callback?.Invoke(value);
    }

    public void Raise(Tenum type, float value) {
      floatEvents.TryGetValue(type, out var callback);
      callback?.Invoke(value);
    }

    public void Raise(Tenum type, string value) {
      stringEvents.TryGetValue(type, out var callback);
      callback?.Invoke(value);
    }
  }
}