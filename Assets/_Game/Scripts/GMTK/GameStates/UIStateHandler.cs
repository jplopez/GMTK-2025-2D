using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Handles all changes in the UI when the GameState changes.
  /// </summary>
  public class UIStateHandler : GameStateHandler {

    [Header("UI References")]
    public HudController hudController;
    public TurorialController tutorialController;

    private void OnEnable() {
      Priority = 100;
      HandlerName = nameof(UIStateHandler);
    }

    public void Awake() {
      if (hudController == null) {
        hudController = FindFirstObjectByType<HudController>();
      }

      if (tutorialController == null) {
        tutorialController = FindFirstObjectByType<TurorialController>();
      }
    }

    protected override void ToPreparation() {
      hudController.UpdateUIFromGameState(GameStates.Preparation);
      tutorialController.ToggleTutorialBoxes(false);
    }

    protected override void ToReset() {
      hudController.UpdateUIFromGameState(GameStates.Reset);
    }

    protected override void ToPlaying() {
      hudController.UpdateUIFromGameState(GameStates.Playing);
      tutorialController.ToggleTutorialBoxes(false);
    }
  }

}
