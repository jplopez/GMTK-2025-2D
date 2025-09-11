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

#if UNITY_EDITOR
    private void OnValidate() {
      this.Log("=== UE: SERVICES INITIALIZATION START ===");
      InitializeAllServices();
      this.Log("=== UE: SERVICES INITIALIZATION COMPLETE ===");
    }
#endif


    private void Awake() {
      this.Log("=== SERVICES INITIALIZATION START ===");
      InitializeAllServices();

      if (ConnectGameStateMachineEvents) {
        InitializeGameStateIntegration();
      }
      this.Log("=== SERVICES INITIALIZATION COMPLETE ===");
    }
    /// <summary>
    /// Initializes the ServiceLocator and all registered services
    /// </summary>
    private void InitializeAllServices() {
      if (!ServiceLocator.IsInitialized || ForceReinitialize) {
        this.Log("Initializing ServiceLocator...");

        if(string.IsNullOrEmpty(ServiceRegistryResourcePath)) {
          this.LogWarning("ServiceRegistryResourcePath is null or empty, using default 'ServiceRegistry'");
          ServiceLocator.ServiceRegistryResourcePath = "ServiceRegistry";
        }
        else {
          ServiceLocator.ServiceRegistryResourcePath = ServiceRegistryResourcePath;
        }

        if (ForceReinitialize) {
          ServiceLocator.Clear();
          this.Log("ServiceLocator cleared for reinitialization");
        }

        if (ServiceLocator.TryInitialize()) {
          this.Log("✓ ServiceLocator initialized successfully");
          LogRegisteredServices();
        }
        else {
          this.LogError("✗ ServiceLocator initialization failed");
        }
      }
      else {
        this.Log("ServiceLocator already initialized, skipping");
      }
    }

    /// <summary>
    /// Connects GameStateMachine to external GameEventChannel if both are available
    /// </summary>
    private void InitializeGameStateIntegration() {
      if (!ServiceLocator.IsInitialized) {
        this.LogError("Cannot initialize GameState integration - ServiceLocator not ready");
        return;
      }

      if (ServiceLocator.TryGet<GameEventChannel>(out var eventChannel)) {
        this.Log("✓ GameEventChannel service found");

        if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine)) {
          this.Log("✓ GameStateMachine service found");

          // Connect external events
          if (!stateMachine.IsSubscribedToExternalEvents) {
            stateMachine.ConnectToExternalEvents(eventChannel);
            this.Log("✓ GameStateMachine connected to external events");
          }
          else {
            this.Log("GameStateMachine already connected to external events");
          }
        }
        else {
          this.LogWarning("GameStateMachine service not found - external events not connected");
        }
      }
      else {
        this.LogWarning("GameEventChannel service not found - external events not connected");
      }
    }

    /// <summary>
    /// Logs all currently registered services for debugging
    /// </summary>
    private void LogRegisteredServices() {
      if (!EnableDebugLogging) return;

      var registeredTypes = ServiceLocator.GetRegisteredTypes();
      this.Log($"Registered services ({registeredTypes.Length}):");

      foreach (var type in registeredTypes) {
        this.Log($"  - {type.Name}");
      }
    }

  }
}
