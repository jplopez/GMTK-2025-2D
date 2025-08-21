using UnityEngine;

namespace GMTK {
  public class InputStateHandler : GameStateHandler {

    [Header("Input References")]
    public SnappableInputHandler snappableInput;

    private void OnEnable() {
      Priority = 50;
      HandlerName = nameof(InputStateHandler);
    }

    protected override void Awake() {
      base.Awake();
      if (snappableInput == null) {
        snappableInput = FindFirstObjectByType<SnappableInputHandler>();
      }
    }

    protected override void ToPreparation() => snappableInput.UpdateFromGameState(GameStates.Preparation);
    protected override void ToPlaying() => snappableInput.UpdateFromGameState(GameStates.Playing);
    protected override void ToPause() => snappableInput.UpdateFromGameState(GameStates.Pause);
    protected override void ToOptions() => snappableInput.UpdateFromGameState(GameStates.Options);
  }

}
