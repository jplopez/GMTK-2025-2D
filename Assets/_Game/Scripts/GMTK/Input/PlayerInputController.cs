using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static GMTK.PlayerControls;
using static UnityEngine.InputSystem.InputAction;

namespace GMTK {

  public enum PlayerControllerStates { Idle, MovingElement, OverElement, }

  public class PlayerInputController : MonoBehaviour, IGameplayActions {

    [SerializeField] private GridManager Grid;
    [SerializeField] private GridSnappable CurrentElement;

    public bool IsMoving { get => (_controllerState == PlayerControllerStates.MovingElement); }
    public bool IsOverElement { get => (_controllerState == PlayerControllerStates.OverElement); }


    public static event Action<GridSnappable> OnElementHovered;
    public static event Action OnElementUnhovered;

    protected GridSnappable _lastElementOver;
    private Vector2 _pointerScreenPos;
    private PlayerControllerStates _controllerState = PlayerControllerStates.Idle;
    protected PlayerControls _controls; //auto-generated class 

    //Command pattern for input actions
    private Dictionary<string, GridInputCommand> _inputCommands;

    #region Input Commands
    private void InitializeInputCommands() {
      _inputCommands = new Dictionary<string, GridInputCommand> {
        { "RotateCW", new GridInputCommand(name:"RotateCW", onStarted:
            () => TryExecuteOnTarget(e => e.RotateClockwise(), "Rotated CW"),
            onPerformed: null,
            validStates: new[] { PlayerControllerStates.MovingElement, PlayerControllerStates.OverElement }) },

        { "RotateCCW", new GridInputCommand(name:"RotateCCW", onStarted:
            () => TryExecuteOnTarget(e => e.RotateCounterClockwise(), "Rotated CCW"),
            onPerformed: null,
            validStates: new[] { PlayerControllerStates.MovingElement, PlayerControllerStates.OverElement }) },

        { "FlipX", new GridInputCommand(name:"FlipX",
            onStarted :() => TryExecuteOnTarget(e => e.FlipX(), "Flipped X"), onPerformed : null,
            validStates: new[] { PlayerControllerStates.MovingElement, PlayerControllerStates.OverElement }) },

        { "FlipY", new GridInputCommand(name : "FlipY",
            onStarted :() => TryExecuteOnTarget(e => e.FlipY(), "Flipped Y"), onPerformed : null,
            validStates: new[] { PlayerControllerStates.MovingElement, PlayerControllerStates.OverElement }) },

        { "Select", new GridInputCommand(name : "Select",
            onStarted: null,
            onPerformed: HandleSelectPressed,
            onCanceled: HandleSelectReleased,
            validStates: new[] { PlayerControllerStates.Idle, PlayerControllerStates.MovingElement, PlayerControllerStates.OverElement }) },

        { "Secondary", new GridInputCommand(name : "Secondary",
            onStarted: null,
            onPerformed: HandleSecondaryPressed,
            validStates: new[] { PlayerControllerStates.Idle, PlayerControllerStates.MovingElement, PlayerControllerStates.OverElement }) },
      };
    }

    private void TryExecuteOnTarget(Action<GridSnappable> action, string debugLabel) {
      GridSnappable target = null;

      switch (_controllerState) {
        case PlayerControllerStates.MovingElement:
          target = CurrentElement;
          break;
        case PlayerControllerStates.OverElement:
          target = _lastElementOver;
          break;
      }

      if (target == null) return;

      action.Invoke(target);
      Debug.Log($"[PlayerInputController] {debugLabel} on {(target == CurrentElement ? "current" : "over")} element.");
    }

    private void HandleSelectPressed() {
      switch (_controllerState) {
        case PlayerControllerStates.Idle:
          Debug.Log("[PlayerInputController] Clicked in Idle state");
          BeginMovingCurrentElement();
          break;
        case PlayerControllerStates.MovingElement:
          Debug.LogWarning("[PlayerInputController] Clicked while already moving an element");
          break;
        case PlayerControllerStates.OverElement:
          Debug.LogWarning("[PlayerInputController] Clicked while over an element");
          CurrentElement = _lastElementOver;
          BeginMovingCurrentElement();
          break;
      }
    }

