using System.Collections.Generic;
using UnityEngine;

namespace Ameba {
  /// <summary>
  /// Base ScriptableObject for managing runtime handlers with automatic discovery and execution
  /// </summary>
  public abstract class HandlerRegistry : ScriptableObject {

    [Header("Configuration")]
    [Tooltip("If true, automatically scans for handlers on initialization")]
    public bool AutoScanOnInitialize = true;

    [Tooltip("If true, logs detailed information about handler registration")]
    public bool EnableDebugLogging = true;

    [Tooltip("Maximum time to wait for handlers to register (in seconds)")]
    public float HandlerRegistrationTimeout = 2f;

    [Header("Handler Discovery")]
    [Tooltip("Tags to include when scanning for handlers (empty = scan all)")]
    public string[] IncludeTags = new string[0];

    [Tooltip("Tags to exclude when scanning for handlers")]
    public string[] ExcludeTags = new string[0];

    protected bool _isInitialized = false;

    /// <summary>
    /// Initialize the registry (scan for handlers if AutoScanOnInitialize is true)
    /// </summary>
    public virtual void Initialize() {
      if (AutoScanOnInitialize) {
        ScanForHandlers();
      }
      _isInitialized = true;
    }

    /// <summary>
    /// Scan for handlers in the current scene
    /// </summary>
    public abstract void ScanForHandlers();

    /// <summary>
    /// Clear all registered handlers
    /// </summary>
    public abstract void ClearAllHandlers();

    /// <summary>
    /// Filter GameObjects by include/exclude tags
    /// </summary>
    protected GameObject[] FilterGameObjectsByTags(GameObject[] allObjects) {
      if (IncludeTags.Length == 0 && ExcludeTags.Length == 0) {
        return allObjects;
      }

      var filtered = new List<GameObject>();

      foreach (var go in allObjects) {
        // Include filter
        if (IncludeTags.Length > 0) {
          bool includeMatch = false;
          foreach (var tag in IncludeTags) {
            if (go.CompareTag(tag)) {
              includeMatch = true;
              break;
            }
          }
          if (!includeMatch) continue;
        }

        // Exclude filter
        if (ExcludeTags.Length > 0) {
          bool excludeMatch = false;
          foreach (var tag in ExcludeTags) {
            if (go.CompareTag(tag)) {
              excludeMatch = true;
              break;
            }
          }
          if (excludeMatch) continue;
        }

        filtered.Add(go);
      }

      return filtered.ToArray();
    }

    /// <summary>
    /// Log debug information if debug logging is enabled
    /// </summary>
    protected void LogDebug(string message) {
      if (EnableDebugLogging) {
        Debug.Log($"[{GetType().Name}] {message}");
      }
    }

    /// <summary>
    /// Log warning information
    /// </summary>
    protected void LogWarning(string message) {
      Debug.LogWarning($"[{GetType().Name}] {message}");
    }

    /// <summary>
    /// Log error information
    /// </summary>
    protected void LogError(string message) {
      Debug.LogError($"[{GetType().Name}] {message}");
    }

    // Inspector helpers
    public bool IsInitialized => _isInitialized;

    [ContextMenu("Force Initialize")]
    private void ForceInitialize() => Initialize();

    [ContextMenu("Clear All Handlers")]
    private void ForceClearAll() => ClearAllHandlers();
  }
}
