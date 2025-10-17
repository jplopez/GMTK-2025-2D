using UnityEngine;

namespace GMTK {
  public class InputStateHandler : BaseGameStateHandler {

    [Header("Input References")]
    public PlayableElementInputHandler snappableInput;

    private void OnEnable() {
      Priority = 50;
      HandlerName = nameof(InputStateHandler);
    }

    protected override void Init() {
      if (snappableInput == null) {
        snappableInput = FindFirstObjectByType<PlayableElementInputHandler>();
      }
    }

    protected override void ToPreparation() => snappableInput.UpdateFromGameState(GameStates.Preparation);
    protected override void ToPlaying() => snappableInput.UpdateFromGameState(GameStates.Playing);
    protected override void ToPause() => snappableInput.UpdateFromGameState(GameStates.Pause);
    protected override void ToOptions() => snappableInput.UpdateFromGameState(GameStates.Options);
  }

}
