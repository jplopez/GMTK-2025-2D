using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Handles all changes in the UI when the GameState changes.
  /// </summary>
  public class UIStateHandler : GameStateHandler {

    [Header("UI References")]
    public HudController Hud;
    public TutorialController Tutorial;

    private void OnEnable() {
      Priority = 100;
      HandlerName = nameof(UIStateHandler);
    }

    protected override void Init() {
      if (Hud == null) {
        Hud = FindFirstObjectByType<HudController>();
      }

      if (Tutorial == null) {
        Tutorial = FindFirstObjectByType<TutorialController>();
      }
    }

    protected override void ToPreparation() {
      Hud.UpdateUIFromGameState(GameStates.Preparation);
      // Show tutorial boxes if enabled on component
      Tutorial.ToggleTutorialBoxes(Tutorial.ShowOnStart);
    }

    protected override void ToReset() {
      Hud.UpdateUIFromGameState(GameStates.Reset);
      Tutorial.ToggleTutorialBoxes(false);
    }

    protected override void ToPlaying() {
      Hud.UpdateUIFromGameState(GameStates.Playing);
      Tutorial.ToggleTutorialBoxes(false);
    }
  }

}
