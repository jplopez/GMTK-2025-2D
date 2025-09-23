using Ameba;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour to handle player input for PlayableElement objects, enabling selection, dragging, rotation, and flipping.
  /// Implements dragging logic directly without depending on DraggingController.
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
  public partial class PlayableElementInputHandler : MonoBehaviour, ISelector<PlayableElement>, IDragger<PlayableElement>, IHover<PlayableElement> {

    [Header("Input Handler Settings")]
    [SerializeField] private bool _enableInput = true;
    [SerializeField] private bool _debugLogging = false;
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

    #region Core Update Logic (replacing DraggingController)

    public void Update() {
      if (!_enableInput) return;

      UpdatePointerPosition();
      UpdateInput();
    }

    protected virtual void UpdateInput() {
      // Update hover detection
      UpdateHover();

      // Handle element position updates during dragging
      if (IsDragging && DraggedElement != null) {
        UpdateDrag(_pointerWorldPos);
      }

      // Handle primary button press
      if (GetPrimaryButtonDown()) {
        HandlePrimaryPress();
      }

      // Handle primary button release
      if (GetPrimaryButtonUp()) {
        HandlePrimaryRelease();
      }

      // Handle secondary button press
      if (GetSecondaryButtonDown()) {
        HandleSecondaryPress();
      }

      // Handle secondary button release
      if (GetSecondaryButtonUp()) {
        HandleSecondaryRelease();
      }

      // Handle double clicks (if implemented in the future)
      if (GetPrimaryDoubleClick()) {
        HandlePrimaryDoubleClick();
      }

      if (GetSecondaryDoubleClick()) {
        HandleSecondaryDoubleClick();
      }
    }

    #endregion

    #region Input Handlers (from DraggingController)

    protected virtual void HandlePrimaryPress() {
      var targetObject = GetObjectAtPointer();

      if (targetObject != null && targetObject.IsDraggable) {
        // Start dragging
        StartDragging(targetObject);
        // Set as active
        SetActiveObject(targetObject);
      }
      else {
        // Clicked on empty space - clear active object
        SetActiveObject(null);
      }

      OnPrimaryPress(_pointerWorldPos, targetObject);
    }

    protected virtual void HandlePrimaryRelease() {
      if (IsDragging && DraggedElement != null) {
        StopDragging(DraggedElement);
      }

      OnPrimaryRelease(_pointerWorldPos, DraggedElement);
    }

    protected virtual void HandleSecondaryPress() {
      var targetObject = GetObjectAtPointer();
      OnSecondaryPress(_pointerWorldPos, targetObject);
    }

    protected virtual void HandleSecondaryRelease() {
      var targetObject = GetObjectAtPointer();
      OnSecondaryRelease(_pointerWorldPos, targetObject);
    }

    protected virtual void HandlePrimaryDoubleClick() {
      var targetObject = GetObjectAtPointer();
      OnPrimaryDoubleClick(_pointerWorldPos, targetObject);
    }

    protected virtual void HandleSecondaryDoubleClick() {
      var targetObject = GetObjectAtPointer();
      OnSecondaryDoubleClick(_pointerWorldPos, targetObject);
    }

    #endregion

    #region Drag Management (from DraggingController)

    protected virtual void StartDragging(IDraggable obj) {
      if (obj == null || !obj.IsDraggable) return;

      if (obj is PlayableElement element) {
        TryStartDrag(element);
      }

      OnObjectDragStart(obj);
      DebugLog($"Started dragging {obj}");
    }

    protected virtual void StopDragging(IDraggable obj) {
      if (obj == null) return;

      if (obj is PlayableElement element && DraggedElement == element) {
        TryStopDrag();
      }

      OnObjectDragEnd(obj);
      DebugLog($"Stopped dragging {obj}");
    }

    protected virtual void SetActiveObject(IDraggable obj) {
      if (obj is PlayableElement element) {
        // Deactivate previous
        if (_currentElement != null && _currentElement != element) {
          _currentElement.IsActive = false;
          _currentElement.OnBecomeInactive();
          OnObjectBecomeInactive(_currentElement);
        }

        // Activate new
        _currentElement = element;
        if (_currentElement != null) {
          _currentElement.IsActive = true;
          _currentElement.OnBecomeActive();
          OnObjectBecomeActive(_currentElement);
        }
      }
      else if (obj == null) {
        // Deactivate current
        if (_currentElement != null) {
          _currentElement.IsActive = false;
          _currentElement.OnBecomeInactive();
          OnObjectBecomeInactive(_currentElement);
          _currentElement = null;
        }
      }

      DebugLog($"Active object changed to {obj}");
    }

    #endregion

    #region Object Detection (from DraggingController)

    /// <summary>
    /// Get the IDraggable object at the current pointer position
    /// </summary>
    protected virtual IDraggable GetObjectAtPointer() {
      Vector2 worldPos = _pointerWorldPos;
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

      if (hit.collider != null) {
        var draggable = hit.collider.GetComponent<IDraggable>();
        if (draggable != null) {
          return draggable;
        }

        // Also try getting from parent
        draggable = hit.collider.GetComponentInParent<IDraggable>();
        return draggable;
      }

      return null;
    }

    #endregion

    #region Pointer Positions

    public Vector2 GetPointerScreenPosition() {
      UpdatePointerPosition();
      return _pointerScreenPos;
    }

    public Vector3 GetPointerWorldPosition() {
      UpdatePointerPosition();
      return _pointerWorldPos;
    }

    #endregion

    #region Button State Methods (from DraggingController)

    protected bool GetPrimaryButtonDown() {
      bool result = _primaryPressed;
      _primaryPressed = false; // Reset after reading
      return result;
    }

    protected bool GetPrimaryButtonUp() {
      bool result = _primaryReleased;
      _primaryReleased = false; // Reset after reading
      return result;
    }

    protected bool GetPrimaryButtonHeld() {
      return IsMoving; // Use our existing tracking
    }

    protected bool GetSecondaryButtonDown() {
      bool result = _secondaryPressed;
      _secondaryPressed = false; // Reset after reading
      return result;
    }

    protected bool GetSecondaryButtonUp() {
      bool result = _secondaryReleased;
      _secondaryReleased = false; // Reset after reading
      return result;
    }

    protected bool GetPrimaryDoubleClick() {
      // Not implemented in the current system
      return false;
    }

    protected bool GetSecondaryDoubleClick() {
      // Not implemented in the current system
      return false;
    }

    #endregion

    #region Virtual Event Methods (from DraggingController)

    protected virtual void OnObjectHoverStart(IDraggable obj) {
      if (obj is PlayableElement element) {
        StartHover(element);
      }
    }

    protected virtual void OnObjectHoverEnd(IDraggable obj) {
      if (obj is PlayableElement element && HoveredElement == element) {
        StopHover();
      }
    }

    protected virtual void OnObjectDragStart(IDraggable obj) {
      // Already handled in StartDragging method
    }

    protected virtual void OnObjectDragUpdate(IDraggable obj, Vector3 worldPosition) {
      // Handled in UpdateDrag method
    }

    protected virtual void OnObjectDragEnd(IDraggable obj) {
      // Already handled in StopDragging method
    }

    protected virtual void OnObjectBecomeActive(IDraggable obj) {
      // Handled in SetActiveObject method
    }

    protected virtual void OnObjectBecomeInactive(IDraggable obj) {
      // Handled in SetActiveObject method
    }

    protected virtual void OnPrimaryPress(Vector3 worldPosition, IDraggable targetObject) {
      // Override for custom behavior
    }

    protected virtual void OnPrimaryRelease(Vector3 worldPosition, IDraggable targetObject) {
      // Override for custom behavior
    }

    protected virtual void OnSecondaryPress(Vector3 worldPosition, IDraggable targetObject) {
      // Override for custom behavior
    }

    protected virtual void OnSecondaryRelease(Vector3 worldPosition, IDraggable targetObject) {
      // Override for custom behavior
    }

    protected virtual void OnPrimaryDoubleClick(Vector3 worldPosition, IDraggable targetObject) {
      // Override for custom behavior
    }

    protected virtual void OnSecondaryDoubleClick(Vector3 worldPosition, IDraggable targetObject) {
      // Override for custom behavior
    }

    #endregion

    #region Utility Methods

    private void UpdatePointerPosition() {
      if (_inputDispatcher != null) {
        _pointerScreenPos = _inputDispatcher.PointerScreenPosition;
        _pointerWorldPos = _inputDispatcher.PointerWorldPoition;
      }
    }

    private bool TryGetElementOnCurrentPointer(out PlayableElement element) {
      // Raycast to detect the element under the cursor
      Vector2 worldPos = Camera.main.ScreenToWorldPoint(_pointerScreenPos);
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

      // Check if we hit something
      if (hit && hit.collider != null && hit.collider.gameObject is var selected) {
        // Check if it's a PlayableElement
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

    protected void DebugLog(string message) {
      if (_debugLogging) {
        Debug.Log($"[{GetType().Name}] {message}");
      }
    }

    #endregion

    #region Legacy Input Action Methods (for compatibility)

    public void HandleSelect(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        var phase = inputArgs.Phase;
        switch (phase) {
          case InputActionPhase.Performed:
            _primaryPressed = true;
            
            // Try to select element at current pointer position
            if (TryGetElementOnCurrentPointer(out PlayableElement element)) {
              TrySelect(element);
            } else {
              // If clicking on empty space, deselect current element
              TryDeselect();
            }
            
            _eventsChannel.Raise(GameEventType.InputSelected);
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
            
            // Secondary click for deselection
            TryDeselect();
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
          
          // Rotate the selected element if any, otherwise rotate current element
          if (SelectedElement != null) {
            SelectedElement.RotateClockwise();
          } else {
            TryExecuteOnCurrentElement(e => e.RotateClockwise());
          }
        }
      }
    }

    public void RotateCCW(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        if (inputArgs.Phase == InputActionPhase.Performed) {
          _eventsChannel.Raise(GameEventType.InputRotateCCW);
          
          // Rotate the selected element if any, otherwise rotate current element
          if (SelectedElement != null) {
            SelectedElement.RotateCounterClockwise();
          } else {
            TryExecuteOnCurrentElement(e => e.RotateCounterClockwise());
          }
        }
      }
    }

    public void FlipX(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        if (inputArgs.Phase == InputActionPhase.Performed) {
          //_eventsChannel.Raise(GameEventType.InputFlippedX);
          
          // Flip the selected element if any, otherwise flip current element
          if (SelectedElement != null) {
            SelectedElement.FlipX();
          } else {
            TryExecuteOnCurrentElement(e => e.FlipX());
          }
        }
      }
    }

    public void FlipY(EventArgs eventArgs) {
      if (TryHandleAsInputActionEventArgs(eventArgs, out InputActionEventArgs inputArgs)) {
        if (inputArgs.Phase == InputActionPhase.Performed) {
          //_eventsChannel.Raise(GameEventType.InputFlippedY);

          // Flip the selected element if any, otherwise flip current element
          if (SelectedElement != null) {
            SelectedElement.FlipY();
          } else {
            TryExecuteOnCurrentElement(e => e.FlipY());
          }
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
          CanSelect = true;
          CanDrag = true;
          CanHover = true;
          break;
        case GameStates.Playing:
          CanHover = false;
          CanSelect = false;
          CanDrag = false;
          // Deselect and stop dragging when entering play mode
          TryDeselect();
          TryStopDrag();
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