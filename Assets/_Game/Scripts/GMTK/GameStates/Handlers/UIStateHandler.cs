using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Handles all changes in the UI when the GameState changes.
  /// </summary>
  public class UIStateHandler : GameStateHandler {

    [Header("UI References")]
    public HudController HudController;
    public TurorialController TutorialController;

    private void OnEnable() {
      Priority = 100;
      HandlerName = nameof(UIStateHandler);
    }

    protected override void Awake() {
      base.Awake();
      if (HudController == null) {
        HudController = FindFirstObjectByType<HudController>();
      }

      if (TutorialController == null) {
        TutorialController = FindFirstObjectByType<TurorialController>();
      }
    }

    protected override void ToPreparation() {
      HudController.UpdateUIFromGameState(GameStates.Preparation);
      TutorialController.ToggleTutorialBoxes(false);
    }

    protected override void ToReset() {
      HudController.UpdateUIFromGameState(GameStates.Reset);
    }

    protected override void ToPlaying() {
      HudController.UpdateUIFromGameState(GameStates.Playing);
      TutorialController.ToggleTutorialBoxes(false);
    }
  }

}