    private void HandleSelectReleased() {
      switch (_controllerState) {
        case PlayerControllerStates.Idle:
          Debug.LogWarning("[PlayerInputController] Released click while in Idle state");
          break;
        case PlayerControllerStates.MovingElement:
          Debug.Log("[PlayerInputController] Released click while moving an element");
          EndMovingCurrentElement();
          break;
        case PlayerControllerStates.OverElement:
          Debug.LogWarning("[PlayerInputController] Released click while over an element");
          // TODO: logic to place element on top of another element?
          break;
      }
    }

    private void HandleSecondaryPressed() {
      switch (_controllerState) {
        case PlayerControllerStates.Idle:
          Debug.Log("[PlayerInputController] Right-clicked in Idle state");
          if (TryGetElementOnCurrentPointer(out GridSnappable element)) {
            Debug.Log("[PlayerInputController] Found element under cursor, removing it.");
            RemoveElement(element);
          }
          else {
            Debug.Log("[PlayerInputController] No element under cursor to remove.");
          }
          break;
        case PlayerControllerStates.MovingElement:
          Debug.Log("[PlayerInputController] Right-clicked while moving an element");
          RemoveCurrentElement();
          break;
        case PlayerControllerStates.OverElement:
          Debug.Log("[PlayerInputController] Right-clicked while over an element");
          RemoveOverElement();
          break;
      }
    }

    #endregion

    #region MonoBehaviour methods
    public void OnEnable() {
      _controls ??= new PlayerControls();
      _controls.Gameplay.SetCallbacks(this);
      _controls.Gameplay.Enable();
      InitializeInputCommands();
    }

    public void OnDisable() => _controls.Gameplay.Disable();

    void Update() {
      //updates CurrentElement position if controller is moving.
      Vector3 worldPos = Camera.main.ScreenToWorldPoint(_pointerScreenPos);
      if (IsMoving && CurrentElement != null) {
        worldPos.z = 0f;
        CurrentElement.UpdatePosition(worldPos);
        //Debug.Log($"Element placed at {worldPos}");
        // Optional: Show ghost highlight
      }
      else if (!IsMoving) {
        // logic to detect if the pointer is over an element
        if (TryGetElementOnCurrentPointer(out GridSnappable element)) {
          if (_controllerState != PlayerControllerStates.OverElement) {
            _controllerState = PlayerControllerStates.OverElement;
            _lastElementOver = element;
            _lastElementOver.OnPointerOver();
            OnElementHovered?.Invoke(_lastElementOver);
            //Debug.Log($"[PlayerInputController] Pointer is over an element: {element.name}");
          }
          else {
            // still over the same element, do nothing
            if (_lastElementOver != element) {
              // moved over a different element
              _lastElementOver.OnPointerOut();
              OnElementUnhovered?.Invoke();
              _lastElementOver = element;
              _lastElementOver.OnPointerOver();
              OnElementHovered?.Invoke(_lastElementOver);
              //Debug.Log($"[PlayerInputController] Pointer moved over a different element: {element.name}");
            }
          }
        }
        else {
          if (_controllerState != PlayerControllerStates.Idle) {
            _controllerState = PlayerControllerStates.Idle;
            _lastElementOver.OnPointerOut();
            //Debug.Log($"[PlayerInputController] Pointer is not over any element, back to Idle state.");
          }
        }
      }
    }

    #endregion

    #region IGameplayActions implementation
    void IGameplayActions.OnPointerPosition(CallbackContext context) => _pointerScreenPos = context.ReadValue<Vector2>();


    // escape or East button (PS: Circle, Xbox: B)
    public void OnCancel(CallbackContext context) {
      Debug.Log("Not implemented: OnCancel");
    }
    void IGameplayActions.OnRotateCW(CallbackContext context) => _inputCommands["RotateCW"].HandleInput(context, _controllerState);
    void IGameplayActions.OnRotateCCW(CallbackContext context) => _inputCommands["RotateCCW"].HandleInput(context, _controllerState);
    void IGameplayActions.OnFlipX(CallbackContext context) => _inputCommands["FlipX"].HandleInput(context, _controllerState);
    void IGameplayActions.OnFlipY(CallbackContext context) => _inputCommands["FlipY"].HandleInput(context, _controllerState);

