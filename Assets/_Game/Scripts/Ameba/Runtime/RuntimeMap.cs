using System.Collections.Generic;
using UnityEngine;

namespace Ameba.Runtime {
  [CreateAssetMenu(fileName = "RuntimeMap", menuName = "Ameba/Runtime/RuntimeMap")]
  public class RuntimeMap : ScriptableObject {

    protected Dictionary<string, object> _registry = new();

    public void Register(string id, object obj) {
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("Attempted to register an object with a null or empty ID.");
        return;
      }
      if (obj == null) {
        Debug.LogWarning($"Attempted to register a null object with ID '{id}'.");
        return;
      }
      if (_registry.ContainsKey(id)) {
        Debug.LogWarning($"ID '{id}' is already registered. Overwriting the existing entry.");
      }
      _registry[id] = obj;
    }

    public void Unregister(string id) {
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("Attempted to unregister an object with a null or empty ID.");
        return;
      }
      if (!_registry.ContainsKey(id)) {
        Debug.LogWarning($"ID '{id}' is not registered. Cannot unregister.");
        return;
      }
      _registry.Remove(id);
    }

    public bool TryGet(string id, out object obj) {
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("Attempted to get an object with a null or empty ID.");
        obj = default;
        return false;
      }
      if (_registry.TryGetValue(id, out obj)) {
        return true;
      }
      Debug.LogWarning($"ID '{id}' is not registered. Returning false.");
      obj = default;
      return false;
    }

    public object Get(string id) {
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("Attempted to get an object with a null or empty ID.");
        return default;
      }
      if (_registry.TryGetValue(id, out object obj)) {
        return obj;
      }
      Debug.LogWarning($"ID '{id}' is not registered. Returning null.");
      return default;
    }

    public T Get<T>(string id) where T : class {
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("Attempted to get an object with a null or empty ID.");
        return default;
      }
      if (_registry.TryGetValue(id, out object obj)) {
        if (obj is T typedObj) {
          return typedObj;
        }
        else {
          Debug.LogWarning($"Object registered with ID '{id}' is not of type {typeof(T).Name}. Returning null.");
          return default;
        }
      }
      Debug.LogWarning($"ID '{id}' is not registered. Returning null.");
      return default;
    }

    public bool TryGet<T>(string id, out T obj) where T : class {
      obj = default;
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("Attempted to get an object with a null or empty ID.");
        return false;
      }
      if (_registry.TryGetValue(id, out object rawObj)) {
        if (rawObj is T typedObj) {
          obj = typedObj;
          return true;
        }
        else {
          Debug.LogWarning($"Object registered with ID '{id}' is not of type {typeof(T).Name}. Returning false.");
          return false;
        }
      }
      Debug.LogWarning($"ID '{id}' is not registered. Returning false.");
      return false;
    }

    public bool Contains(string id) {
      if (string.IsNullOrEmpty(id)) {
        Debug.LogWarning("Attempted to check containment for a null or empty ID.");
        return false;
      }
      return _registry.ContainsKey(id);
    }

    public IEnumerable<string> GetAllIds() => _registry.Keys;

    public IEnumerable<object> GetAllObjects() => _registry.Values;

    public void Clear() => _registry.Clear();

    public int Count => _registry.Count;
  }
}
