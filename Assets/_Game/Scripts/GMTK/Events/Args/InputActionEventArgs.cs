using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// Event arguments wrapper to include as payload on events raised by the InputActionEventChannel.
  /// </summary>
  public class InputActionEventArgs : EventArgs {
    public GameEventType InputEvent { get; }
    public InputActionPhase Phase { get; }
    public InputAction.CallbackContext Context { get; }
    public Vector2 ScreenPos { get;}
    public Vector3 WorldPos { get;}

    public InputActionEventArgs(GameEventType inputEvent, InputActionPhase phase, InputAction.CallbackContext context) {
      InputEvent = inputEvent;
      Phase = phase;
      Context = context;
    }

    public InputActionEventArgs(GameEventType inputEvent, InputActionPhase phase, InputAction.CallbackContext context, Vector2 screenPos, Vector3 worldPos) {
      InputEvent = inputEvent;
      Phase = phase;
      Context = context;
      ScreenPos = screenPos;
      WorldPos = worldPos;
    }

  }
}
