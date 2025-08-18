// /Assets/_Game/Scripts/GMTK/StateHandlers/GameStateHandlerRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Ameba;

namespace GMTK {
  [CreateAssetMenu(menuName = "GMTK/Game State Handler Registry", fileName = "GameStateHandlerRegistry")]
  public class GameStateHandlerRegistry : HandlerRegistry {

    [Header("Runtime State")]
    [SerializeField, DisplayWithoutEdit]
    private List<HandlerInfo> _registeredHandlers = new();

    [System.Serializable]
    private class HandlerInfo {
      public string name;
      public string typeName;
      public int priority;
      public bool isEnabled;
      public GameObject gameObject;
      [System.NonSerialized] public GameStateHandler handler;
    }

    // Runtime collections
    private List<GameStateHandler> _activeHandlers = new();

    /// <summary>
    /// Scans the current scene for GameState handlers
    /// </summary>
    public override void ScanForHandlers() {
      LogDebug("Starting handler scan...");

      ClearAllHandlers();

      // Find all GameObjects in the scene
      var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None); //   FindObjectsOfType<GameObject>(includeInactive: false);
      var scannedObjects = FilterGameObjectsByTags(allGameObjects);

      // Scan for handlers
      ScanForHandlersInObjects(scannedObjects);

      // Sort handlers by priority
      _activeHandlers = _activeHandlers.OrderBy(h => h.Priority).ToList();

      _isInitialized = true;

      LogDebug($"Scan complete! Found {_activeHandlers.Count} handlers");
      if (EnableDebugLogging) {
        LogRegisteredHandlers();
      }
    }

    /// <summary>
    /// Dispatches state change to all registered handlers
    /// </summary>
    public void HandleStateChange(StateMachineEventArg<GameStates> eventArg) {
      if (!_isInitialized) {
        LogWarning("Registry not initialized. Call Initialize() first.");
        return;
      }

      // Validate transition using GameStateMachine.TestTransition
      if (!Game.Context.StateMachine.TestTransition(eventArg.FromState, eventArg.ToState)) {
        LogWarning($"State transition {eventArg.FromState} -> {eventArg.ToState} failed validation");
        return;
      }

      LogDebug($"Handling state change: {eventArg.FromState} -> {eventArg.ToState}");

      // Execute handlers in priority order
      foreach (var handler in _activeHandlers) {
        if (!handler.IsEnabled) continue;

        try {
          handler.HandleStateChange(eventArg);
          LogDebug($"✓ {handler.HandlerName} handled state change");
        }
        catch (System.Exception ex) {
          LogError($"✗ Handler '{handler.HandlerName}' failed: {ex.Message}");
          Debug.LogException(ex);
        }
      }
    }

    /// <summary>
    /// Manually register a handler (useful for dynamically created objects)
    /// </summary>
    public void RegisterHandler(GameStateHandler handler, GameObject source = null) {
      if (_activeHandlers.Contains(handler)) return;

      _activeHandlers.Add(handler);
      _activeHandlers = _activeHandlers.OrderBy(h => h.Priority).ToList();

      var info = new HandlerInfo {
        name = handler.HandlerName,
        typeName = handler.GetType().Name,
        priority = handler.Priority,
        isEnabled = handler.IsEnabled,
        gameObject = source,
        handler = handler
      };

      _registeredHandlers.Add(info);
      LogDebug($"Manually registered handler: {handler.HandlerName}");
    }

    /// <summary>
    /// Manually unregister a handler
    /// </summary>
    public void UnregisterHandler(GameStateHandler handler) {
      _activeHandlers.Remove(handler);
      _registeredHandlers.RemoveAll(h => h.handler == handler);
      LogDebug($"Unregistered handler: {handler.HandlerName}");
    }

    /// <summary>
    /// Clear all registered handlers
    /// </summary>
    public override void ClearAllHandlers() {
      _activeHandlers.Clear();
      _registeredHandlers.Clear();
      _isInitialized = false;
    }

    private void ScanForHandlersInObjects(GameObject[] gameObjects) {
      foreach (var go in gameObjects) {
        var handlers = go.GetComponents<MonoBehaviour>().OfType<GameStateHandler>();

        foreach (var handler in handlers) {
          RegisterHandler(handler, go);
        }
      }
    }

    private void LogRegisteredHandlers() {
      LogDebug("=== Registered State Handlers ===");
      foreach (var info in _registeredHandlers.OrderBy(h => h.priority)) {
        LogDebug($"  [{info.priority:D3}] {info.name} ({info.typeName}) - {(info.isEnabled ? "ENABLED" : "DISABLED")}");
      }
    }

    // Inspector helpers
    public int HandlerCount => _activeHandlers?.Count ?? 0;

    [ContextMenu("Force Scan Handlers")]
    private void ForceScan() => ScanForHandlers();
  }

}
