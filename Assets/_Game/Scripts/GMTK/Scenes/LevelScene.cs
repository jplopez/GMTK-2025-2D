using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Specialized SceneController tailored to game levels.
  /// Adds the notion of a Transition scene in between levels, like a 
  /// 'level complete' or similar.
  /// </summary>
  [AddComponentMenu("GMTK/Scenes/Level Scene")]
  public class LevelScene : SceneController {

    [Header("Level Complete Transition")]
    public bool SkipLevelComplete = false;
    public string TransitionConfig;

    protected LevelConfig _transitionConfig;
    protected override void OnSceneInitialized() {
      LogDebug("Level scene initialized");
    }

    protected override void ApplyLevelConfiguration() {
      base.ApplyLevelConfiguration();

      // Level-specific configuration
      LogDebug("Setting up level-specific systems");
    }

    protected virtual void LoadTransitionConfig() {
      var nextLevel = _levelService.GetNextLevel();
      if (SkipLevelComplete) {
        _transitionConfig = nextLevel;
        return;
      }
      if (string.IsNullOrEmpty(TransitionConfig)) {
        Debug.LogWarning($"[LevelScene] '{SceneName}' had no TransitionConfig defined. Falling back to normal next level logic");
        _transitionConfig = nextLevel;
        return;
      }

      if (_levelService.TryGetLevelConfig(TransitionConfig, out _transitionConfig)) {
        _transitionConfig.PreviousSceneName = SceneName;
        _transitionConfig.NextSceneName = nextLevel.NextSceneName;
        Debug.Log($"[LevelScene] '{SceneName}' transition config '{TransitionConfig}' loaded");
      }
    }

    protected override bool TryGetNextLevelConfig(out LevelConfig nextLevel) {
      if (_transitionConfig == null) {
        return _levelService.TryGetLevelConfig(SceneName, out nextLevel);
      } else {
        nextLevel = _transitionConfig;
        return true;
      }
    }
  }
}
