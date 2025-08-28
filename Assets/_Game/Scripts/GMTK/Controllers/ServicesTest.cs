using UnityEngine;
using Ameba;

namespace GMTK {

  /// <summary>
  /// Simple test component to verify Services are working correctly
  /// </summary>
  public class ServicesTest : MonoBehaviour {

    [Header("Test Results")]
    [SerializeField] private bool _gameEventChannelFound;
    [SerializeField] private bool _gameStateMachineFound;
    [SerializeField] private bool _handlerRegistryFound;
    [SerializeField] private bool _levelServiceFound;

    private void Start() {
      TestServices();
    }

    [ContextMenu("Test Services")]
    public void TestServices() {
      Debug.Log("[ServicesTest] Testing service locator...");

      // Test GameEventChannel
      var eventChannel = Services.Get<GameEventChannel>();
      _gameEventChannelFound = eventChannel != null;
      Debug.Log($"[ServicesTest] GameEventChannel: {(_gameEventChannelFound ? "? FOUND" : "? NOT FOUND")}");

      // Test GameStateMachine  
      var stateMachine = Services.Get<GameStateMachine>();
      _gameStateMachineFound = stateMachine != null;
      Debug.Log($"[ServicesTest] GameStateMachine: {(_gameStateMachineFound ? "? FOUND" : "? NOT FOUND")}");

      // Test HandlerRegistry
      var handlerRegistry = Services.Get<GameStateHandlerRegistry>();
      _handlerRegistryFound = handlerRegistry != null;
      Debug.Log($"[ServicesTest] GameStateHandlerRegistry: {(_handlerRegistryFound ? "? FOUND" : "? NOT FOUND")}");

      var levelService = Services.Get<LevelService>();
      _levelServiceFound = levelService != null;
      Debug.Log($"[ServicesTest] LevelService: {(_levelServiceFound ? "? FOUND" : "? NOT FOUND")}");

      // Show all registered services
      var registeredTypes = Services.GetRegisteredTypes();
      Debug.Log($"[ServicesTest] Total services registered: {registeredTypes.Length}");
      foreach (var type in registeredTypes) {
        Debug.Log($"[ServicesTest] - {type.Name}");
      }
    }
  }
}
