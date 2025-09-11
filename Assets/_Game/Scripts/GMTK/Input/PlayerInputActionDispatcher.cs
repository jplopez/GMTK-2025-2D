using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// This class translates the InputAction (button pressed, pointer position) into game events (move element, rotate, etc) and determine 
  /// the Screen and World position of the pointer (mouse cursor).
  /// The events are raised through <see cref="InputActionEventChannel"/>.
  /// </summary>
  public class PlayerInputActionDispatcher : MonoBehaviour, PlayerControls.IGameplayActions {

    //Reference to the _eventChannel used to propagate InputAction events
    public InputActionEventChannel InputEvents { get; private set; }
    public Vector2 PointerScreenPosition { get; private set; }
    public Vector3 PointerWorldPoition { get; private set; }

    private PlayerControls.GameplayActions gameplayActions;

    public PlayerInputActionDispatcher(InputActionEventChannel inputEvents) {
      InputEvents = inputEvents;
    }
    private void Awake() {
     
      //Getting the _eventChannel with 2 fallbacks
      if (InputEvents == null) {
        InputEvents = ServiceLocator.Get<InputActionEventChannel>();
      }
      if (InputEvents == null) {
        InputEvents = Resources.Load<InputActionEventChannel>("InputActionEventChannel");
      }
      if (InputEvents == null) {
        this.LogError($"InputActionEventChannel instance could not be found. The EventDispatcher will not work");
        return;
      }
      try {
        PlayerControls controls = new();
        gameplayActions = controls.Gameplay;
        gameplayActions.Enable();
        gameplayActions.AddCallbacks(this);
      }
      catch (Exception e) {
        this.LogError($"Enabling ActionMap threw an exception: {e.Message}");
#if UNITY_EDITOR
        this.LogException(e);
#endif
        throw;
      }
      this.Log($"EventDispatcher initialized");
    }

    public void OnDestroy() {
      try {
        gameplayActions.Disable();
      } catch (Exception e) {
        this.LogWarning($"Disabling ActionMap ended in exception: {e.Message}");
      }
    }

    /// <summary>
    /// This method updates both ScreenPos and WorldPos properties
    /// </summary>
    /// <param name="context"></param>
    public void OnPointerPosition(InputAction.CallbackContext context) {
      Vector2 pointerPos = context.ReadValue<Vector2>();
      if (pointerPos != PointerScreenPosition) {
        PointerScreenPosition = pointerPos;
        if (Camera.main == null) {
          PointerWorldPoition = Vector3.zero;
        }
        else {
          //obtain world position from Camera, forzing z to zero to maintaing the 2d restrictions
          var camWorldPos = Camera.main.ScreenToWorldPoint(PointerScreenPosition);
          PointerWorldPoition = new(camWorldPos.x, camWorldPos.y, 0f);
          InputEvents.Raise(InputActionType.PointerPosition, BuildEventArgs(InputActionType.PointerPosition, context));
        }
      }
    }

    /// <summary>
    /// This method checks if the 'Select' InputAction was done over a GridSnappable component.<br/>
    /// In which case, starts tracking the element dragging.<br/>
    /// When tracking, PointerScreenPosition and PointerWorldPosition will return where 
    /// element's position.
    /// </summary>
    /// <param name="context"></param>
    public void OnSelect(InputAction.CallbackContext context) => InputEvents.Raise(InputActionType.Select, BuildEventArgs(InputActionType.Select, context));

    public void OnSecondary(InputAction.CallbackContext context) => InputEvents.Raise(InputActionType.Secondary, BuildEventArgs(InputActionType.Secondary, context));

    public void OnCancel(InputAction.CallbackContext context) => InputEvents.Raise(InputActionType.Cancel, BuildEventArgs(InputActionType.Cancel, context));

    public void OnRotateCW(InputAction.CallbackContext context) => InputEvents.Raise(InputActionType.RotateCW, BuildEventArgs(InputActionType.RotateCW, context));

    public void OnRotateCCW(InputAction.CallbackContext context) => InputEvents.Raise(InputActionType.RotateCCW, BuildEventArgs(InputActionType.RotateCCW, context));

    public void OnFlipX(InputAction.CallbackContext context) => InputEvents.Raise(InputActionType.FlipX, BuildEventArgs(InputActionType.FlipX, context));

    public void OnFlipY(InputAction.CallbackContext context) => InputEvents.Raise(InputActionType.FlipY, BuildEventArgs(InputActionType.FlipY, context));

    private EventArgs BuildEventArgs(InputActionType actionType, InputAction.CallbackContext context) => new InputActionEventArgs(actionType, context.phase, context, PointerScreenPosition, PointerWorldPoition);
  }
}