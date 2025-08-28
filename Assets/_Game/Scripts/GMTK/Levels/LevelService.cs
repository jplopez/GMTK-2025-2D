using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ameba;

namespace GMTK {

  [CreateAssetMenu(menuName = "GMTK/Level Service", fileName = "LevelService")]
  public class LevelService : ScriptableObject {

    [Header("Level Configurations")]
    [Tooltip("All level configurations in the game")]
    public LevelConfig[] Levels;

    [Header("Runtime State")]
    [SerializeField] private int _currentLevelIndex = -1;
    [SerializeField] private string _currentSceneName;

    [System.Serializable]
    public class LevelConfig {
      [Header("Basic Info")]
      public string SceneName;
      public string DisplayName;
      public SceneType Type = SceneType.Level;

      [Header("Game State")]
      public GameStates InitialGameState = GameStates.Preparation;
      public bool SetStateOnLoad = true;

      [Header("Scene Management")]
      public bool IsUnlocked = true;
      public bool CanRestart = true;
      public bool CanSkip = false;

      [Header("Progression")]
      public string[] UnlockConditions;
      public string NextSceneName;
      public string PreviousSceneName;

      //[Header("Optional Overrides")]
      public float LoadDelay = 0f;
      public bool UseCustomLoadMethod = false;
      public string CustomLoadMethod;
    }

    public enum SceneType {
      Start,      // First scene. Splash screen or intro movie.
      End,        //  GameOver, Credits, etc.
      Level,      // Actual gameplay levels  
      Transition, // Loading, LevelComplete
      Special     // LevelDesigner, etc.
    }

    // Current level properties
    public LevelConfig CurrentLevel => _currentLevelIndex >= 0 && _currentLevelIndex < Levels.Length
        ? Levels[_currentLevelIndex] : null;

    public string CurrentSceneName => _currentSceneName;
    public int CurrentLevelIndex => _currentLevelIndex;

    /// <summary>
    /// Get level configuration by scene name
    /// </summary>
    public LevelConfig GetLevelConfig(string sceneName) {
      foreach (var level in Levels) {
        if (level.SceneName == sceneName) {
          return level;
        }
      }
      return null;
    }

    public bool TryGetLevelConfig(string sceneName, out LevelConfig levelConfig) {
      levelConfig = GetLevelConfig(sceneName);
      return levelConfig != null;
    }

    /// <summary>
    /// Get level configuration by index
    /// </summary>
    public LevelConfig GetLevelConfig(int index) {
      if (index >= 0 && index < Levels.Length) {
        return Levels[index];
      }
      return null;
    }

    public bool TryGetLevelConfig(int index, out LevelConfig levelConfig) {
      levelConfig = GetLevelConfig(index);
      return levelConfig != null;
    }

    /// <summary>
    /// Set current level and update runtime state
    /// </summary>
    public void SetCurrentLevel(string sceneName) {
      _currentSceneName = sceneName;
      _currentLevelIndex = GetLevelIndex(sceneName);

      Debug.Log($"[LevelService] Current level set to: {sceneName} (index {_currentLevelIndex})");
    }

    /// <summary>
    /// Get the index of a level by scene name
    /// </summary>
    public int GetLevelIndex(string sceneName) {
      for (int i = 0; i < Levels.Length; i++) {
        if (Levels[i].SceneName == sceneName) {
          return i;
        }
      }
      return -1;
    }

    /// <summary>
    /// Get next level configuration
    /// </summary>
    public LevelConfig GetNextLevel() {
      if (CurrentLevel != null && !string.IsNullOrEmpty(CurrentLevel.NextSceneName)) {
        return GetLevelConfig(CurrentLevel.NextSceneName);
      }

      // Fallback to index-based progression
      if (_currentLevelIndex >= 0 && _currentLevelIndex < Levels.Length - 1) {
        return Levels[_currentLevelIndex + 1];
      }

      return null;
    }

    public bool TryGetNextLevel(out LevelConfig nextLevelConfig) {
      nextLevelConfig = GetNextLevel();
      return nextLevelConfig != null;
    }

    /// <summary>
    /// Get previous level configuration
    /// </summary>
    public LevelConfig GetPreviousLevel() {
      if (CurrentLevel != null && !string.IsNullOrEmpty(CurrentLevel.PreviousSceneName)) {
        return GetLevelConfig(CurrentLevel.PreviousSceneName);
      }

      // Fallback to index-based progression
      if (_currentLevelIndex > 0) {
        return Levels[_currentLevelIndex - 1];
      }

      return null;
    }

    public bool TryGetPreviousLevel(out LevelConfig prevLevelConfig) {
      prevLevelConfig = GetPreviousLevel();
      return prevLevelConfig != null;
    }

    /// <summary>
    /// Check if level is unlocked
    /// </summary>
    public bool IsLevelUnlocked(string sceneName) {
      var config = GetLevelConfig(sceneName);
      return config?.IsUnlocked ?? false;
    }

    /// <summary>
    /// Unlock a level
    /// </summary>
    public void UnlockLevel(string sceneName) {
      var config = GetLevelConfig(sceneName);
      if (config != null) {
        config.IsUnlocked = true;
        Debug.Log($"[LevelService] Level unlocked: {sceneName}");
      }
    }

    // Navigation helpers
    public bool HasNextLevel() => GetNextLevel() != null;
    public bool HasPreviousLevel() => GetPreviousLevel() != null;

    // Editor utilities
    [ContextMenu("Sort Levels by Scene Name")]
    private void SortLevelsBySceneName() {
      Array.Sort(Levels, (a, b) => string.Compare(a.SceneName, b.SceneName));
      Debug.Log("[LevelService] Levels sorted by scene name");
    }

    [ContextMenu("Auto-Detect Scenes")]
    private void AutoDetectScenes() {
      // This would scan for scene files and create basic configurations
      // Implementation depends on your specific needs
      Debug.Log("[LevelService] Auto-detect scenes feature - implement as needed");
    }
  }
}
