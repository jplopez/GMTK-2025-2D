using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour to handle player input for PlayableElement objects, enabling selection, dragging, rotation, and flipping.
  /// Extends DraggingController to implement the dragging system using PlayerInputActionDispatcher.
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
  public class SnappableInputHandler : DraggingController {

    [Header("Snappable Input Handler")]
    [SerializeField] private PlayableElement _currentElement;
    [SerializeField] private PlayableElement _lastElementOver;

    // Legacy properties for compatibility
    public GridSnappable Current => ConvertToGridSnappable(_currentElement);
    public GridSnappable LastOnOver => ConvertToGridSnappable(_lastElementOver);

    // New properties for PlayableElement
    public PlayableElement CurrentElement => _currentElement;
    public PlayableElement LastElementOver => _lastElementOver;

    public bool IsMoving { get; private set; } = false;
    public bool IsOverElement { get; private set; } = false;

    [SerializeField] private Vector2 _pointerScreenPos;
    [SerializeField] private Vector3 _pointerWorldPos;

    protected bool DisableDragging = false;

    protected GameEventChannel _eventsChannel;
    protected PlayerInputActionDispatcher _inputDispatcher;

    // Input phase tracking for proper event handling
    private bool _primaryPressed = false;
    private bool _primaryReleased = false;
    private bool _secondaryPressed = false;
    private bool _secondaryReleased = false;

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

    #endregion

    #region DraggingController Implementation

    protected override Vector2 GetPointerScreenPosition() {
      UpdatePointerPosition();
      return _pointerScreenPos;
    }

    protected override Vector3 GetPointerWorldPosition() {
      UpdatePointerPosition();
      return _pointerWorldPos;
    }

    protected override bool GetPrimaryButtonDown() {
      bool result = _primaryPressed;
      _primaryPressed = false; // Reset after reading
      return result;
    }

    protected override bool GetPrimaryButtonUp() {
      bool result = _primaryReleased;
      _primaryReleased = false; // Reset after reading
      return result;
    }

    protected override bool GetPrimaryButtonHeld() {
      return IsMoving; // Use our existing tracking
    }

    protected override bool GetSecondaryButtonDown() {
      bool result = _secondaryPressed;
      _secondaryPressed = false; // Reset after reading
      return result;
    }

    protected override bool GetSecondaryButtonUp() {
      bool result = _secondaryReleased;
      _secondaryReleased = false; // Reset after reading
      return result;
    }

    protected override bool GetPrimaryDoubleClick() {
      // Not implemented in the current system
      return false;
    }

    protected override bool GetSecondaryDoubleClick() {
      // Not implemented in the current system
      return false;
    }

    #endregion

    #region Overridden DraggingController Methods

    protected override void Update() {
      UpdatePointerPosition();

      // Handle element position updates during dragging
      if (IsMoving && _currentElement != null) {
        _currentElement.UpdatePosition(_pointerWorldPos);
      }

      // Handle hover detection
      UpdateHoverDetection();

      // Call base update for dragging controller logic
      base.Update();
    }

    protected override IDraggable GetObjectAtPointer() {
      if (TryGetElementOnCurrentPointer(out PlayableElement element)) {
        return element;
      }
      return null;
    }

    protected override void OnObjectHoverStart(IDraggable obj) {
      if (obj is PlayableElement element) {
        _lastElementOver = element;
        IsOverElement = true;
        element.OnPointerOver();
        _eventsChannel.Raise(GameEventType.ElementHovered,
            new GridSnappableEventArgs(ConvertToGridSnappable(element), _pointerScreenPos, _pointerWorldPos));
      }
    }

    protected override void OnObjectHoverEnd(IDraggable obj) {
      if (obj is PlayableElement element) {
        element.OnPointerOut();
        _eventsChannel.Raise(GameEventType.ElementUnhovered,
            new GridSnappableEventArgs(ConvertToGridSnappable(element), _pointerScreenPos, _pointerWorldPos));

        if (_lastElementOver == element) {
          _lastElementOver = null;
          IsOverElement = false;
        }
      }
    }

    protected override void OnObjectDragStart(IDraggable obj) {
      if (obj is PlayableElement element) {
        _currentElement = element;
        IsMoving = true;
        _eventsChannel.Raise(GameEventType.ElementSelected,
            new GridSnappableEventArgs(ConvertToGridSnappable(element), _pointerScreenPos, _pointerWorldPos));
      }
    }

    protected override void OnObjectDragEnd(IDraggable obj) {
      if (obj is PlayableElement element) {
        _eventsChannel.Raise(GameEventType.ElementDropped,
            new GridSnappableEventArgs(ConvertToGridSnappable(element), _pointerScreenPos, _pointerWorldPos));

        if (_currentElement == element) {
          _currentElement = null;
          IsMoving = false;
        }
      }
    }

    #endregion

    #region Utility Methods

    private void UpdatePointerPosition() {
      if (_inputDispatcher != null) {
        _pointerScreenPos = _inputDispatcher.PointerScreenPosition;
        _pointerWorldPos = _inputDispatcher.PointerWorldPoition;
      }
    }

    private void UpdateHoverDetection() {
      // This method maintains compatibility with the original hover detection system
      // The base DraggingController also handles hover, but we need this for legacy event compatibility
      // In the future, this could be simplified to rely entirely on the base class
    }

    private bool TryGetElementOnCurrentPointer(out PlayableElement element) {
      // Raycast to detect the element under the cursor
      Vector2 worldPos = Camera.main.ScreenToWorldPoint(_pointerScreenPos);
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

      // Check if we hit something
      if (hit && hit.collider != null && hit.collider.gameObject is var selected) {
        // Check if it's a PlayableElement and is draggable
        if (selected.TryGetComponent(out element)) {
          return true;
        }
      }
      element = null;
      return false;
    }

    private GridSnappable ConvertToGridSnappable(PlayableElement element) {
      // Compatibility layer: In a real implementation, you might want to handle this differently
      // For now, we'll try to get a GridSnappable component if it exists, or return null
      if (element == null) return null;
      return element.ToGridSnappable();
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

    #region Legacy Input Action Methods (for compatibility)

    public void HandleSelect(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        var phase = inputArgs.Phase;
        switch (phase) {
          case InputActionPhase.Performed:
            _primaryPressed = true;
            break;
          case InputActionPhase.Canceled:
            _primaryReleased = true;
            break;
        }
      }
    }

    public void HandleSecondary(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        var phase = inputArgs.Phase;
        switch (phase) {
          case InputActionPhase.Performed:
            _secondaryPressed = true;
            break;
          case InputActionPhase.Canceled:
            _secondaryReleased = true;
            break;
        }
      }
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

    private void TryExecuteOnCurrentElement(Action<PlayableElement> action) {
      if (_currentElement != null) action.Invoke(_currentElement);
    }

    #endregion

    #region Game State Management

    // The element dragging is disabled during Playing, to prevent players
    // from moving elements while the level is running
    public void UpdateFromGameState(GameStates state) {
      switch (state) {
        case GameStates.Preparation:
          DisableDragging = false;
          DraggingEnabled = true;
          break;
        case GameStates.Playing:
          DisableDragging = true;
          DraggingEnabled = false;
          break;
      }
    }

    #endregion

#if UNITY_EDITOR
    public void TriggerHoveredEvent() =>
        _eventsChannel.Raise(GameEventType.ElementHovered,
            new GridSnappableEventArgs(ConvertToGridSnappable(_currentElement), _pointerScreenPos, _pointerWorldPos));

    public void TriggerUnhoveredEvent() =>
        _eventsChannel.Raise(GameEventType.ElementUnhovered,
            new GridSnappableEventArgs(ConvertToGridSnappable(_currentElement), _pointerScreenPos, _pointerWorldPos));

    public void TriggerSelectedEvent() =>
        _eventsChannel.Raise(GameEventType.ElementSelected,
            new GridSnappableEventArgs(ConvertToGridSnappable(_currentElement), _pointerScreenPos, _pointerWorldPos));

    public void TriggerDroppedEvent() =>
        _eventsChannel.Raise(GameEventType.ElementDropped,
            new GridSnappableEventArgs(ConvertToGridSnappable(_currentElement), _pointerScreenPos, _pointerWorldPos));
#endif
  }
}