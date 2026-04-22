using UnityEngine;
using UnityEngine.Serialization;

namespace GMTK {
  public class InputStateHandler : BaseGameStateHandler {

    [Header("Input References")]
    [FormerlySerializedAs("snappableInput")] 
    public PlayableElementInputHandler playerInputHandler;

    private void OnEnable() {
      Priority = 50;
      HandlerName = nameof(InputStateHandler);
    }

    protected override void Init() {
      if (playerInputHandler == null) {
        playerInputHandler = FindFirstObjectByType<PlayableElementInputHandler>();
      }
    }

    protected override void ToPreparation() => playerInputHandler.UpdateFromGameState(GameStates.Preparation);
    protected override void ToPlaying() => playerInputHandler.UpdateFromGameState(GameStates.Playing);
    protected override void ToPause() => playerInputHandler.UpdateFromGameState(GameStates.Pause);
    protected override void ToOptions() => playerInputHandler.UpdateFromGameState(GameStates.Options);
  }

}
