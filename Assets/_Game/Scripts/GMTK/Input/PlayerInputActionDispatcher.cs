using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// Partial class extending the Unity-generated <see cref="PlayerControls"/>
  /// and implementing the <see cref="IGameplayActions"/> interface
  /// to act as an Event dispatcher using the <see cref="InputActionEventChannel"/> 
  /// </summary>
  public class PlayerInputActionDispatcher : MonoBehaviour, PlayerControls.IGameplayActions {

    //Reference to the EventChannel used to propagate InputAction events
    public InputActionEventChannel InputEvents { get; private set; }
    public Vector2 PointerScreenPosition { get; private set; }
    public Vector3 PointerWorldPoition { get; private set; }

    private PlayerControls.GameplayActions gameplayActions;

    public PlayerInputActionDispatcher(InputActionEventChannel inputEvents) {
      InputEvents = inputEvents;
    }
    public void Awake() {
      //Getting the EventChannel with 2 fallbacks
      if (InputEvents == null) {
        InputEvents = Game.Context.InputEventsChannel;
      }
      if (InputEvents == null) {
        InputEvents = Resources.Load<InputActionEventChannel>("InputActionEventChannel");
      }
      if (InputEvents == null) {
        Debug.LogError($"PlayerInputActionDispatcher: InputActionEventChannel instance could not be found. The EventDispatcher will not work");
        return;
      }
      try {
        PlayerControls controls = new();
        gameplayActions = controls.Gameplay;
        gameplayActions.Enable();
        gameplayActions.AddCallbacks(this);
      }
      catch (Exception e) {
        Debug.LogError($"PlayerInputActionDispatcher: Enabling ActionMap threw an exception: {e.Message}");
#if UNITY_EDITOR
        Debug.LogException(e);
#endif
        throw;
      }
      Debug.Log($"PlayerInputActionDispatcher: EventDispatcher initialized");
    }

    public void OnDestroy() {
      try {
        gameplayActions.Disable();
      } catch (Exception e) {
        Debug.LogWarning($"PlayerInputActionDispatcher: Disabling ActionMap ended in exceptionv {e.Message}");
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
          InputEvents.Raise(InputActionType.PointerPosition, context.phase, context, PointerScreenPosition, PointerWorldPoition);
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
    public void OnSelect(InputAction.CallbackContext context) {
      InputEvents.Raise(InputActionType.Select, context.phase, context, PointerScreenPosition, PointerWorldPoition);
    }

    /// <summary>
    /// Not implemented yet
    /// </summary>
    /// <param name="context"></param>
    public void OnSecondary(InputAction.CallbackContext context) {
      Debug.Log("SnappableInputHandler.HandleSecondary : Not implemented yet");
      //InputEvents.Raise(InputActionType.Secondary, context.phase, context, PointerScreenPosition, PointerWorldPoition);
    }

    public void OnCancel(InputAction.CallbackContext context) {
      Debug.Log("SnappableInputHandler.HandleSecondary : Not implemented yet");
      InputEvents.Raise(InputActionType.Cancel, context.phase, context, PointerScreenPosition, PointerWorldPoition);
    }

    public void OnRotateCW(InputAction.CallbackContext context) {
      InputEvents.Raise(InputActionType.RotateCW, context.phase, context, PointerScreenPosition, PointerWorldPoition);
    }

    public void OnRotateCCW(InputAction.CallbackContext context) {
      InputEvents.Raise(InputActionType.RotateCCW, context.phase, context, PointerScreenPosition, PointerWorldPoition);
    }

    public void OnFlipX(InputAction.CallbackContext context) {
      InputEvents.Raise(InputActionType.FlipX, context.phase, context, PointerScreenPosition, PointerWorldPoition);
    }

    public void OnFlipY(InputAction.CallbackContext context) {
      InputEvents.Raise(InputActionType.FlipY, context.phase, context, PointerScreenPosition, PointerWorldPoition);
    }

  }
}