using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK {
  /// <summary>
  /// Convenient Lookup for GameContext
  /// </summary>
  public static class Game {
    public static GameContext Context {
      get {
        if (_context == null) Init();
        if (_context == null) _context = GameObject.FindAnyObjectByType<GameContext>();
        if (_context == null) {
          Debug.LogWarning("GameContext not found, will retry on next access request");
        }
        return _context;
      }
      private set => _context = value;
    }

    private static GameContext _context;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init() {
      if (_context != null) return;

      try {
        _context = GameObject.FindAnyObjectByType<GameContext>();
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

      if (_context == null) {
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