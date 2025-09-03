using System;
using System.Collections;
using UnityEngine;

namespace Ameba {

  /// <summary>
  /// Framework-level initialization manager that ensures ScriptableObjects 
  /// are loaded before any GameObjects start their lifecycle.
  /// Games should extend InitializationComponent to define their specific resources.
  /// </summary>
  public static class InitializationManager {

    public static bool IsInitialized { get; private set; } = false;
    public static event Action OnInitializationComplete;

    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() {
      IsInitialized = false;
      OnInitializationComplete = null;
    }

    //[RuntimeInitializeOnLoadMethod  (RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeScene() {
      Debug.Log("InitializationManager: Starting pre-scene initialization");

      var initObject = new GameObject("_InitializationManager");

      // Try to find a game-specific initialization component
      var initComponent = FindInitializationComponent();
      if (initComponent != null) {
        var component = initObject.AddComponent(initComponent);
        GameObject.DontDestroyOnLoad(initObject);

        if (component is InitializationComponent baseComponent) {
          baseComponent.StartInitialization();
        }
        else {
          Debug.LogError($"Found initialization component {initComponent.Name} but it doesn't inherit from InitializationComponent");
          MarkInitializationComplete(); // Fail gracefully
        }
      }
      else {
        Debug.LogWarning("No InitializationComponent implementation found. Skipping resource pre-loading.");
        MarkInitializationComplete(); // Complete immediately if no custom component
      }
    }

    /// <summary>
    /// Finds the first InitializationComponent implementation in the current assembly.
    /// Games should have exactly one implementation.
    /// </summary>
    private static System.Type FindInitializationComponent() {
      var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
      foreach (var assembly in assemblies) {
        try {
          var types = assembly.GetTypes();
          foreach (var type in types) {
            if (type.IsSubclassOf(typeof(InitializationComponent)) && !type.IsAbstract) {
              Debug.Log($"Found initialization component: {type.Name}");
              return type;
            }
          }
        }
        catch (System.Exception ex) {
          // Some assemblies might not be accessible, skip them
          Debug.LogWarning($"Could not scan assembly {assembly.FullName}: {ex.Message}");
        }
      }
      return null;
    }

    /// <summary>
    /// Wait for initialization to complete before executing the callback.
    /// Use this in MonoBehaviour.Awake() methods that need ScriptableObjects.
    /// </summary>
    public static void WaitForInitialization(MonoBehaviour caller, Action onComplete) {
      if (IsInitialized) {
        onComplete?.Invoke();
        return;
      }

      caller.StartCoroutine(WaitForInitializationCoroutine(onComplete));
    }

    /// <summary>
    /// Coroutine version for manual control
    /// </summary>
    public static IEnumerator WaitForInitializationCoroutine(Action onComplete = null) {
      while (!IsInitialized) {
        yield return null;
      }
      onComplete?.Invoke();
    }

    /// <summary>
    /// Mark initialization as complete. Should be called by InitializationComponent implementations.
    /// </summary>
    public static void MarkInitializationComplete() {
      if (IsInitialized) return; // Prevent double completion

      IsInitialized = true;
      OnInitializationComplete?.Invoke();
      Debug.Log("InitializationManager: All ScriptableObjects initialized successfully");
    }
  }
}
