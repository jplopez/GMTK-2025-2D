using Ameba;
using Ameba.Input;
using System;
using Unity.Android.Gradle;
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
  public class SnappableInputHandler : GameplayInputBase {

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

    // Declare events using EventHadler<GridSnappableEventArgs>
    public static event EventHandler<GridSnappableEventArgs> OnElementHovered;
    public static event EventHandler<GridSnappableEventArgs> OnElementUnhovered;
    public static event EventHandler<GridSnappableEventArgs> OnElementSelected;
    public static event EventHandler<GridSnappableEventArgs> OnElementDropped;
    public static event EventHandler<GridSnappableEventArgs> OnElementSecondary;


    protected override void Awake() {
      base.Awake();
      if(_eventsChannel == null) {
        _eventsChannel = Game.Context.EventsChannel;
      }
    }


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

    #region InputHandler methods

    [InputHandler("PointerPosition")]
    public void HandlePointer(InputAction.CallbackContext context) {
      Vector2 pointerPos = context.ReadValue<Vector2>();
      if (pointerPos != _pointerScreenPos) {
        _pointerScreenPos = pointerPos;
        if (Camera.main == null) {
          _pointerWorldPos = Vector3.zero;
        }
        else {
          _pointerWorldPos = Camera.main.ScreenToWorldPoint(_pointerScreenPos);
        }
        _pointerWorldPos.z = 0f;
      }
    }

    [InputHandler("Select", InputActionPhase.Performed, InputActionPhase.Canceled)]
    public void HandleSelect(InputAction.CallbackContext context) {
      _eventsChannel.Raise(GameEventType.InputSelected);
      switch (context.phase) {
        case InputActionPhase.Performed: BeginMovingElement(); break;
        case InputActionPhase.Canceled: EndMovingElement(); break;
      }
    }

    [InputHandler("Secondary")]
    public void HandleSecondary(InputAction.CallbackContext context) {
      Debug.Log("SnappableInputHandler.HandleSecondary : Not implemented yet");
      _eventsChannel.Raise(GameEventType.InputSecondary);
    }

    [InputHandler("RotateCW", InputActionPhase.Started)]
    public void RotateCW(InputAction.CallbackContext context) {
      _eventsChannel.Raise(GameEventType.InputRotateCW);
      TryExecuteOnCurrentElement(e => e.RotateClockwise());
    }

    [InputHandler("RotateCCW", InputActionPhase.Started)]
    public void RotateCCW(InputAction.CallbackContext context) {
      _eventsChannel.Raise(GameEventType.InputRotateCCW);
      TryExecuteOnCurrentElement(e => e.RotateCounterClockwise());
    }

    [InputHandler("FlipX", InputActionPhase.Started)]
    public void FlipX(InputAction.CallbackContext context) {
      _eventsChannel.Raise(GameEventType.InputFlippedX);
      TryExecuteOnCurrentElement(e => e.FlipX());
    }

    [InputHandler("FlipY", InputActionPhase.Started)]
    public void FlipY(InputAction.CallbackContext context) {
      _eventsChannel.Raise(GameEventType.InputFlippedY);
      TryExecuteOnCurrentElement(e => e.FlipY());
    }

    #endregion


    #region MonoBehaviour methods

    protected virtual void Update() {

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
          RaiseEvent(OnElementHovered, _lastElementOver);
          _eventsChannel.Raise(GameEventType.ElementHovered, 
              new GridSnappableEventArgs(_lastElementOver, _pointerScreenPos));
          //Debug.Log($"OnElementHovered: {_lastElementOver.name}");
        }
      }
      //no element, means we have unhover the lastElementOver
      else { UnhoverLastElement(); }
    }

    private void UnhoverLastElement() {
      if (_lastElementOver != null && IsOverElement) {
        _lastElementOver.OnPointerOut();
        RaiseEvent(OnElementUnhovered, _lastElementOver);
        _eventsChannel.Raise(GameEventType.ElementUnhovered, 
            new GridSnappableEventArgs(_lastElementOver, _pointerScreenPos));
        //Debug.Log($"OnElementUnhovered: {_lastElementOver.name}");
        _lastElementOver = null;
        IsOverElement = false;
      }
    }

    private void RaiseEvent(EventHandler<GridSnappableEventArgs> evt, GridSnappable element) {
      //Debug.Log($"SnappableInputHandler:RaiseEvent {nameof(evt)}. Element: {element.name;}");
      var payload = new GridSnappableEventArgs(element, _pointerScreenPos);
      evt?.Invoke(this, payload);
      //Game.Context.EventsChannel.Raise(GameEventType.Input, payload);
    }

    private void RaiseEventOrException(EventHandler<GridSnappableEventArgs> evt, GridSnappable element) {
      if (evt == null) throw new ArgumentNullException(nameof(evt));
      if (element == null) throw new ArgumentNullException(nameof(element));
      try {
        RaiseEvent(evt, element);
      }
      catch (Exception ex) {
        Debug.LogError($"Failed to raise event '{evt?.GetType().Name}': {ex.Message}");
#if !UNITY_EDITOR
        throw;
#endif
      }

    }
    #endregion

    #region GridSnappable management

    private void BeginMovingElement() {
      if (TryGetElementOnCurrentPointer(out GridSnappable selectedElement)) {
        if (!DisableDragging && selectedElement.Draggable) {
          _currentElement = selectedElement;
          IsMoving = true;
          RaiseEvent(OnElementSelected, _currentElement);
          _eventsChannel.Raise(GameEventType.ElementSelected, 
              new GridSnappableEventArgs(_currentElement, _pointerScreenPos));
        }
      }
    }

    private void EndMovingElement() {
      if (_currentElement != null) {
        //RaiseEvent(OnElementDropped, _currentElement);
        _eventsChannel.Raise(GameEventType.ElementDropped, 
            new GridSnappableEventArgs(_currentElement, _pointerScreenPos));
        _currentElement = null;
        IsMoving = false;
      }
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

    #endregion


#if UNITY_EDITOR
    public void TriggerHoveredEvent() => RaiseEventOrException(OnElementHovered, _currentElement);

    public void TriggerUnhoveredEvent() => RaiseEventOrException(OnElementUnhovered, _currentElement);

    public void TriggerSelectedEvent() => RaiseEventOrException(OnElementSelected, _currentElement);

    public void TriggerDroppedEvent() => RaiseEventOrException(OnElementDropped, _currentElement);

    public void TriggerSecondaryEvent() => RaiseEventOrException(OnElementSecondary, _currentElement);
#endif
  }
}