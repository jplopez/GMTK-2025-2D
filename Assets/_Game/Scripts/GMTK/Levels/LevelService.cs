using Ameba;
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
    ConfigName,   // Specify the level by the scene name (default)
    Manual,       // Manually set the level (not recommended)
  }

  [CreateAssetMenu(menuName = "GMTK/Level Service", fileName = "LevelService")]
  public class LevelService : ScriptableObject {

    [Header("Level Configurations")]
    [Tooltip("All level configurations in the game")]
    public LevelConfig[] Configurations;

    [Header("Start Scene")]
    public SceneConfigSource startSceneConfigSource = SceneConfigSource.ConfigName;
    [SerializeField] protected string _startSceneName;
    [SerializeField] protected LevelConfig _startSceneConfig;

    [Header("End Scene")]
    public SceneConfigSource endSceneConfigSource = SceneConfigSource.ConfigName;
    [SerializeField] protected string _endSceneName;
    [SerializeField] protected LevelConfig _endSceneConfig;

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

    public LevelConfig CurrentLevelConfig {
      get {
        return string.IsNullOrEmpty(CurrentLevelSceneName) ? null : Configurations.FirstOrDefault(level => level.SceneName == CurrentLevelSceneName);
      }
    }

    public int CurrentLevelIndex => _currentLevelIndex;

    #endregion

    #region Start/End Scenes
    public LevelConfig StartSceneConfig {
      get {
        return startSceneConfigSource switch {
          SceneConfigSource.ConfigName => GetConfigBySceneName(_startSceneName),
          SceneConfigSource.Manual => _startSceneConfig,
          _ => null,
        };
      }
    }

    public void SetStartSceneWithSceneName(string sceneName) {
      if (string.IsNullOrEmpty(sceneName)) {
        Debug.LogWarning("[LevelService] Start scene name cannot be null or empty. Keeping previous start scene.");
        return;
      }

      if (TryGetConfigBySceneName(sceneName, out var config)) {
        _startSceneConfig = config;
        _startSceneName = sceneName;
      }
      else {
        Debug.LogWarning($"[LevelService] No LevelConfig found for scene name '{sceneName}'. Keeping previous start scene.");
        return;
      }
    }

    public void SetStartSceneWithConfigName(string configName) {
      if (string.IsNullOrEmpty(configName)) {
        Debug.LogWarning("[LevelService] Start scene config name cannot be null or empty. Keeping previous start scene.");
        return;
      }

      if (TryGetConfigByName(configName, out var config)) {
        _startSceneConfig = config;
        _startSceneName = config.SceneName;
      }
      else {
        Debug.LogWarning($"[LevelService] No LevelConfig found for config name '{configName}'. Keeping previous start scene.");
        return;
      }
    }

    // End Scene properties

    public LevelConfig EndSceneConfig {
      get {
        return endSceneConfigSource switch {
          SceneConfigSource.ConfigName => GetConfigBySceneName(_endSceneName),
          SceneConfigSource.Manual => _endSceneConfig,
          _ => null,
        };
      }
    }

    public void SetEndSceneWithSceneName(string sceneName) {
      if (string.IsNullOrEmpty(sceneName)) {
        Debug.LogWarning("[LevelService] End scene name cannot be null or empty. Keeping previous end scene.");
        return;
      }

      if (TryGetConfigBySceneName(sceneName, out var config)) {
        _endSceneConfig = config;
        _endSceneName = sceneName;
      }
      else {
        Debug.LogWarning($"[LevelService] No LevelConfig found for scene name '{sceneName}'. Keeping previous end scene.");
        return;
      }

    }
    public void SetEndSceneWithConfigName(string configName) {
      if (string.IsNullOrEmpty(configName)) {
        Debug.LogWarning("[LevelService] End scene config name cannot be null or empty. Keeping previous end scene.");
        return;
      }

      if (TryGetConfigByName(configName, out var config)) {
        _endSceneConfig = config;
        _endSceneName = config.SceneName;
      }
      else {
        Debug.LogWarning($"[LevelService] No LevelConfig found for config name '{configName}'. Keeping previous end scene.");
        return;
      }
    }

    #endregion

    #region LevelConfig Management

    /// <summary>
    /// Get level configuration by scene name
    /// </summary>
    public LevelConfig GetConfigBySceneName(string sceneName) => Configurations.FirstOrDefault(level => level.SceneName == sceneName);

    public bool TryGetConfigBySceneName(string sceneName, out LevelConfig levelConfig) {
      levelConfig = GetConfigBySceneName(sceneName);
      return levelConfig != null;
    }
    public LevelConfig GetConfigByName(string configName) => Configurations.FirstOrDefault(level => level.ConfigName == configName);

    public bool TryGetConfigByName(string configName, out LevelConfig levelConfig) {
      levelConfig = GetConfigByName(configName);
      return levelConfig != null;
    }

    /// <summary>
    /// Get level configuration by index
    /// </summary>
    public LevelConfig GetLevelConfig(int index) {
      if (index >= 0 && index < Configurations.Length) {
        return Configurations[index];
      }
      return null;
    }

    public bool TryGetLevelConfig(int index, out LevelConfig levelConfig) {
      levelConfig = GetLevelConfig(index);
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

    #endregion

    #region Level Navigation

    public IReadOnlyList<string> GameLevelSceneNames => _gameLevelSceneNames.AsReadOnly();

    public int GameLevelCount => _gameLevelSceneNames.Count;

    public bool IsFirstLevel() => _currentLevelIndex == 0;

    public bool IsLastLevel() => _currentLevelIndex == _gameLevelSceneNames.Count - 1;

    public bool IsCurrentSceneCurrentLevel() => IsSceneCurrentLevel(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

    public bool IsSceneCurrentLevel(string sceneName) => sceneName == CurrentLevelSceneName;

    public void MoveToStartScene() => _currentLevelIndex = _gameLevelSceneNames.Count > 0 ? 0 : -1;

    public void MoveToEndScene() => _currentLevelIndex = _gameLevelSceneNames.Count - 1;

    public void AdvanceToNextLevel() {

      if (HasNextLevel()) {
        _currentLevelIndex++;
      }
      else {
        Debug.LogWarning("[LevelService] Cannot advance to next level. Already at the last level.");
      }
    }

    /// <summary>
    /// Get next level configuration
    /// </summary>
    public LevelConfig GetNextLevelConfig() {
      if (CurrentLevelConfig != null && !IsLastLevel()) {
        var nextLevelSceneName = _gameLevelSceneNames[_currentLevelIndex + 1];
        if (TryGetConfigBySceneName(nextLevelSceneName, out var nextLevelConfig)) {
          return nextLevelConfig;
        }
        else {
          Debug.LogWarning($"[LevelService] Next level scene name '{nextLevelSceneName}' not found in configurations.");
          return null;
        }
      }
      else {
        Debug.LogWarning("[LevelService] No current level or already at last level.");
        return null;
      }
    }

    public bool TryGetNextLevelConfig(out LevelConfig nextLevelConfig) {
      nextLevelConfig = GetNextLevelConfig();
      return nextLevelConfig != null;
    }

    public string GetNextLevelSceneName() => GetNextLevelConfig()?.SceneName;

    public bool TryGetNextLevelSceneName(out string sceneName) {
      sceneName = GetNextLevelSceneName();
      return !string.IsNullOrEmpty(sceneName);
    }

    // Navigation helpers
    public bool HasNextLevel() => GetNextLevelConfig() != null;
    public bool HasPreviousLevel() {
      if (CurrentLevelConfig != null && !IsFirstLevel()) {
        var prevLevelSceneName = _gameLevelSceneNames[_currentLevelIndex - 1];
        return TryGetConfigBySceneName(prevLevelSceneName, out _);
      }
      return false;
    }

    #endregion

    #region Current/Next Scene calculation

    /// <summary>
    /// Determines the name of the next scene to load based on the current game state and active scene.
    /// </summary>
    /// <remarks>The next scene is determined by the current game state, as managed by the <see
    /// cref="GameStateMachine"/>, and the active scene. Special cases include transitioning between levels, handling
    /// level completion, and ensuring the active scene aligns with the expected game state. If the game state or scene
    /// configuration is inconsistent, a warning is logged, and the current level is loaded as a fallback.</remarks>
    /// <returns>The name of the next scene to load. This may be the current scene, a level-specific scene, or a scene associated
    /// with the next game state, depending on the game's state and configuration.</returns>
    public string ComputeNextSceneName() {
      var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
      // By default, stay in the current scene
      var nextScene = activeScene;
      if (ServiceLocator.TryGet(out GameStateMachine stateMachine)) {
        switch (stateMachine.Current) {
          case GameStates.Start:
            nextScene = StartSceneConfig.SceneName; break;
          case GameStates.Gameover:
            nextScene = EndSceneConfig.SceneName; break;
          case GameStates.Preparation:
          case GameStates.Playing:
          case GameStates.Pause:
            // If we are playing a level, next scene depends if we are in the current level or not, and if the current level has a level complete scene.
            if (IsSceneCurrentLevel(activeScene)) {
              nextScene = CurrentLevelConfig.HasLevelCompleteScene ? CurrentLevelConfig.LevelCompleteSceneName : GetNextLevelSceneName();
            }
            else {
              // Otherwise, is a bug because we we shouldn't be playing a level that isnt the current level
              Debug.LogWarning("[LevelService] Current game state is in-level but active scene is not the current level. Loading current level."); break;
            }
            break;
          case GameStates.LevelComplete:
            // If current level has a dedicated level complete scene, go there
            if (_gameLevelSceneNames.Contains(CurrentLevelConfig.SceneName)) {
              nextScene = CurrentLevelConfig.LevelCompleteSceneName; break;
            }
            // If we are already in the level complete scene, go to next level
            if (activeScene == CurrentLevelConfig.LevelCompleteSceneName) {
              nextScene = GetNextLevelSceneName(); break;
            }
            break;
        }
      }
      return nextScene;
    }

    public bool TryComputeNextSceneName(out string sceneName) {
      sceneName = ComputeNextSceneName();
      return !string.IsNullOrEmpty(sceneName);
    }

    #endregion

    // Editor utilities
    [ContextMenu("Sort Configurations by Scene Name")]
    private void SortLevelsBySceneName() {
      Array.Sort(Configurations, (a, b) => string.Compare(a.SceneName, b.SceneName));
      Debug.Log("[LevelService] Configurations sorted by scene name");
    }

    [ContextMenu("Auto-Detect Scenes")]
    private void AutoDetectScenes() {
      // This would scan for scene files and create basic configurations
      // Implementation depends on your specific needs
      Debug.Log("[LevelService] Auto-detect scenes feature - implement as needed");
    }
  }
}
