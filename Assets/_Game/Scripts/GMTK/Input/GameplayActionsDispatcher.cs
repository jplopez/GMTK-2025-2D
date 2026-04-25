using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// <para>
  /// This class implements the <see cref="PlayerControls.IGameplayActions"/> interface.
  /// Its main role is handling InputActions defined in the "Gameplay" ActionMap, and propagate
  /// them through the <see cref="GameEventChannel"/> system, and tracking the Pointer position
  /// in both Screen and World coordinates.<br/>
  /// It serves as an abstraction to Unity's Input System, translating player inputs to game events.
  /// </para>
  /// <para>
  /// The events are raised with an <see cref="InputActionEventArgs"/> argument,
  /// containing the original InputAction context and the current pointer position in both Screen and World coordinates.
  /// </para>
  /// </summary>
  public class GameplayActionsDispatcher : MonoBehaviour, PlayerControls.IGameplayActions {

    //Reference to the _eventChannel used to propagate InputAction events
    [Tooltip("Reference to the GameEventChannel used to propagate InputAction events")]
    [SerializeField] protected GameEventChannel eventChannel;

    [Tooltip("Current Pointer position in Screen coordinates")]
    [SerializeField] protected Vector2 pointerScreenPosition;

    [Tooltip("Current Pointer position in World coordinates")]
    [SerializeField] protected Vector3 pointerWorldPosition;

    [Tooltip("Minimum movement threshold to consider the pointer position has changed")]
    [Range(0.01f, 1f)]
    public float PointerMoveThreshold = 0.1f;

    [Tooltip("Frequency (in seconds) to raise Pointer Position events")]
    [Range(0.01f, 0.5f)]
    public float PointerEventFrequency = 0.02f;

    private PlayerControls.GameplayActions _gameplayActions;
    private float _timeSinceLastPointerEvent;

    private bool CanRaisePointerEvent => _timeSinceLastPointerEvent >= PointerEventFrequency;

    private void Awake() {
     
      //Getting the _eventChannel with 2 fallbacks
      if (eventChannel == null) {
        eventChannel = ServiceLocator.Get<GameEventChannel>();
      }
      if (eventChannel == null) {
        eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }
      if (eventChannel == null) {
        this.LogError($"GameEventChannel instance could not be found. The PlayerControlsDispatcher will not work");
        return;
      }
      try {
        PlayerControls controls = new();
        _gameplayActions = controls.Gameplay;
        _gameplayActions.Enable();
        _gameplayActions.AddCallbacks(this);
      }
      catch (Exception e) {
        this.LogError($"Enabling ActionMap threw an exception: {e.Message}");
#if UNITY_EDITOR
        this.LogException(e);
#endif
        throw;
      }
      this.Log($"PlayerControlsDispatcher initialized");
    }

    private void Update() {
      _timeSinceLastPointerEvent += Time.deltaTime;
    }

    public void OnDestroy() {
      try {
        _gameplayActions.Disable();
      } catch (Exception e) {
        this.LogWarning($"Disabling ActionMap ended in exception: {e.Message}");
      }
    }

    /// <summary>
    /// Handles updates to the pointer position based on the provided input context.
    /// </summary>
    /// <remarks>This method processes the pointer position and raises a pointer event if the position is
    /// successfully updated and pointer events are enabled. The event raised is of type <see
    /// cref="GameEventType.InputPointerPosition"/>.</remarks>
    /// <param name="context">The input context containing the pointer position data. The position is expected to be a <see cref="Vector2"/>
    /// value.</param>
    public void OnPointerPosition(InputAction.CallbackContext context) {
      if(TryUpdatePointerPosition(context.ReadValue<Vector2>())) {
        if(CanRaisePointerEvent) {
          RaiseEvent(GameEventType.InputPointerPosition, context);
          _timeSinceLastPointerEvent = 0f;
        }
      }
    }

    /// <summary>
    /// This method updates both ScreenPos and WorldPos properties if the pointer has moved enough from the last recorded position.
    /// </summary>
    /// <param name="pointerPos"></param>
    private bool TryUpdatePointerPosition(Vector2 pointerPos) {
      if (Vector2.Distance(pointerPos, pointerScreenPosition) >= PointerMoveThreshold) {
        pointerScreenPosition = pointerPos;
        if (Camera.main == null) {
          pointerWorldPosition = Vector3.zero;
        }
        else {
          //obtain world position from Camera, forcing z to zero to maintaining the 2d restrictions
          var camWorldPos = Camera.main.ScreenToWorldPoint(pointerScreenPosition);
          pointerWorldPosition = new(camWorldPos.x, camWorldPos.y, 0f);
        }
        //this.LogDebug($"Pointer position updated. ScreenPos: {pointerScreenPosition} | WorldPos: {pointerWorldPosition}");
        return true;
      }
      return false;
    }

    /// <summary>
    /// Input handler for the primary action (Select). Raises the <see cref="GameEventType.InputSelected"/> event.
    /// </summary>
    /// <param name="context"></param>
    public void OnSelect(InputAction.CallbackContext context) => RaiseEvent(GameEventType.InputSelected, context);

    /// <summary>
    /// Input handler for the secondary action. Raises the <see cref="GameEventType.InputSecondary"/> event.
    /// </summary>
    /// <param name="context"></param>
    public void OnSecondary(InputAction.CallbackContext context) => RaiseEvent(GameEventType.InputSecondary, context);

    /// <summary>
    /// Input handler for the cancel action. Raises the <see cref="GameEventType.InputCancel"/> event.
    /// </summary>
    /// <param name="context"></param>
    public void OnCancel(InputAction.CallbackContext context) => RaiseEvent(GameEventType.InputCancel, context);

    /// <summary>
    /// Input handler for the Rotate Clockwise. Raises the <see cref="GameEventType.InputRotateCW"/> event.
    /// </summary>
    /// <param name="context"></param>
    public void OnRotateCW(InputAction.CallbackContext context) => RaiseEvent(GameEventType.InputRotateCW, context);

    /// <summary>
    /// Input handler for the Rotate counterclockwise. Raises the <see cref="GameEventType.InputRotateCCW"/> event.
    /// </summary>
    /// <param name="context"></param>
    public void OnRotateCCW(InputAction.CallbackContext context) => RaiseEvent(GameEventType.InputRotateCCW, context);

    /// <summary>
    /// Input handler for the Flip in X axis. Raises the <see cref="GameEventType.InputFlippedX"/> event.
    /// </summary>
    /// <param name="context"></param>
    public void OnFlipX(InputAction.CallbackContext context) => RaiseEvent(GameEventType.InputFlippedX, context);

    /// <summary>
    /// Input handler for the Flip on Y axis. Raises the <see cref="GameEventType.InputFlippedY"/> event.
    /// </summary>
    /// <param name="context"></param>
    public void OnFlipY(InputAction.CallbackContext context) => RaiseEvent(GameEventType.InputFlippedY, context);


    private void RaiseEvent(GameEventType actionType, InputAction.CallbackContext context) {
      this.LogDebug($"Raising InputAction Event: {actionType} | InputAction: {context.action} | Phase: {context.phase} | Interaction: {context.interaction} | ScreenPos: {pointerScreenPosition} | WorldPos: {pointerWorldPosition}");
      eventChannel.Raise(actionType, BuildEventArgs(actionType, context));
    }

    private InputActionEventArgs BuildEventArgs(GameEventType actionType, InputAction.CallbackContext context) {
      return new InputActionEventArgs(actionType, context.phase, context, pointerScreenPosition, pointerWorldPosition);
    }

  }
}