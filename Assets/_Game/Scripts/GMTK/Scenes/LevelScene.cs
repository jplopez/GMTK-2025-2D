using Ameba;
using UnityEngine;

namespace GMTK {

  [AddComponentMenu("GMTK/Scenes/Level Scene")]

  public class LevelScene : SceneController {

    [Header("Level Configuration")]
    public bool AutoScanForHandlers = true;
    public bool ResetScoreOnLoad = true;

    protected override void OnSceneInitialized() {
      LogDebug("Level scene initialized");

      if (AutoScanForHandlers) {
        var handlerRegistry = Services.Get<GameStateHandlerRegistry>();
        if(handlerRegistry != null) handlerRegistry.ScanForHandlers();
      }

      if (ResetScoreOnLoad) {
        _eventChannel.Raise(GameEventType.ScoreChanged, 0);
      }
    }

    protected override void ApplyLevelConfiguration() {
      base.ApplyLevelConfiguration();

      // Level-specific configuration
      LogDebug("Setting up level-specific systems");
    }
  }
}
