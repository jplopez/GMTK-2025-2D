using static GMTK.PlayerControls;
using static UnityEngine.InputSystem.InputAction;
using Ameba.Input;

namespace GMTK {

  public abstract class GameplayInputBase : InputHandlerBase, IGameplayActions {

    void IGameplayActions.OnCancel(CallbackContext context) => Registry.Handle("Cancel", context);

    void IGameplayActions.OnFlipX(CallbackContext context) => Registry.Handle("FlipX", context);

    void IGameplayActions.OnFlipY(CallbackContext context) => Registry.Handle("FlipY", context);

    void IGameplayActions.OnPointerPosition(CallbackContext context) => Registry.Handle("PointerPosition", context);

    void IGameplayActions.OnRotateCCW(CallbackContext context) => Registry.Handle("RotateCCW", context);

    void IGameplayActions.OnRotateCW(CallbackContext context) => Registry.Handle("RotateCW", context);

    void IGameplayActions.OnSecondary(CallbackContext context) => Registry.Handle("Secondary", context);

    void IGameplayActions.OnSelect(CallbackContext context) => Registry.Handle("Select", context);
  }

}