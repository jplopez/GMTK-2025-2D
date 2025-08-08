using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Ameba.Input;

namespace GMTK {

  /// <summary>
  /// Handles player input for <see cref="GridSnappable"/> objects, enabling selection, dragging, rotation, and flipping.
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

    // Declare events using EventHadler<GridSnappableEventArgs>
    public static event EventHandler<GridSnappableEventArgs> OnElementHovered;
    public static event EventHandler<GridSnappableEventArgs> OnElementUnhovered;
    public static event EventHandler<GridSnappableEventArgs> OnElementSelected;
    public static event EventHandler<GridSnappableEventArgs> OnElementDropped;
    public static event EventHandler<GridSnappableEventArgs> OnElementSecondary;

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
      switch (context.phase) {
        case InputActionPhase.Performed: BeginMovingElement(); break;
        case InputActionPhase.Canceled: EndMovingElement(); break;
      }
    }

    [InputHandler("Secondary")]
    public void HandleSecondary(InputAction.CallbackContext context) {
      Debug.Log("SnappableInputHandler.HandleSecondary : Not implemented yet");
    }

    [InputHandler("RotateCW", InputActionPhase.Started)]
    public void RotateCW(InputAction.CallbackContext context) => TryExecuteOnCurrentElement(e => e.RotateClockwise());


    [InputHandler("RotateCCW", InputActionPhase.Started)]
    public void RotateCCW(InputAction.CallbackContext context) => TryExecuteOnCurrentElement(e => e.RotateCounterClockwise());


    [InputHandler("FlipX", InputActionPhase.Started)]
    public void FlipX(InputAction.CallbackContext context) => TryExecuteOnCurrentElement(e => e.FlipX());


    [InputHandler("FlipY", InputActionPhase.Started)]
    public void FlipY(InputAction.CallbackContext context) => TryExecuteOnCurrentElement(e => e.FlipY());

    #region MonoBehaviour methods

    protected virtual void Update() {

      if (IsMoving && _currentElement != null) {
        _currentElement.UpdatePosition(_pointerWorldPos);
      }

      if (TryGetElementOnCurrentPointer(out GridSnappable element)) {
        IsOverElement = true;
        if (!element.Equals(_lastElementOver)) {
          // moved over a different element
          if (_lastElementOver != null) {
            _lastElementOver.OnPointerOut();
            RaiseEvent(OnElementUnhovered, _lastElementOver);
          }
          _lastElementOver = element;
          _lastElementOver.OnPointerOver();
          RaiseEvent(OnElementHovered, _lastElementOver);
        }
      }
      else { IsOverElement = false; }
    }

    private void RaiseEvent(EventHandler<GridSnappableEventArgs> evt, GridSnappable element) {
      //Debug.Log($"SnappableInputHandler:RaiseEvent {nameof(evt)}. Element: {element.name}");
      evt?.Invoke(this, new GridSnappableEventArgs(element, _pointerScreenPos));
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
        if (selectedElement.Draggable) {
          _currentElement = selectedElement;
          IsMoving = true;
          RaiseEvent(OnElementSelected, _currentElement);
        }
      }
    }

    private void EndMovingElement() {
      if (_currentElement != null) {
        RaiseEvent(OnElementDropped, _currentElement);
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