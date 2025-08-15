using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK {
  /// <summary>
  /// Convenient Lookup for GameContext
  /// </summary>
  public static class Game {
    public static GameContext Context { get; private set; }

    [RuntimeInitializeOnLoadMethod]
    private static void Init() {
      if (Context != null) return;

      try {
        Context = GameObject.FindAnyObjectByType<GameContext>();
      }
      catch (Exception ex) {
        Debug.LogError($"GameContext failed to load in scene '{SceneManager.GetActiveScene().name}'.");
        Debug.LogError("GameContext failed to load. Make sure a GameContext MonoBehaviour exists in the active scene or is loaded via addressables.");
        Debug.LogException(ex);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif      
      }

        if (Context == null) {
        Debug.LogError($"GameContext not found in scene '{SceneManager.GetActiveScene().name}'.");
        Debug.LogError("GameContext is missing. Make sure a GameContext MonoBehaviour exists in the active scene or is loaded via addressables.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
      }
    }
  }
}