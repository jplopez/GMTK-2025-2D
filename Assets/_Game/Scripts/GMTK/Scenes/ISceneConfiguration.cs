namespace GMTK {

  /// <summary>
  /// Interface for components that extend the functionality of a <see cref="SceneController"/> instance.
  /// <br/>
  /// See:
  /// <list type="bullet">
  /// <item><see cref="RaiseGameEventConfig"/>: customize the raise of a specific event upon scene load</item>
  /// <item><see cref="SceneTransitionConfig"/>: specifies a transition scene loaded between the current and next scene upon calling to LoadNextScene</item>
  /// <item><see cref="ServicesInitConfig"/>: config with high execution piority, used to pre-load resources</item>
  /// <item><see cref="EndSceneConfig"/>: config with high execution priority, used as clean up before ending the game</item>
  /// </list>
  /// </summary>
  public interface ISceneConfigExtension {
    void ApplyConfig(SceneController controller);
    bool CanApplyOnType(SceneType type);
  }

}