using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Ameba;
using static GMTK.GameEventType;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour to handle player input for PlayableElement objects, enabling selection, dragging, rotation, and flipping.
  /// Implements events through several partial classes:
  /// 
  /// <para><b>Events:</b></para>
  /// <list type="bullet">
  ///   <item><b>InputPointerPosition</b>:when the pointer moves. Relevant for Hover and Dragging</item>
  ///   <item><b>InputSelected</b>: The pointer's primary action acts on an element (left-click or touch). Relevant for Select and UI controls</item>
  ///   <item><b>InputSecondary</b>:The pointer's secondary action acts on an element (right-click or two-point-touch). Relevant for Unselect</item>
  ///   <item><b>InputRotateCW</b>: The rotate clockwise button is pressed</item>
  ///   <item><b>InputRotateCCW</b>:The rotate counter clockwise button is pressed</item>
  ///   <item><b>InputFlippedX</b>: The flip in X axis button is pressed</item>
  ///   <item><b>InputFlippedY</b>: The flip in Y axis button is pressed.</item>
  /// </list>
  /// 
  /// <para>This class has room to support double-click or double-taps in the future. Currently not used in the game</para>
  /// <para>This class is split in 4 partial class files: PlayableElementInputHandler, PlayableElementInputHandler.IDragger, PlayableElementInputHandler.IHover, PlayableElementInputHandler.ISelector. The last 3 contain the implementation of interfaces IDragger, IHover and ISelector respectively</para>
  /// </summary>
  public partial class PlayableElementInputHandler : MonoBehaviour, ISelector<PlayableElement>, IDragger<PlayableElement>, IHover<PlayableElement> {

    [Header("Input Handler Settings")]
    [SerializeField] private bool _enableInput = true;
    [SerializeField] private bool _debugLogging = false;
    [SerializeField] private PlayableElement _currentElement;
    [SerializeField] private PlayableElement _lastElementOver;

    // New properties for PlayableElement
    public PlayableElement CurrentElement => _currentElement;
    public PlayableElement LastElementOver => _lastElementOver;

    public bool IsMoving { get; private set; } = false;
    public bool IsOverElement { get; private set; } = false;

    [SerializeField] private Vector2 _pointerScreenPos;
    [SerializeField] private Vector3 _pointerWorldPos;

    protected GameEventChannel _eventsChannel;

    #region Button Properties

    // Input phase tracking for proper event handling
    private bool _primaryPressed = false;
    private bool _primaryReleased = false;
    private bool _secondaryPressed = false;
    private bool _secondaryReleased = false;

    public Vector2 PointerScreenPosition => _pointerScreenPos;

    public Vector3 PointerWorldPosition => _pointerWorldPos;

    public bool PrimaryButtonDown {
      get {
        bool result = _primaryPressed;
        _primaryPressed = false; // Reset after reading
        return result;
      }
    }

    public bool PrimaryButtonUp {
      get {
        bool result = _primaryReleased;
        _primaryReleased = false; // Reset after reading
        return result;
      }
    }

    public bool PrimaryButtonHeld {
      get {
        return IsMoving; // Use our existing tracking
      }
    }

    public bool SecondaryButtonDown {
      get {
        bool result = _secondaryPressed;
        _secondaryPressed = false; // Reset after reading
        return result;
      }
    }

    public bool SecondaryButtonUp {
      get {
        bool result = _secondaryReleased;
        _secondaryReleased = false; // Reset after reading
        return result;
      }
    }

    public bool SecondaryButtonHeld => IsMoving; // Use our existing tracking

    public bool PrimaryDoubleClick => false; // Not implemented in the current system

    public bool SecondaryDoubleClick => false; // Not implemented in the current system

    #endregion

    #region MonoBehaviour methods

    private void Awake() {
      if (_eventsChannel == null) {
        _eventsChannel = ServiceLocator.Get<GameEventChannel>();
      }
    }

    private void Start() {
      if (_eventsChannel != null) {
        _eventsChannel.AddListener<InputActionEventArgs>(InputPointerPosition, UpdatePointerPosition);
        _eventsChannel.AddListener<InputActionEventArgs>(InputSelected, HandleSelect);
        _eventsChannel.AddListener<InputActionEventArgs>(InputSecondary, HandleSecondary);
        _eventsChannel.AddListener<InputActionEventArgs>(InputRotateCW, RotateCW);
        _eventsChannel.AddListener<InputActionEventArgs>(InputRotateCCW, RotateCCW);
        _eventsChannel.AddListener<InputActionEventArgs>(InputFlippedX, FlipX);
        _eventsChannel.AddListener<InputActionEventArgs>(InputFlippedY, FlipY);
      }
    }

    private void OnDestroy() {
      if (_eventsChannel != null) {
        _eventsChannel.RemoveListener<InputActionEventArgs>(InputSelected, HandleSelect);
        _eventsChannel.RemoveListener<InputActionEventArgs>(InputSecondary, HandleSecondary);
        _eventsChannel.RemoveListener<InputActionEventArgs>(InputRotateCW, RotateCW);
        _eventsChannel.RemoveListener<InputActionEventArgs>(InputRotateCCW, RotateCCW);
        _eventsChannel.RemoveListener<InputActionEventArgs>(InputFlippedX, FlipX);
        _eventsChannel.RemoveListener<InputActionEventArgs>(InputFlippedY, FlipY);
      }
    }

    #endregion

    #region Core Update Logic

    public void Update() {
      if (!_enableInput) return;
      UpdateInput();
    }

    public virtual void UpdateInput() {
      // Update hover detection
      UpdateHover();

      // Handle element position updates during dragging
      if (IsDragging && DraggedElement != null) {
        UpdateDrag(_pointerWorldPos);
      }

      // Handle primary button press
      if (PrimaryButtonDown) {
        HandlePrimaryPress();
      }

      // Handle primary button release
      if (PrimaryButtonUp) {
        HandlePrimaryRelease();
      }

      // Handle secondary button press
      if (SecondaryButtonDown) {
        HandleSecondaryPress();
      }

      // Handle secondary button release
      if (SecondaryButtonUp) {
        HandleSecondaryRelease();
      }

      // Handle double clicks (if implemented in the future)
      if (PrimaryDoubleClick) {
        HandlePrimaryDoubleClick();
      }

      if (SecondaryDoubleClick) {
        HandleSecondaryDoubleClick();
      }
    }

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

    #region Button Press/Release

    protected virtual void HandlePrimaryPress() {

      if (TryGetObjectAtPointer<PlayableElement>(out var element)) {
        //if is draggable, try starting drag
        if (element.IsDraggable && TryStartDrag(element)) {
          SetActiveElement(element);
        }
        else {
          // Clicked on empty space - clear active object
          SetActiveElement(null);
        }
      }
    }

    protected virtual void HandlePrimaryRelease() {
      if (IsDragging && DraggedElement != null) {
        if (TryStopDrag()) {
          //SetActiveElement(DraggedElement);
        }
      }
    }

    protected virtual void HandleSecondaryPress() { }

    protected virtual void HandleSecondaryRelease() { }

    protected virtual void HandlePrimaryDoubleClick() { }

    protected virtual void HandleSecondaryDoubleClick() { }

    /// <summary>
    /// Sets the specified <see cref="PlayableElement"/> as the active element, deactivating the previously active
    /// element if necessary.
    /// </summary>
    /// <remarks>This method ensures that only one <see cref="PlayableElement"/> is active at a time. If the
    /// specified element is already active, no changes are made. When an element is activated, its <see
    /// cref="PlayableElement.OnSelected"/> method is invoked. Similarly, when an element is deactivated, its <see
    /// cref="PlayableElement.OnElementDeselected"/> method is invoked.</remarks>
    /// <param name="element">The <see cref="PlayableElement"/> to activate. If <c>null</c>, the currently active element will be deactivated.</param>
    protected virtual void SetActiveElement(PlayableElement element) {
      if (element != null) {
        // Deactivate previous
        if (_currentElement != null && _currentElement != element) {
          _currentElement.IsActive = false;
          _currentElement.OnDeselect();
        }

        // Activate new
        _currentElement = element;
        if (_currentElement != null) {
          _currentElement.IsActive = true;
          _currentElement.OnSelect();
        }
      }
      else {
        // Deactivate current
        if (_currentElement != null) {
          _currentElement.IsActive = false;
          _currentElement.OnDeselect();
          _currentElement = null;
        }
      }
      DebugLog($"Active object changed to " + (element != null ? element.name : "null"));
    }

    /// <summary>
    /// Obtains the object of type T located at the current pointer position.<br/>
    /// This methods uses a raycast to detect objects under the pointer and attempts to retrieve the component of type T from the hit collider or its parent.<br/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected virtual T GetObjectAtPointer<T>() where T : IDraggable, IHoverable, ISelectable
          => TryGetObjectAtPointer<T>(out T obj) ? obj : default(T);

    /// <summary>
    /// Tries to obtain the object of type T located at the current pointer position.<br/>
    /// This methods uses a raycast to detect objects under the pointer and attempts to retrieve the component of type T from the hit collider or its parent.<br/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected virtual bool TryGetObjectAtPointer<T>(out T obj) where T : IDraggable, IHoverable, ISelectable {

      Vector2 worldPos = _pointerWorldPos;
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

      if (hit.collider != null) {
        //attempt to get obj from collider
        if (hit.collider.TryGetComponent<T>(out obj)) {
          return true;
        }
        //attempt to get it from parent
        else if (hit.collider.transform.parent.TryGetComponent<T>(out obj)) {
          return true;
        }
      }
      obj = default;
      return false;
    }

    protected Vector2 ToScreenPosition(Vector3 worldPos) {
      Camera camera = Camera.main;
      if (camera == null) {
        this.LogWarning("[PlayableElementInputHandler] No main camera found for world to screen conversion");
        return Vector2.zero;
      }
      Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
      return new(screenPos.x, screenPos.y);
    }

    #endregion

    #region Input Event Listeners

    public void UpdatePointerPosition(InputActionEventArgs inputArgs) {
      _pointerScreenPos = inputArgs.ScreenPos;
      _pointerWorldPos = inputArgs.WorldPos;
    }

    public void HandleSelect(InputActionEventArgs inputArgs) {
      var phase = inputArgs.Phase;
      switch (phase) {
        case InputActionPhase.Performed:
          _primaryPressed = true;

          // Try to select element at current pointer position
          if (TryGetElementOnCurrentPointer(out PlayableElement element)) {
            TrySelect(element);
          }
          else {
            // If clicking on empty space, deselect current element
            TryDeselect();
          }

          //_eventsChannel.Raise(GameEventType.InputSelected);
          break;
        case InputActionPhase.Canceled:
          _primaryReleased = true;
          break;
      }
    }

    public void HandleSecondary(InputActionEventArgs inputArgs) {
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

    public void RotateCW(InputActionEventArgs inputArgs) {
      if (inputArgs.Phase == InputActionPhase.Performed) {
        //_eventsChannel.Raise(GameEventType.InputRotateCW);

        // Rotate the selected element if any, otherwise rotate current element
        if (SelectedElement != null) {
          SelectedElement.RotateClockwise();
        }
        else {
          TryExecuteOnCurrentElement(e => e.RotateClockwise());
        }
      }

    }

    public void RotateCCW(InputActionEventArgs inputArgs) {
      if (inputArgs.Phase == InputActionPhase.Performed) {
        //_eventsChannel.Raise(GameEventType.InputRotateCCW);

        // Rotate the selected element if any, otherwise rotate current element
        if (SelectedElement != null) {
          SelectedElement.RotateCounterClockwise();
        }
        else {
          TryExecuteOnCurrentElement(e => e.RotateCounterClockwise());
        }
      }
    }
    public void FlipX(InputActionEventArgs inputArgs) {
      if (inputArgs.Phase == InputActionPhase.Performed) {
        //_eventsChannel.Raise(GameEventType.InputFlippedX);

        // Flip the selected element if any, otherwise flip current element
        if (SelectedElement != null) {
          SelectedElement.FlipX();
        }
        else {
          TryExecuteOnCurrentElement(e => e.FlipX());
        }
      }
    }

    public void FlipY(InputActionEventArgs inputArgs) {
      if (inputArgs.Phase == InputActionPhase.Performed) {
        //_eventsChannel.Raise(GameEventType.InputFlippedY);

        // Flip the selected element if any, otherwise flip current element
        if (SelectedElement != null) {
          SelectedElement.FlipY();
        }
        else {
          TryExecuteOnCurrentElement(e => e.FlipY());
        }
      }
    }
    private void TryExecuteOnCurrentElement(Action<PlayableElement> action) {
      if (_currentElement != null) action.Invoke(_currentElement);
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

    #endregion


    protected void DebugLog(string message) { if (_debugLogging) this.Log($"{_currentElement.name} - {message}"); }


#if UNITY_EDITOR
    public void TriggerHoveredEvent() => TriggerEvent(InputPointerPosition, InputActionPhase.Performed, 
      ToScreenPosition(_currentElement.transform.position), _currentElement.transform.position);

    public void TriggerUnhoveredEvent() => TriggerEvent(InputPointerPosition, InputActionPhase.Performed);

    public void TriggerSelectedEvent() => TriggerEvent(InputSelected, InputActionPhase.Performed);

    public void TriggerDragEvent() => TriggerEvent(InputPointerPosition, InputActionPhase.Performed);

    public void TriggerDroppedEvent() => TriggerEvent(InputSelected, InputActionPhase.Performed);

    private void TriggerEvent(GameEventType gameType, InputActionPhase phase, Vector2 screenPos = default, Vector3 worldPos = default)
            => _eventsChannel.Raise(gameType,
                new InputActionEventArgs(gameType, phase, new InputAction.CallbackContext(), 
                  Equals(screenPos, default) ? _pointerScreenPos : screenPos,
                  Equals(worldPos, default) ? _pointerWorldPos : worldPos));

#endif
  }
}