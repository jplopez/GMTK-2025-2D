using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// Enum defining the Event types supported by the InputActionEventChannel class.
  /// </summary>
  public enum InputActionType {
    PointerPosition,
    Select,
    Unselect, //release of Select press button. Useful to track dragging
    Secondary,//secondary button on mouse. Not available for touch
    Cancel,   //explicit cancel button
    RotateCW, // CW : clockwise, or "right"
    RotateCCW,//CCW : counter clockwise, or "left"
    FlipX,    //flip on the X axis, ie: Up-Down 
    FlipY,    //flip on the Y axis, ie: Left-Right
    Pause,
    Escape,
  }

  /// <summary>
  /// Event arguments wrapper to include as payload on events raised by the InputActionEventChannel.
  /// </summary>
  public class InputActionEventArgs : EventArgs {
    public InputActionType ActionType { get; }
    public InputActionPhase Phase { get; }
    public InputAction.CallbackContext Context { get; }
    public Vector2 ScreenPos { get;}
    public Vector3 WorldPos { get;}

    public InputActionEventArgs(InputActionType actionType, InputActionPhase phase, InputAction.CallbackContext context) {
      ActionType = actionType;
      Phase = phase;
      Context = context;
    }

    public InputActionEventArgs(InputActionType actionType, InputActionPhase phase, InputAction.CallbackContext context, Vector2 screenPos, Vector3 worldPos) {
      ActionType = actionType;
      Phase = phase;
      Context = context;
      ScreenPos = screenPos;
      WorldPos = worldPos;
    }

  }

  /// <summary>
  /// _eventChannel for InputAction events. This channel works with the PlayerControl class to simplify the detection of player inputs across the game
  /// </summary>
  [CreateAssetMenu(fileName = "InputActionEventChannel" , menuName = "GMTK/InputAction Event Channel")]
  public class InputActionEventChannel : EventChannel<InputActionType> {  }
}
