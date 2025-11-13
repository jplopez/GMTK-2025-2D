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

    [Header("Log Levels")]
    [Help("Configure which log levels are enabled for this component")]
    [Tooltip("Be cautious this is very verbose and can impact performance")]
    public bool LogDebug = true;
    public bool LogInfo = true;
    [Tooltip("Heads up on edge cases that do not impact the game flow")]
    public bool LogWarning = true;
    [Tooltip("Unexpected situations that will impact the game flow")]
    public bool LogError = true;
    [Tooltip("The code trace from Unity about an error")]
    public bool LogExceptions = true;

#if UNITY_EDITOR
    private void OnValidate() {
      SetLogLevels();
      this.LogDebug("=== UE: SERVICES INITIALIZATION START ===");
      InitializeAllServices();
      this.LogDebug("=== UE: SERVICES INITIALIZATION COMPLETE ===");
    }

    private void SetLogLevels() {
      //update log levels only if they have changed
      if(LogDebug != LoggerExtension.EnabledLogLevels[LoggerExtension.LoggerLevels.Debug])
        LoggerExtension.SetLogLevel(LoggerExtension.LoggerLevels.Debug, LogDebug);

      if(LogInfo != LoggerExtension.EnabledLogLevels[LoggerExtension.LoggerLevels.Info])
        LoggerExtension.SetLogLevel(LoggerExtension.LoggerLevels.Info, LogInfo);

      if(LogWarning != LoggerExtension.EnabledLogLevels[LoggerExtension.LoggerLevels.Warning])
        LoggerExtension.SetLogLevel(LoggerExtension.LoggerLevels.Warning, LogWarning);

      if(LogError != LoggerExtension.EnabledLogLevels[LoggerExtension.LoggerLevels.Error])
        LoggerExtension.SetLogLevel(LoggerExtension.LoggerLevels.Error, LogError);

      if(LogExceptions != LoggerExtension.EnabledLogLevels[LoggerExtension.LoggerLevels.Exception])
        LoggerExtension.SetLogLevel(LoggerExtension.LoggerLevels.Exception, LogExceptions);
    }

#endif


    private void Awake() {
      this.LogDebug("=== SERVICES INITIALIZATION START ===");
      InitializeAllServices();

      if (ConnectGameStateMachineEvents) {
        InitializeGameStateIntegration();
      }
      this.LogDebug("=== SERVICES INITIALIZATION COMPLETE ===");
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
        this.LogDebug($"  - {type.Name}");
      }
    }

  }
}
