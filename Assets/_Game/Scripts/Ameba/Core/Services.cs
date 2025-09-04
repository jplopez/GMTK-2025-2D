using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ameba {
  public static class Services {
    private static Dictionary<Type, object> _services = new();
    private static bool _isInitialized = false;

    public static bool IsInitialized => _isInitialized;

    public static bool TryInitialize() {
      if (_isInitialized) return true;
      try {
        InitializeAllScriptableObjects();
        _isInitialized = true;
      } catch (Exception ex) {
        Debug.LogError($"[Services] Initialization failed: {ex.Message}");
        _isInitialized = false;
      } 
      return _isInitialized;
    }

    private static void InitializeAllScriptableObjects() {
      // Load service registry first
      var registry = Resources.Load<ServiceRegistry>("ServiceRegistry");
      if (registry != null) {
        Initialize(registry);
      }
      else {
        Debug.LogWarning("[Services] No ServiceRegistry found in Resources.");
      }
    }

    public static void Initialize(ServiceRegistry registry) {
      if (_isInitialized) return;
      
      foreach (var serviceType in registry.ServiceTypes) {
        var resource = Resources.Load<ScriptableObject>(serviceType.Name);
        if (resource != null) {
          _services[serviceType] = resource;
          Debug.Log($"[Services] Registered: {serviceType.Name}");
        }
      }
      _isInitialized = true;
    }

    public static T Get<T>() where T : class {
      _services.TryGetValue(typeof(T), out var service);
      return service as T;
    }

    public static bool TryGet<T>(out T service) where T : class {
      if (_services.TryGetValue(typeof(T), out var obj) && obj is T typedService) {
        service = typedService;
        return true;
      }
      service = null;
      return false;
    }

    public static Type[] GetRegisteredTypes() => new List<Type>(_services.Keys).ToArray();

    public static void Clear() {
      _services.Clear();
      _isInitialized = false;
    }

  }
}
