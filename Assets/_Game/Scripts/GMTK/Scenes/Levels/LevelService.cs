using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GMTK {

  public enum SceneType {
    Start,      // First scene. Splash screen or intro movie.
    End,        //  GameOver, Credits, etc.
    Level,      // Actual gameplay levels  
    Transition, // Loading, LevelComplete
    Special     // LevelDesigner, etc.
  }

  public enum SceneConfigSource {
    Preset,   // Specify the config using a preset config
    Manual,       // Manually set the level (not recommended)
  }

  [CreateAssetMenu(menuName = "GMTK/Level Service", fileName = "LevelService")]
  public class LevelService : ScriptableObject {

    [Header("Level Configurations")]
    [Tooltip("All level configurations in the game")]
    public LevelConfig[] Configurations;

    //[Header("Start Scene")]
    public SceneConfigSource startSceneConfigSource = SceneConfigSource.Preset;
    public string StartSceneName;
    public LevelConfig StartSceneConfig;

    //[Header("End Scene")]
    public SceneConfigSource endSceneConfigSource = SceneConfigSource.Preset;
    public string EndSceneName;
    public LevelConfig EndSceneConfig;

    [Header("Game Levels")]
    [Tooltip("Ordered List of scene names that are considered game levels")]
    [SerializeField] List<string> _gameLevelSceneNames = new();
    [Tooltip("The index of the current game level")]
    [SerializeField] protected int _currentLevelIndex = -1;

    #region Current level

    public string CurrentLevelSceneName {
      get {
        return (_currentLevelIndex >= 0 && _currentLevelIndex < _gameLevelSceneNames.Count)
        ? _gameLevelSceneNames[_currentLevelIndex] : null;
      }
    }

    public int CurrentLevelIndex => _currentLevelIndex;

    #endregion

    #region LevelConfig Management

    public LevelConfig FindConfig(string configName) => Configurations.FirstOrDefault(level => level.ConfigName == configName);

    public bool TryFindConfig(string configName, out LevelConfig levelConfig) {
      levelConfig = FindConfig(configName);
      return levelConfig != null;
    }

    /// <summary>
    /// Get level configuration by index
    /// </summary>
    public LevelConfig LevelConfigAtIndex(int index) {
      if (index >= 0 && index < Configurations.Length) {
        return Configurations[index];
      }
      return null;
    }

    public bool TryLevelConfigAtIndex(int index, out LevelConfig levelConfig) {
      levelConfig = LevelConfigAtIndex(index);
      return levelConfig != null;
    }

    public int GetLevelIndex(string sceneName) => string.IsNullOrEmpty(sceneName) ? -1 : _gameLevelSceneNames.IndexOf(sceneName);

    public bool TryGetLevelIndex(string sceneName, out int index) {
      index = GetLevelIndex(sceneName);
      return index >= 0;
    }

    public string GetLevelSceneName(int index) {
      if (index >= 0 && index < _gameLevelSceneNames.Count) {
        return _gameLevelSceneNames[index];
      }
      return null;
    }
    public bool TryGetLevelSceneName(int index, out string sceneName) {
      sceneName = GetLevelSceneName(index);
      return !string.IsNullOrEmpty(sceneName);
    }

    public int ConfigCount => Configurations?.Length ?? 0;
    public bool EmptyConfigs => Configurations == null || Configurations.Length == 0;

    #endregion

    #region Level Navigation

    public IReadOnlyList<string> GameLevelSceneNames => _gameLevelSceneNames.AsReadOnly();

    public int GameLevelCount => _gameLevelSceneNames.Count;

    public bool EmptyLevels => _gameLevelSceneNames == null || _gameLevelSceneNames.Count == 0;

    public bool IsFirstLevel() => _currentLevelIndex == 0;

    public bool IsLastLevel() => _currentLevelIndex == GameLevelCount - 1;

    public bool IsCurrentSceneCurrentLevel() => IsSceneCurrentLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

    public bool IsSceneCurrentLevel(string sceneName) => sceneName == CurrentLevelSceneName;

    public void MoveToFirstLevel() => MoveToLevel(0);

    public void MoveToEndLevel() => MoveToLevel(GameLevelCount - 1);

    public void MoveToLevel(int index) {
      if (index >= 0 && index < GameLevelCount) {
        _currentLevelIndex = index;
      }
      else {
        Debug.LogWarning($"[LevelService] Cannot move to level at index {index}. Index out of range.");
      }
    }

    public void AdvanceToNextLevel() {

      if (HasNextLevel()) {
        _currentLevelIndex++;
      }
      else {
        Debug.LogWarning("[LevelService] Cannot advance to next level. Already at the last level.");
      }
    }

    /// <summary>
    /// Get next level scene name. If we're in the last level, returns null.
    /// </summary>
    public string GetNextLevelSceneName() {
      if (!IsLastLevel() && (_currentLevelIndex + 1) < GameLevelCount) {
        return _gameLevelSceneNames[_currentLevelIndex + 1];
      }
      return null;
    }

    public bool TryGetNextLevelSceneName(out string sceneName) {
      sceneName = GetNextLevelSceneName();
      return !string.IsNullOrEmpty(sceneName);
    }

    // Navigation helpers
    public bool HasNextLevel() => !IsLastLevel() && (_currentLevelIndex + 1) < GameLevelCount;

    public bool HasPreviousLevel() => !IsFirstLevel() && (_currentLevelIndex - 1) >= 0;

    #endregion

    #region Current/Next Scene calculation

    /// <summary>
    /// Determines the name of the next scene for <c>sceneName</c>, based on the provided LevelConfig and GameState.
    /// </summary>
    public string ComputeNextSceneName(string sceneName, LevelConfig config, GameStates currentState) {
      var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
      // By default, stay in the provided scene
      var nextScene = sceneName;

      switch (currentState) {
        case GameStates.Start:
          //Start scene always moves to first level
          _currentLevelIndex = 0;
          nextScene = GetLevelSceneName(0); break;
        case GameStates.Gameover:
          //Gameover states always moves start scene
          _currentLevelIndex = 0;
          nextScene = StartSceneName; break;
        // Next cases depend if we are in a level or not and if config defines a level complete scene
        case GameStates.Preparation:
        case GameStates.Playing:
        case GameStates.Pause:
        case GameStates.LevelComplete:
          // If we are in the last level, stay in current scene
          if (!HasNextLevel()) {
            Debug.LogWarning("[LevelService] No next level available. Staying in current scene.");
            break;
          }
          // If we are playing a level, next scene depends if config has a level complete scene.
          if (config.HasLevelCompleteScene && sceneName != config.LevelCompleteSceneName) {
            nextScene = config.LevelCompleteSceneName;
          }
          else {
            nextScene = GetNextLevelSceneName();
          }
          break;
      }
      return nextScene;
    }

    public bool TryComputeNextSceneName(string sceneName, LevelConfig config, GameStates current, out string nextSceneName) {
      nextSceneName = ComputeNextSceneName(sceneName, config, current);
      return !string.IsNullOrEmpty(nextSceneName);
    }

    #endregion

    public string[] GetConfigNames() => Configurations.Select(c => c.ConfigName).ToArray();

    public string[] GetLevelOrderSceneNames => _gameLevelSceneNames.ToArray();

    // Editor utilities
    [ContextMenu("Sort Configurations by Config Name")]
    private void SortConfigsByConfigName() {
      Array.Sort(Configurations, (a, b) => string.Compare(a.ConfigName, b.ConfigName));
      //Debug.Log("[LevelService] Configurations sorted by config name");
    }

    [ContextMenu("Auto-Detect Scenes")]
    private void AutoDetectScenes() {
      // This would scan for scene files and create basic configurations
      // Implementation depends on your specific needs
      Debug.Log("[LevelService] Auto-detect scenes feature - implement as needed");
    }
  }
}
