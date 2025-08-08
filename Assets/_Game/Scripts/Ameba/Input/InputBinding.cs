using System;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ameba.Input {

  [Serializable]
  public class InputBinding {

    public string ActionName = "";
    public UnityEvent<InputAction.CallbackContext> Started;
    public UnityEvent<InputAction.CallbackContext> Performed;
    public UnityEvent<InputAction.CallbackContext> Canceled;

    public InputBinding(string actionName) => ActionName = actionName;

    public InputBinding(string actionName,
    UnityEvent<InputAction.CallbackContext> onStarted,
    UnityEvent<InputAction.CallbackContext> onPerformed,
    UnityEvent<InputAction.CallbackContext> onCanceled) {
      ActionName = actionName;
      Started = onStarted;
      Performed = onPerformed;
      Canceled = onCanceled;
    }

    public virtual void Invoke(InputAction.CallbackContext context) {
      if (!ValidateBeforeInvoke(context)) return;
      switch (context.phase) {
        case InputActionPhase.Started: InnerInvoke(Started, context); break;
        case InputActionPhase.Performed: InnerInvoke(Performed,context); break;
        case InputActionPhase.Canceled: InnerInvoke(Canceled, context); break;
      }
    }

    protected virtual void InnerInvoke(UnityEvent<InputAction.CallbackContext> unityEvent, InputAction.CallbackContext context) {
      if (unityEvent != null)// && unityEvent.GetPersistentEventCount() > 0)
        unityEvent.Invoke(context);
    }

    protected virtual bool ValidateBeforeInvoke(InputAction.CallbackContext context) => true;

  }

  [Serializable]
  public class InputBindingEvent {

    public UnityEvent<InputAction.CallbackContext> callback;

    public InputBindingEvent(UnityEvent<InputAction.CallbackContext> callback) {
      this.callback = callback;
    }

    public InputBindingEvent(Action<InputAction.CallbackContext> callback) {
      UnityAction<InputAction.CallbackContext> action = new(callback);
      this.callback = new UnityEvent<InputAction.CallbackContext>();
      this.callback.AddListener(action);
    }

    public void Invoke(InputAction.CallbackContext context) => callback?.Invoke(context);
  }

}