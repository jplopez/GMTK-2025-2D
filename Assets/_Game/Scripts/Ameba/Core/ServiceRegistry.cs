using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ameba {

  [CreateAssetMenu(menuName = "Ameba/Service Registry", fileName = "ServiceRegistry")]
  public class ServiceRegistry : ScriptableObject {

    [Header("Service Types")]
    [Tooltip("List of ScriptableObject types to register as services")]
    [SerializeField] private List<string> _serviceTypeNames = new();

    // Runtime property to get actual Types
    public Type[] ServiceTypes {
      get {
        var types = new List<Type>();
        foreach (var typeName in _serviceTypeNames) {
          if (string.IsNullOrEmpty(typeName)) continue;

          var type = GetTypeFromName(typeName);
          if (type != null) {
            types.Add(type);
          }
          else {
            Debug.LogWarning($"[ServiceRegistry] Could not find type: {typeName}");
          }
        }
        return types.ToArray();
      }
    }

    private Type GetTypeFromName(string typeName) {
      // First try just the type name
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
        var type = assembly.GetType(typeName);
        if (type != null) return type;
      }

      // If that fails, try with common namespaces
      string[] namespaces = { "GMTK", "Ameba" };
      foreach (var ns in namespaces) {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
          var type = assembly.GetType($"{ns}.{typeName}");
          if (type != null) return type;
        }
      }

      return null;
    }

#region Context Menu

    [ContextMenu("Add Common Service Types")]
    private void AddCommonServiceTypes() {
      // Add your common ScriptableObject types here
      string[] commonTypes = {
                "LevelService",
                "GameEventChannel",
                "GameStateMachine",
            };

      foreach (var typeName in commonTypes) {
        if (!_serviceTypeNames.Contains(typeName)) {
          _serviceTypeNames.Add(typeName);
        }
      }

      Debug.Log($"[ServiceRegistry] Added {commonTypes.Length} common service types");
    }

    [ContextMenu("Clear All Service Types")]
    private void ClearAllServiceTypes() {
      _serviceTypeNames.Clear();
      Debug.Log("[ServiceRegistry] Cleared all service types");
    }

    [ContextMenu("Validate ServiceLocator")]
    private void ValidateServices() {
      Debug.Log("[ServiceRegistry] Validating services...");

      int validCount = 0;
      foreach (var typeName in _serviceTypeNames) {
        if (string.IsNullOrEmpty(typeName)) continue;

        var type = GetTypeFromName(typeName);
        if (type == null) {
          Debug.LogError($"✗ Type not found: {typeName}");
          continue;
        }

        var resource = Resources.Load<ScriptableObject>(type.Name);
        if (resource != null) {
          validCount++;
          Debug.Log($"✓ {typeName} -> Resources/{type.Name}");
        }
        else {
          Debug.LogError($"✗ Resource not found: Resources/{type.Name} for {typeName}");
        }
      }

      Debug.Log($"[ServiceRegistry] Validation complete: {validCount}/{_serviceTypeNames.Count} services are valid");
    }

    [ContextMenu("Initialize ServiceLocator")]
    public void InitializeServiceLocator() {
      Debug.Log("[ServiceRegistry] Initializing ServiceLocator");
      try {
        if(ServiceLocator.TryInitialize()) {
          Debug.Log($"[ServiceRegistry] Service Locator initialized with {ServiceLocator.GetRegisteredTypes().Length} services");
        } else {
          Debug.LogError($"[ServiceRegistry] Service locator failed to initialize");
        }
      } catch(Exception ex) {
        Debug.LogError($"[ServiceRegistry] Service locator initialization threw an exception: {ex.Message}");
        Debug.LogException(ex);
      }
    }

#endregion

#region Public API

    public void AddServiceType(string typeName) {
      if (!_serviceTypeNames.Contains(typeName)) {
        _serviceTypeNames.Add(typeName);
      }
    }

    public void RemoveServiceType(string typeName) {
      _serviceTypeNames.Remove(typeName);
    }

    public string[] GetServiceTypeNames() {
      return _serviceTypeNames.ToArray();
    }

#endregion
  }
}
