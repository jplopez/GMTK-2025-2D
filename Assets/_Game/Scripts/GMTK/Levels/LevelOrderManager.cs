
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
  [CreateAssetMenu(menuName = "GMTK/Level Order Manager", fileName = "LevelOrderManager")]
  public class LevelOrderManager : ScriptableObject {

    [System.Serializable]
    public class LevelMapping {
      [Tooltip("The gameplay level (SceneType.Level)")]
      public string LevelSceneName;
      [Tooltip("Scene to load when this level is completed (could be transition, cutscene, etc.)")]
      public string CompletionSceneName;
      [Tooltip("The next actual gameplay level")]
      public string NextLevelSceneName;
      [Tooltip("Display order for UI/menus")]
      public int DisplayOrder;
    }

    [Header("Level Order Configuration")]
    [Tooltip("Maps level progression including intermediate scenes")]
    public LevelMapping[] LevelMappings;

    private LevelService _levelService;

    public void Initialize(LevelService levelService) {
      _levelService = levelService;
    }

    /// <summary>
    /// Get the scene that should be loaded when a level is completed
    /// </summary>
    public string GetCompletionSceneForLevel(string levelSceneName) {
      var mapping = GetLevelMapping(levelSceneName);
      if (mapping != null && !string.IsNullOrEmpty(mapping.CompletionSceneName)) {
        return mapping.CompletionSceneName;
      }

      // Fallback to LevelConfig.ComputeNextSceneName
      var levelConfig = (_levelService != null) ? _levelService.GetConfigBySceneName(levelSceneName) : null;
      return levelConfig?.LevelCompleteSceneName;
    }

    /// <summary>
    /// Get the next actual gameplay level (skipping intermediate scenes)
    /// </summary>
    public string GetNextGameplayLevel(string currentLevelSceneName) {
      var mapping = GetLevelMapping(currentLevelSceneName);
      return mapping?.NextLevelSceneName;
    }

    /// <summary>
    /// Get the scene that should be loaded to play the next level
    /// </summary>
    public string GetNextLevelScene(string currentLevelSceneName) {
      var nextLevel = GetNextGameplayLevel(currentLevelSceneName);
      if (string.IsNullOrEmpty(nextLevel)) return null;

      // The scene to load is always the level scene itself
      return nextLevel;
    }

    /// <summary>
    /// Get all gameplay levels in display order
    /// </summary>
    public string[] GetLevelsInOrder() {
      return LevelMappings
          .OrderBy(m => m.DisplayOrder)
          .Select(m => m.LevelSceneName)
          .ToArray();
    }

    /// <summary>
    /// Check if there's a next level after the current one
    /// </summary>
    public bool HasNextLevel(string currentLevelSceneName) {
      return !string.IsNullOrEmpty(GetNextGameplayLevel(currentLevelSceneName));
    }

    /// <summary>
    /// Get level mapping by scene name
    /// </summary>
    private LevelMapping GetLevelMapping(string levelSceneName) {
      return LevelMappings?.FirstOrDefault(m => m.LevelSceneName == levelSceneName);
    }

    /// <summary>
    /// Get the display order of a level
    /// </summary>
    public int GetLevelDisplayOrder(string levelSceneName) {
      var mapping = GetLevelMapping(levelSceneName);
      return mapping?.DisplayOrder ?? -1;
    }
  }
}