    void IGameplayActions.OnSelect(CallbackContext context) => _inputCommands["Select"].HandleInput(context, _controllerState);
    void IGameplayActions.OnSecondary(CallbackContext context) => _inputCommands["Secondary"].HandleInput(context, _controllerState);

    #endregion


    #region GridSnappable management
    private bool TryGetElementOnCurrentPointer(out GridSnappable element) {
      // Raycast to detect the element under the cursor
      Vector2 worldPos = Camera.main.ScreenToWorldPoint(_pointerScreenPos);
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
      // only update CurrentElement if is a GridSnappable
      if (hit && hit.collider != null && hit.collider.gameObject is var selected) {
        if (selected.TryGetComponent(out GridSnappable gridElement)) {
          element = gridElement;
          //check if it's registered in the grid
          if (element.IsRegistered) Grid.RemoveElement(element); // temporarily remove it from the grid
        }
        else {
          Debug.Log($"[PlayerInputController] GameObject found at {_pointerScreenPos} (worldPos:{worldPos}) does not have a GridInteractive compatible component");
          element = null;
        }
      }
      else {
        element = null;
      }
      return (element != null);
    }
    private void BeginMovingCurrentElement() {
      _controllerState = PlayerControllerStates.MovingElement;

      if (TryGetElementOnCurrentPointer(out GridSnappable element)) {
        Debug.Log($"[PlayerInputController] Found element under cursor, picking it up.");
        CurrentElement = element;
        Grid.RemoveElement(CurrentElement);
      }
      //else {
      //  Debug.Log($"[PlayerInputController] No element under cursor, creating a new one.");
      //}
    }
    private void EndMovingCurrentElement() {
      // Only snap if we have a current element being moved
      if (CurrentElement != null) {
        if (!Grid.RegisterElement(CurrentElement)) {
          Debug.LogWarning($"[PlayerInputController] Could not place element, cell occupied. Returning to original position.");
          // Optionally, you could implement logic to return the element to its original position
        }
        CurrentElement = null;
      }
      else {
        Debug.LogWarning($"[PlayerInputController] No current element to place.");
      }
      _controllerState = PlayerControllerStates.Idle;
    }

    private void RemoveElement(GridSnappable element) {
      if (element != null) {
        Grid.RemoveElement(element);
        element = null;
        _controllerState = PlayerControllerStates.Idle;
      }
    }

    protected virtual void RemoveCurrentElement() => RemoveElement(CurrentElement);

    protected virtual void RemoveOverElement() => RemoveElement(_lastElementOver);
    #endregion

  }


  // Simple command pattern for input actions
  public class GridInputCommand {
    public string Name { get; }
    private readonly Action _onStarted;
    private readonly Action _onPerformed;
    private readonly Action _onCanceled;
    private readonly HashSet<PlayerControllerStates> _validStates;

    public GridInputCommand(string name, Action onStarted, Action onPerformed, Action onCanceled = null, IEnumerable<PlayerControllerStates> validStates = null) {
      Name = name;
      _onStarted = onStarted;
      _onPerformed = onPerformed;
      _onCanceled = onCanceled;
      _validStates = validStates != null
            ? new HashSet<PlayerControllerStates>(validStates)
            : null; // null means "always allowed"
    }

    public bool CanExecute(PlayerControllerStates currentState) => _validStates == null || _validStates.Contains(currentState);

    public void HandleInput(InputAction.CallbackContext context, PlayerControllerStates currentState) {
      if (!CanExecute(currentState)) return;

      if (context.started) {
        Debug.Log($"[GridInputCommand] {Name} started");
        _onStarted?.Invoke();
      }
      else if (context.performed) {
        Debug.Log($"[GridInputCommand] {Name} performed");
        _onPerformed?.Invoke();
      }
      else if (context.canceled) {
        Debug.Log($"[GridInputCommand] {Name} canceled");
        _onCanceled?.Invoke();
      }
    }


  }
}