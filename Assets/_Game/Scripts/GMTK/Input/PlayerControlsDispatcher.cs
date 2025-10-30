using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// This class implements the <see cref="PlayerControls.IPlayerActions"/> interface to listen to InputAction events and resolves them as game events.
  /// Also, this class tracks the Pointer position in both Screen and World coordinates.
  /// 
  /// TODO: extend so MonoBehaviours can subscribe to inputs done over specific GameObjects (ie: UI Buttons, in-game objects, etc)
  /// </summary>
  public class PlayerControlsDispatcher : MonoBehaviour, PlayerControls.IGameplayActions {

    //Reference to the _eventChannel used to propagate InputAction events
    [Tooltip("Reference to the GameEventChannel used to propagate InputAction events")]
    [SerializeField] protected GameEventChannel _eventChannel;

    [Tooltip("Current Pointer position in Screen coordinates")]
    [SerializeField] protected Vector2 _pointerScreenPosition;

    [Tooltip("Current Pointer position in World coordinates")]
    [SerializeField] protected Vector3 _pointerWorldPoition;

    [Tooltip("Minimum movement threshold to consider the pointer position has changed")]
    [Range(0.01f, 1f)]
    public float PointerMoveThreshold = 0.1f;

    [Tooltip("Frequency (in seconds) to raise Pointer Position events")]
    [Range(0.01f, 0.5f)]
    public float PointerEventFrequency = 0.02f;

    private PlayerControls.GameplayActions gameplayActions;
    private float _timeSinceLastPointerEvent = 0f;

    private bool CanRaisePointerEvent => _timeSinceLastPointerEvent >= PointerEventFrequency;

    private void Awake() {
     
      //Getting the _eventChannel with 2 fallbacks
      if (_eventChannel == null) {
        _eventChannel = ServiceLocator.Get<GameEventChannel>();
      }
      if (_eventChannel == null) {
        _eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }
      if (_eventChannel == null) {
        this.LogError($"GameEventChannel instance could not be found. The PlayerControlsDispatcher will not work");
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
      this.Log($"PlayerControlsDispatcher initialized");
    }

    private void Update() {
      _timeSinceLastPointerEvent += Time.deltaTime;
    }

    public void OnDestroy() {
      try {
        gameplayActions.Disable();
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
          this.LogDebug($"Input Pointer Position event raised.");
        }
      }
    }

    /// <summary>
    /// This method updates both ScreenPos and WorldPos properties if the pointer has moved enough from the last recorded position.
    /// </summary>
    /// <param name="context"></param>
    private bool TryUpdatePointerPosition(Vector2 pointerPos) {
      if (Vector2.Distance(pointerPos, _pointerScreenPosition) >= PointerMoveThreshold) {
        _pointerScreenPosition = pointerPos;
        if (Camera.main == null) {
          _pointerWorldPoition = Vector3.zero;
        }
        else {
          //obtain world position from Camera, forzing z to zero to maintaing the 2d restrictions
          var camWorldPos = Camera.main.ScreenToWorldPoint(_pointerScreenPosition);
          _pointerWorldPoition = new(camWorldPos.x, camWorldPos.y, 0f);
        }
        this.LogDebug($"Pointer position updated. ScreenPos: {_pointerScreenPosition} | WorldPos: {_pointerWorldPoition}");
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
    /// Input handler for the Rotate counter clockwise. Raises the <see cref="GameEventType.InputRotateCCW"/> event.
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
      this.LogDebug($"Raising Input Action Event: {actionType} | Phase: {context.phase} | ScreenPos: {_pointerScreenPosition} | WorldPos: {_pointerWorldPoition}");
      _eventChannel.Raise(actionType, BuildEventArgs(actionType, context));
    }

    private InputActionEventArgs BuildEventArgs(GameEventType actionType, InputAction.CallbackContext context) {
      return new InputActionEventArgs(actionType, context.phase, context, _pointerScreenPosition, _pointerWorldPoition);
    }

  }
}