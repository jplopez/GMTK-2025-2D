using System;
using UnityEngine;

namespace GMTK {

  [CreateAssetMenu(fileName = "_levelSequence", menuName = "GMTK/Level Sequence", order = 1)]
  public class LevelSequence : ScriptableObject {

    [Tooltip("Ordered list of scene names representing the levels in this sequence.")]
    public string[] LevelSceneNames;
    [Tooltip("Current scene name in the sequence.")]
    public string CurrentScene;

    private int _currentIndex = -1;

    public void SetCurrentScene(string sceneName) {
      // Allow setting if it's the first time or if it's the next level in the sequence
      if (_currentIndex == -1 ||HasNextLevel(sceneName)) {
        _currentIndex = Array.IndexOf(LevelSceneNames, sceneName);
        CurrentScene = sceneName;
      } else {
        Debug.LogWarning($"Attempted to set current scene to '{sceneName}' which is not in the _levelSequence or is not the next level.");
      }
    }

    public string GetNextLevel() => GetNextLevel(CurrentScene);

    public string GetNextLevel(string sceneName) {
      if (LevelSceneNames == null || LevelSceneNames.Length == 0) {
        Debug.LogWarning("_levelSequence has no levels defined.");
        return null;
      }
      int index = Array.IndexOf(LevelSceneNames, sceneName);
      if (index >= 0 && index < LevelSceneNames.Length - 1) {
        return LevelSceneNames[index + 1];
      }
      return null; // No next level
    }

    public bool HasNextLevel() => GetNextLevel(CurrentScene) != null;
    public bool HasNextLevel(string sceneName) => GetNextLevel(sceneName) != null;
  }
}