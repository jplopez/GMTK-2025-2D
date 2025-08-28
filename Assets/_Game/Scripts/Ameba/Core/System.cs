using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Static Service Locator for managing ScriptableObject services throughout the application
  /// </summary>
  public static class Services {
    private static ServiceRegistry _registry;
    private static readonly Dictionary<Type, ScriptableObject> _services = new();
    private static bool _isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
      if (_isInitialized) {
        Debug.LogWarning("[Services] Already initialized - skipping");
        return;
      }

      Debug.Log("[Services] Starting initialization...");

      LoadRegistry();
      if (_registry != null) {
        RegisterAllServices();
      }

      _isInitialized = true;

#if UNITY_WEBGL && !UNITY_EDITOR
            // Protect from GC in WebGL
            GC.KeepAlive(_services);
            GC.KeepAlive(_registry);
            Debug.Log("[Services] WebGL GC protection applied");
#endif

      Debug.Log($"[Services] Initialization complete - {_services.Count} services registered");
    }

    private static void LoadRegistry() {
      _registry = Resources.Load<ServiceRegistry>("ServiceRegistry");
      if (_registry == null) {
        Debug.LogError("[Services] ServiceRegistry not found at Resources/ServiceRegistry! Please create one.");
      }
      else {
        Debug.Log($"[Services] ServiceRegistry loaded from Resources");
      }
    }

    private static void RegisterAllServices() {
      foreach (var serviceType in _registry.ServiceTypes) {
        if (serviceType == null) {
          Debug.LogWarning("[Services] Null service type found in registry - skipping");
          continue;
        }

        RegisterServiceOfType(serviceType);
      }
    }

    private static void RegisterServiceOfType(Type serviceType) {
      // Use the type name as the resource name
      var resourceName = serviceType.Name;

      var service = Resources.Load<ScriptableObject>(resourceName);
      if (service != null) {
        _services[serviceType] = service;
        Debug.Log($"[Services] ? Registered {serviceType.Name} from Resources/{resourceName}");
      }
      else {
        Debug.LogError($"[Services] ? Failed to load {resourceName} for {serviceType.Name}");
      }
    }

    /// <summary>
    /// Get a service of the specified type
    /// </summary>
    public static T Get<T>() where T : ScriptableObject {
      if (!_isInitialized) {
        Debug.LogWarning("[Services] Not initialized yet - forcing initialization");
        Initialize();
      }

      if (_services.TryGetValue(typeof(T), out var service)) {
        return service as T;
      }

      Debug.LogError($"[Services] Service of type {typeof(T).Name} not found! Make sure it's registered in ServiceRegistry and exists in Resources folder.");
      return null;
    }

    /// <summary>
    /// Check if a service is registered
    /// </summary>
    public static bool Has<T>() where T : ScriptableObject {
      return _services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Get all registered service types (for debugging)
    /// </summary>
    public static Type[] GetRegisteredTypes() {
      var types = new Type[_services.Count];
      _services.Keys.CopyTo(types, 0);
      return types;
    }

    /// <summary>
    /// Force re-initialization (for testing)
    /// </summary>
    public static void ForceReinitialize() {
      _isInitialized = false;
      _services.Clear();
      Initialize();
    }
  }
}
