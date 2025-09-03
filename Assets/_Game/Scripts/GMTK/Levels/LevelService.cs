using System;
using UnityEngine;
using System.Linq;

namespace GMTK {

  public enum SceneType {
    Start,      // First scene. Splash screen or intro movie.
    End,        //  GameOver, Credits, etc.
    Level,      // Actual gameplay levels  
    Transition, // Loading, LevelComplete
    Special     // LevelDesigner, etc.
  }

  public enum StartLevelTypes {
    SceneName,   // Specify the level by the scene name (default)
    FirstLevel,  // The first level in the Levels array
    Config       // A preset configuration
  }

  public enum EndLevelTypes {
    SceneName,   // Specify the level by the scene name (default)
    LastLevel,   // The last level in the Levels array 
    Config       // A preset configuration
  }

  [CreateAssetMenu(menuName = "GMTK/Level Service", fileName = "LevelService")]
  public class LevelService : ScriptableObject {

    [Header("Level Configurations")]
    [Tooltip("All level configurations in the game")]
    public LevelConfig[] Levels;

    [Header("Current Level")]
    [SerializeField] private int _currentLevelIndex = -1;
    [SerializeField] private string _currentSceneName;

    [Header("Start Level")]
    public StartLevelTypes startLevelType = StartLevelTypes.SceneName;
    [SerializeField] protected string _startSceneName;
    [SerializeField] protected LevelConfig _startLevelConfig;

    [Header("End Level")]
    public EndLevelTypes endLevelType = EndLevelTypes.SceneName;
    [SerializeField] protected string _endSceneName;
    [SerializeField] protected LevelConfig _endLevelConfig;


    // Current level properties
    public LevelConfig CurrentLevel => _currentLevelIndex >= 0 && _currentLevelIndex < Levels.Length
        ? Levels[_currentLevelIndex] : null;

    public string CurrentSceneName => _currentSceneName;
    public int CurrentLevelIndex => _currentLevelIndex;

    // Start Level properties
    public LevelConfig StartLevel {
      get {
        return startLevelType switch {
          StartLevelTypes.SceneName => GetLevelConfig(_startSceneName),
          StartLevelTypes.FirstLevel => Levels.Length > 0 ? Levels[0] : null,
          StartLevelTypes.Config => _startLevelConfig,
          _ => null,
        };
      }
    }

    public void SetStartLevel(string sceneName) {
      _startSceneName = sceneName;
      startLevelType = StartLevelTypes.SceneName;
    }
    public void SetStartLevel(LevelConfig config) {
      _startLevelConfig = config;
      startLevelType = StartLevelTypes.Config;
    }

    // End Level properties

    public LevelConfig EndLevel {
      get {
        return endLevelType switch {
          EndLevelTypes.LastLevel => Levels.Length > 0 ? Levels[^1] : null,
          EndLevelTypes.SceneName => GetLevelConfig(_endSceneName),
          EndLevelTypes.Config => _endLevelConfig,
          _ => null,
        };
      }
    }

    public void SetEndLevel(string sceneName) {
      _endSceneName = sceneName;
      endLevelType = EndLevelTypes.SceneName;
    }
    public void SetEndLevel(LevelConfig config) {
      _endLevelConfig = config;
      endLevelType = EndLevelTypes.Config;
    }

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

    public LevelConfig FirstStartConfig() => Levels.First(l => l.Type == SceneType.Start);

    public LevelConfig FirstEndConfig() => Levels.First(l => l.Type == SceneType.End);

    public LevelConfig[] GetConfigsByType(SceneType type) =>
          Levels.Where(l => l.Type.Equals(type)).ToArray();

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
