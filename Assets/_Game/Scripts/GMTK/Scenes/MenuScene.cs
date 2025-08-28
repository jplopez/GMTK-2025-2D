using UnityEngine;

namespace GMTK {

  [AddComponentMenu("GMTK/Scenes/Start Scene")]
  public class MenuScene : SceneController {

    [Header("Start Configuration")]
    public bool AutoFocusFirstButton = true;
    public bool PlayBackgroundMusic = true;

    protected override void OnSceneInitialized() {
      LogDebug("Start scene initialized");

      if (AutoFocusFirstButton) {
        // Focus first UI button
      }

      if (PlayBackgroundMusic) {
        // Start background music
      }
    }

    protected override void ApplyLevelConfiguration() {
      base.ApplyLevelConfiguration();

      // Start-specific configuration
      LogDebug("Applying menu-specific configuration");
    }
  }
}
