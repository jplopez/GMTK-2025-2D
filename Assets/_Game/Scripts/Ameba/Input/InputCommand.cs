using UnityEngine.InputSystem;

namespace Ameba.Input {
  public abstract class InputCommand {
    public abstract void Execute(InputAction.CallbackContext context);
    public virtual void Cancel() { }
    public virtual void Hold(float duration) { }
  }
}