using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// Handles all changes in the UI when the GameState changes.
  /// </summary>
  public class UIStateHandler : GameStateHandler {

    [Header("UI References")]
    public GUIController GUI;

    private void OnEnable() {
      Priority = 100;
      HandlerName = nameof(UIStateHandler);
    }

    protected override void Init() {
      if (GUI == null) {
        GUI = FindFirstObjectByType<GUIController>();
      }
    }

    protected override void ToPreparation() => GUI.UpdateUIFromGameState(GameStates.Preparation);

    protected override void ToReset() => GUI.UpdateUIFromGameState(GameStates.Reset);

    protected override void ToPlaying() => GUI.UpdateUIFromGameState(GameStates.Playing);
  }

}
