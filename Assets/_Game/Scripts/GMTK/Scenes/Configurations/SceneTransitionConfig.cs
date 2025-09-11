using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This configuration component can be added to any SceneController to define a transition scene before the actual next scene.
  /// </summary>
  [AddComponentMenu("GMTK/Scene Configurations/Scene Transition Config")]
  public class SceneTransitionConfig : MonoBehaviour, ISceneConfigExtension {

    [Tooltip("The transition scene name")]
    public string TransitionSceneName;

    [Tooltip("The configuration for the transition scene")]
    public LevelConfig Config;

    private LevelConfig _originalConfig;

    /// <summary>
    /// The way this component works is that it will set the 'TransitionSceneName' on the SceneController, and then Transition config will have the actual next scene.
    /// This allows for a transition scene to be used before the actual next scene.
    /// </summary>
    /// <param name="controller"></param>
    public void ApplyConfig(SceneController controller) {
      if (controller == null) return;

      var currentConfig = controller.GetLevelConfig();
      //clone the current config to restore later
      _originalConfig = new LevelConfig() {
        //SceneName = currentConfig.SceneName,
        ConfigName = currentConfig.ConfigName,
        //Type = currentConfig.Type,
        SetStateOnLoad = currentConfig.SetStateOnLoad,
        InitialGameState = currentConfig.InitialGameState,
        CanRestart = currentConfig.CanRestart,
        CanSkip = currentConfig.CanSkip,
        //ComputeNextSceneName = currentConfig.ComputeNextSceneName,
        //PreviousSceneName = currentConfig.PreviousSceneName,
        LoadDelay = currentConfig.LoadDelay,
        //UseCustomLoadMethod = currentConfig.UseCustomLoadMethod,
        //CustomLoadMethod = currentConfig.CustomLoadMethod
      };
      //set the next scene to be the transition scene
      //currentConfig.ComputeNextSceneName = Config.SceneName;
      currentConfig.LoadDelay = Config.LoadDelay;
      controller.SetLevelConfig(currentConfig);

      //Config.ComputeNextSceneName = _originalConfig.ComputeNextSceneName;
      //Config.PreviousSceneName = Config.SceneName;
      Config.LoadDelay = _originalConfig.LoadDelay;
    }

    public bool CanApplyOnType(SceneType type) => type != SceneType.End;
  }
}