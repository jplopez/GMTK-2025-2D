using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ameba {

  /// <summary>
  /// This static class manages the registration and retrieval of services implemented as ScriptableObjects.
  /// </summary>
  public static class ServiceLocator {

    public static string ServiceRegistryResourcePath { get => _serviceRegistryResourcePath;
      set { 
        if(string.IsNullOrEmpty(value)) 
          Debug.LogWarning("[ServiceLocator] ServiceRegistryResourcePath cannot be null or empty. Keeping previous value.");
        else 
          _serviceRegistryResourcePath = value;
      }
    }

    // Dictionary to hold registered services
    private static Dictionary<Type, object> _services = new();
    // Flag to indicate if the system has been initialized
    private static bool _isInitialized = false;

    private static string _serviceRegistryResourcePath = "ServiceRegistry";
    /// <summary>
    /// whether the system has been successfully initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// Attempts to initialize all scriptable objects required by the service.
    /// </summary>
    /// <remarks>This method ensures that the initialization process is performed only once.  If the service
    /// is already initialized, the method returns <see langword="true"/> immediately.  If an exception occurs during
    /// initialization, the method logs the error and returns <see langword="false"/>.</remarks>
    /// <returns><see langword="true"/> if the initialization is successful or has already been completed;  otherwise, <see
    /// langword="false"/>.</returns>
    public static bool TryInitialize() {
      if (_isInitialized) return true;
      try {
        InitializeAllScriptableObjects();
        _isInitialized = true;
      }
      catch (Exception ex) {
        Debug.LogError($"[ServiceLocator] Initialization failed: {ex.Message}");
        _isInitialized = false;
      }
      return _isInitialized;
    }

    // will load the ServiceRegistry from Resources and initialize all listed services
    private static void InitializeAllScriptableObjects() {
      // Load service registry first
      var registry = Resources.Load<ServiceRegistry>(ServiceRegistryResourcePath);
      if (registry != null) {
        Initialize(registry);
      }
      else {
        Debug.LogWarning("[ServiceLocator] No ServiceRegistry found in Resources.");
      }
    }

    // Initialize all services listed in the registry
    private static void Initialize(ServiceRegistry registry) {
      if (_isInitialized) return;
      if (registry.ServiceTypes.All(s => RegisterService(s))) {
        Debug.Log($"[ServiceLocator] All services in {registry.name} were initialized!");
        _isInitialized = true;
      }
      else {
        Debug.LogWarning($"[ServiceLocator] Some services in {registry.name} were not initialized. See logs for details");
        _isInitialized = false;
      }
    }

    // Register a single service by loading it from Resources
    private static bool RegisterService(Type serviceType) {
      var resource = Resources.Load<ScriptableObject>(serviceType.Name);
      if (resource != null) {
        _services[serviceType] = resource;
        Debug.Log($"[ServiceLocator] Registered: {serviceType.Name}");
        return true;
      }
      Debug.Log($"[ServiceLocator] Failed to register {serviceType.Name}");
      return false;
    }

    /// <summary>
    /// Retrieves a registered service of the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T Get<T>() where T : class {
      _services.TryGetValue(typeof(T), out var service);
      return service as T;
    }

    /// <summary>
    /// Attempts to retrieve a service of the specified type from the service collection.
    /// </summary>
    /// <typeparam name="T">The type of the service to retrieve. Must be a reference type.</typeparam>
    /// <param name="service">When this method returns, contains the service instance of type <typeparamref name="T"/> if found; otherwise,
    /// <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a service of type <typeparamref name="T"/> was successfully retrieved; otherwise, <see
    /// langword="false"/>.</returns>
    public static bool TryGet<T>(out T service) where T : class {
      if (_services.TryGetValue(typeof(T), out var obj) && obj is T typedService) {
        service = typedService;
        return true;
      }
      service = null;
      return false;
    }

    /// <summary>
    /// Determines whether an object of the specified type exists in the current context.
    /// </summary>
    /// <remarks>This method checks for the presence of an object of the specified type without retrieving
    /// it.</remarks>
    /// <typeparam name="T">The type of object to locate. Must be a reference type.</typeparam>
    /// <returns><see langword="true"/> if an object of type <typeparamref name="T"/> exists; otherwise, <see langword="false"/>.</returns>
    public static bool Contains<T>() where T : class => TryGet<T>(out _);


    /// <summary>
    /// Returns all the Types currently registered in the service registry.
    /// </summary>
    /// <returns></returns>
    public static Type[] GetRegisteredTypes() => new List<Type>(_services.Keys).ToArray();

    /// <summary>
    /// Clears all registered services and resets the initialization state.
    /// </summary>
    /// <remarks>This method removes all services from the internal registry and marks the system as
    /// uninitialized.  After calling this method, the system must be reinitialized before registering or using
    /// services.</remarks>
    public static void Clear() {
      _services.Clear();
      _isInitialized = false;
    }

  }
}
