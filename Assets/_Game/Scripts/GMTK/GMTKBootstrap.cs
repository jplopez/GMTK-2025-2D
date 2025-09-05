using Ameba;

using UnityEngine;

namespace GMTK {

  /// <summary>
  ///  High-priority service initialization component.
  /// Runs before all other scripts to ensure services are available.
  /// </summary>
  [AddComponentMenu("GMTK/Bootstrap Component")]
  [DefaultExecutionOrder(-100)] // Very high priority - runs before almost everything
  public class GMTKBootstrap : MonoBehaviour {

    [Header("Service Initialization")]
    [Tooltip("If true, this component initialize all services, regardless if they were initialized before")]
    public bool ForceReinitialize = false;
    [Tooltip("Path within Resources where the ServiceRegistry ScriptableObject is located")]
    public string ServiceRegistryResourcePath = "ServiceRegistry";
    [Tooltip("Enable detailed logging of service initialization")]
    public bool EnableDebugLogging = true;

    [Header("GameState Integration")]
    [Tooltip("Automatically connect GameStateMachine to external events")]
    public bool ConnectGameStateMachineEvents = true;

    private void Awake() {
      LogDebug("=== SERVICES INITIALIZATION START ===");
      InitializeAllServices();

      if (ConnectGameStateMachineEvents) {
        InitializeGameStateIntegration();
      }
      LogDebug("=== SERVICES INITIALIZATION COMPLETE ===");
    }
    /// <summary>
    /// Initializes the ServiceLocator and all registered services
    /// </summary>
    private void InitializeAllServices() {
      if (!ServiceLocator.IsInitialized || ForceReinitialize) {
        LogDebug("Initializing ServiceLocator...");

        if(string.IsNullOrEmpty(ServiceRegistryResourcePath)) {
          LogWarning("ServiceRegistryResourcePath is null or empty, using default 'ServiceRegistry'");
          ServiceLocator.ServiceRegistryResourcePath = "ServiceRegistry";
        }
        else {
          ServiceLocator.ServiceRegistryResourcePath = ServiceRegistryResourcePath;
        }

        if (ForceReinitialize) {
          ServiceLocator.Clear();
          LogDebug("ServiceLocator cleared for reinitialization");
        }

        if (ServiceLocator.TryInitialize()) {
          LogDebug("✓ ServiceLocator initialized successfully");
          LogRegisteredServices();
        }
        else {
          LogError("✗ ServiceLocator initialization failed");
        }
      }
      else {
        LogDebug("ServiceLocator already initialized, skipping");
      }
    }

    /// <summary>
    /// Connects GameStateMachine to external GameEventChannel if both are available
    /// </summary>
    private void InitializeGameStateIntegration() {
      if (!ServiceLocator.IsInitialized) {
        LogError("Cannot initialize GameState integration - ServiceLocator not ready");
        return;
      }

      if (ServiceLocator.TryGet<GameEventChannel>(out var eventChannel)) {
        LogDebug("✓ GameEventChannel service found");

        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine)) {
          LogDebug("✓ GameStateMachine service found");

          // Connect external events
          if (!stateMachine.IsSubscribedToExternalEvents) {
            stateMachine.ConnectToExternalEvents(eventChannel);
            LogDebug("✓ GameStateMachine connected to external events");
          }
          else {
            LogDebug("GameStateMachine already connected to external events");
          }
        }
        else {
          LogWarning("GameStateMachine service not found - external events not connected");
        }
      }
      else {
        LogWarning("GameEventChannel service not found - external events not connected");
      }
    }

    /// <summary>
    /// Logs all currently registered services for debugging
    /// </summary>
    private void LogRegisteredServices() {
      if (!EnableDebugLogging) return;

      var registeredTypes = ServiceLocator.GetRegisteredTypes();
      LogDebug($"Registered services ({registeredTypes.Length}):");

      foreach (var type in registeredTypes) {
        LogDebug($"  - {type.Name}");
      }
    }

    // Logging helpers
    private void LogDebug(string message) {
      if (EnableDebugLogging) {
        Debug.Log($"[Bootstrap] {message}");
      }
    }

    private void LogWarning(string message) {
      Debug.LogWarning($"[Bootstrap] {message}");
    }

    private void LogError(string message) {
      Debug.LogError($"[Bootstrap] {message}");
    }
  }
}
