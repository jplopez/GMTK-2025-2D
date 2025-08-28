using System;
using System.Collections;
using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Abstract base class for game-specific initialization components.
  /// Each game should have exactly one implementation that defines which 
  /// ScriptableObjects need to be loaded before GameObjects start their lifecycle.
  /// </summary>
  public abstract class InitializationComponent : MonoBehaviour {

    /// <summary>
    /// Start the initialization process. Called by InitializationManager.
    /// </summary>
    public void StartInitialization() {
      StartCoroutine(InitializeAllScriptableObjects());
    }

    /// <summary>
    /// Implement this method to define which ScriptableObjects your game needs to load.
    /// Make sure to call InitializationManager.MarkInitializationComplete() when done.
    /// </summary>
    protected abstract IEnumerator InitializeAllScriptableObjects();

    /// <summary>
    /// Helper method to load resources asynchronously with proper error handling.
    /// </summary>
    protected IEnumerator LoadResourceAsync<T>(string resourceName, Action<T> onLoaded) where T : ScriptableObject {
      var request = Resources.LoadAsync<T>(resourceName);
      yield return request;

      if (request.asset is T resource) {
        onLoaded?.Invoke(resource);
        Debug.Log($"Loaded {resourceName}");
      }
      else {
        Debug.LogError($"Failed to load {resourceName}");
      }
    }

    /// <summary>
    /// Helper method to load resources with retry logic for WebGL compatibility.
    /// </summary>
    protected IEnumerator LoadResourceWithRetry<T>(string resourceName, Action<T> onLoaded, int maxRetries = 3) where T : ScriptableObject {
      for (int attempt = 0; attempt < maxRetries; attempt++) {
        var request = Resources.LoadAsync<T>(resourceName);
        yield return request;

        if (request.asset is T resource) {
          onLoaded?.Invoke(resource);
          Debug.Log($"Loaded {resourceName} (attempt {attempt + 1})");
          yield break; // Success, exit the retry loop
        }

        Debug.LogWarning($"Failed to load {resourceName} (attempt {attempt + 1}/{maxRetries})");
        if (attempt < maxRetries - 1) {
          yield return new WaitForSeconds(0.1f); // Brief delay before retry
        }
      }

      Debug.LogError($"Failed to load {resourceName} after {maxRetries} attempts");
    }
  }
}
