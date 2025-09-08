using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour to handle player input for <see cref="GridSnappable"/> objects, enabling selection, dragging, rotation, and flipping.
  /// 
  /// <para><b>Events:</b></para>
  /// <list type="bullet">
  ///   <item><b>OnElementHovered</b>: Triggered when the pointer moves over an element.</item>
  ///   <item><b>OnElementUnhovered</b>: Triggered when the pointer exits an element.</item>
  ///   <item><b>OnElementSelected</b>: Triggered when the select button is pressed to begin dragging.</item>
  ///   <item><b>OnElementDropped</b>: Triggered when the select button is released to finalize dragging.</item>
  ///   <item><b>OnElementSecondary</b>: Reserved for secondary interactions (e.g., right-click). Not yet implemented.</item>
  /// </list>
  /// </summary>
  public class SnappableInputHandler : MonoBehaviour {

    public GridSnappable Current => _currentElement;
    public GridSnappable LastOnOver => _lastElementOver;

    [SerializeField] private GridSnappable _currentElement;
    [SerializeField] private GridSnappable _lastElementOver;

    public bool IsMoving { get; private set; } = false;
    public bool IsOverElement { get; private set; } = false;

    private Vector2 _pointerScreenPos;
    private Vector3 _pointerWorldPos;

    protected bool DisableDragging = false;

    protected GameEventChannel _eventsChannel;
    protected PlayerInputActionDispatcher _inputDispatcher;

    #region MonoBehaviour methods

    private void Awake() {
     
      if (_eventsChannel == null) {
        _eventsChannel = ServiceLocator.Get<GameEventChannel>();
      }

      if (_inputDispatcher == null && TryGetComponent(out _inputDispatcher)) {
        this.Log($"InputDispatcher obtained: {_inputDispatcher.name}");
      }
    }

    private void Start() {

      if (_inputDispatcher != null) {

        _inputDispatcher.InputEvents.AddListener<EventArgs>(InputActionType.Select, HandleSelect);
        _inputDispatcher.InputEvents.AddListener<EventArgs>(InputActionType.Secondary, HandleSecondary);
        _inputDispatcher.InputEvents.AddListener<EventArgs>(InputActionType.RotateCW, RotateCW);
        _inputDispatcher.InputEvents.AddListener<EventArgs>(InputActionType.RotateCCW, RotateCCW);
        _inputDispatcher.InputEvents.AddListener<EventArgs>(InputActionType.FlipX, FlipX);
        _inputDispatcher.InputEvents.AddListener<EventArgs>(InputActionType.FlipY, FlipY);
      }
      UpdatePointerPosition();
    }

    private void UpdatePointerPosition() {
      if (_inputDispatcher != null) {
        _pointerScreenPos = _inputDispatcher.PointerScreenPosition;
        _pointerWorldPos = _inputDispatcher.PointerWorldPoition;
      }
    }

    private void OnDestroy() {
      if (_inputDispatcher != null) {
        _inputDispatcher.InputEvents.RemoveListener<EventArgs>(InputActionType.Select, HandleSelect);
        _inputDispatcher.InputEvents.RemoveListener<EventArgs>(InputActionType.Secondary, HandleSecondary);
        _inputDispatcher.InputEvents.RemoveListener<EventArgs>(InputActionType.RotateCW, RotateCW);
        _inputDispatcher.InputEvents.RemoveListener<EventArgs>(InputActionType.RotateCCW, RotateCCW);
        _inputDispatcher.InputEvents.RemoveListener<EventArgs>(InputActionType.FlipX, FlipX);
        _inputDispatcher.InputEvents.RemoveListener<EventArgs>(InputActionType.FlipY, FlipY);
      }
    }

    protected virtual void Update() {

      UpdatePointerPosition();

      //TODO change this to snappable component that decides when to update position
      if (IsMoving && _currentElement != null) {
        _currentElement.UpdatePosition(_pointerWorldPos);
      }

      if (TryGetElementOnCurrentPointer(out GridSnappable element)) {
        IsOverElement = true;
        if (!element.Equals(_lastElementOver)) {

          // moved over a different element
          UnhoverLastElement();

          _lastElementOver = element;
          _lastElementOver.OnPointerOver();
          //RaiseEvent(OnElementHovered, _lastElementOver);
          _eventsChannel.Raise(GameEventType.ElementHovered,
              new GridSnappableEventArgs(_lastElementOver, _pointerScreenPos, _pointerWorldPos));
          //Debug.Log($"OnElementHovered: {_lastElementOver.name}");
        }
      }
      //no element, means we have unhover the lastElementOver
      else { UnhoverLastElement(); }
    }

    private void UnhoverLastElement() {
      if (_lastElementOver != null && IsOverElement) {
        _lastElementOver.OnPointerOut();
        //RaiseEvent(OnElementUnhovered, _lastElementOver);
        _eventsChannel.Raise(GameEventType.ElementUnhovered,
            new GridSnappableEventArgs(_lastElementOver, _pointerScreenPos, _pointerWorldPos));
        //Debug.Log($"OnElementUnhovered: {_lastElementOver.name}");
        _lastElementOver = null;
        IsOverElement = false;
      }
    }

    #endregion

    //The element dragging is disabled during Playing, to prevent players
    //from moving elements while the level is running
    //In the future, this might not be always the case, which is why
    //this specific encapsulation
    public void UpdateFromGameState(GameStates state) {
      switch (state) {
        case GameStates.Preparation:
          DisableDragging = false;
          break;
        case GameStates.Playing:
          DisableDragging = true;
          break;
      }
    }

    #region Gameplay InputAction methods

    public void HandleSelect(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        var phase = inputArgs.Phase;
        switch (phase) {
          case InputActionPhase.Performed: BeginMovingElement(); break;
          case InputActionPhase.Canceled: EndMovingElement(); break;
        }
      }
    }

    public void HandleSecondary(EventArgs eventArgs) {
      this.Log("HandleSecondary : Not implemented yet");
      //_eventsChannel.Raise(GameEventType.InputSecondary);
    }

    public void RotateCW(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        if (inputArgs.Phase == InputActionPhase.Performed) {
          _eventsChannel.Raise(GameEventType.InputRotateCW);
          TryExecuteOnCurrentElement(e => e.RotateClockwise());
        }
      }
    }

    public void RotateCCW(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        if (inputArgs.Phase == InputActionPhase.Performed) {
          _eventsChannel.Raise(GameEventType.InputRotateCCW);
          TryExecuteOnCurrentElement(e => e.RotateCounterClockwise());
        }
      }
    }

    public void FlipX(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        if (inputArgs.Phase == InputActionPhase.Performed) {
          _eventsChannel.Raise(GameEventType.InputFlippedX);
          TryExecuteOnCurrentElement(e => e.FlipX());
        }
      }
    }

    public void FlipY(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        if (inputArgs.Phase == InputActionPhase.Performed) {
          _eventsChannel.Raise(GameEventType.InputFlippedY);
          TryExecuteOnCurrentElement(e => e.FlipY());
        }
      }
    }

    private void RaiseGameEvent(GameEventType eventType, GridSnappable snappable) {
      _eventsChannel.Raise(eventType,
              new GridSnappableEventArgs(snappable, _pointerScreenPos, _pointerWorldPos));
    }

    private void TryExecuteOnCurrentElement(Action<GridSnappable> action) {
      //Debug.Log($"SnappableInputHandler:TryExecute: {nameof(action)} Current: {_currentElement.name}");
      if (_currentElement != null) action.Invoke(_currentElement);
    }

    private bool TryGetElementOnCurrentPointer(out GridSnappable element) {
      // Raycast to detect the element under the cursor
      Vector2 worldPos = Camera.main.ScreenToWorldPoint(_pointerScreenPos);
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
      //check if we hit something
      if (hit && hit.collider != null && hit.collider.gameObject is var selected) {
        // check if is a GridSnappable and is draggable
        if (selected.TryGetComponent(out element)) {
          return true;
        }
      }
      element = null;
      return false;
    }

    private bool TryHandleAsInputActionEventArgs(EventArgs args, out InputActionEventArgs inputArgs) {
      inputArgs = default;
      if (args is InputActionEventArgs validArgs) {
        inputArgs = validArgs;
        return true;
      }
      return false;
    }

    #endregion


    #region GridSnappable Movement Tracking

    private void BeginMovingElement() {
      if (TryGetElementOnCurrentPointer(out GridSnappable selectedElement)) {
        if (!DisableDragging && selectedElement.Draggable) {
          _currentElement = selectedElement;
          IsMoving = true;
          //RaiseEvent(OnElementSelected, _currentElement);
          _eventsChannel.Raise(GameEventType.ElementSelected,
              new GridSnappableEventArgs(_currentElement, _pointerScreenPos, _pointerWorldPos));
        }
      }
    }

    private void EndMovingElement() {
      if (_currentElement != null) {
        //RaiseEvent(OnElementDropped, _currentElement);
        _eventsChannel.Raise(GameEventType.ElementDropped,
            new GridSnappableEventArgs(_currentElement, _pointerScreenPos, _pointerWorldPos));
        _currentElement = null;
        IsMoving = false;
      }
    }

    #endregion

#if UNITY_EDITOR
    public void TriggerHoveredEvent() => RaiseGameEvent(GameEventType.ElementHovered, _currentElement);

    public void TriggerUnhoveredEvent() => RaiseGameEvent(GameEventType.ElementUnhovered, _currentElement);

    public void TriggerSelectedEvent() => RaiseGameEvent(GameEventType.ElementSelected, _currentElement);

    public void TriggerDroppedEvent() => RaiseGameEvent(GameEventType.ElementDropped, _currentElement);

#endif
  }
}