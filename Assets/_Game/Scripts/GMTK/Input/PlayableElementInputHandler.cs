using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Ameba;
using Meryel.Serilog;
using static GMTK.GameEventType;

namespace GMTK {

  [Flags]
  public enum SelectionTrigger {
    None = 0, OnClick = 1, OnKeyPress = 2
  }
  
  /// <summary>
  /// This class listens to game events related to <see cref="PlayableElement"/> for hovering, selecting, dragging, rotation, and flipping.
  /// It maintains the state of the currently active element (selected or hovered) and updates it based on the input events received.
  /// 
  /// <para><b>Events:</b></para>
  /// <list type="bullet">
  ///   <item><b>InputPointerPosition</b>:when the pointer moves. Relevant for Hover and Dragging</item>
  ///   <item><b>InputSelected</b>: The pointer's primary action (left-click or space bar). Depending on the interaction type (press or hold), the element is selected or dragged </item>
  ///   <item><b>InputRotateCW</b>: The rotate clockwise button is pressed</item>
  ///   <item><b>InputRotateCCW</b>:The rotate counterclockwise button is pressed</item>
  ///   <item><b>InputFlippedX</b>: The flip in X axis button is pressed</item>
  ///   <item><b>InputFlippedY</b>: The flip in Y axis button is pressed.</item>
  /// </list>
  /// 
  /// <para>This class has room to support double-click or double-taps in the future. Currently not used in the game</para>
  /// <para>This class is split in 4 partial class files: PlayableElementInputHandler, PlayableElementInputHandler.IDragger, PlayableElementInputHandler.IHover, PlayableElementInputHandler.ISelector. The last 3 contain the implementation of interfaces IDragger, IHover and ISelector respectively</para>
  /// </summary>
  public partial class PlayableElementInputHandler : MonoBehaviour{

    [SerializeField] private bool _enableInput = true;
    [SerializeField] private bool _debugLogging;
    [SerializeField] private PlayableElement _activeElement;
    [SerializeField] private PlayableElement _currentHoveredElement;
   
    /// <summary>
    /// Active PlayableElement, which is the element currently selected or being dragged.
    /// This is the main reference for the element that input events will act upon. It can be null if no element is currently active.
    /// </summary>
    public PlayableElement ActiveElement => _activeElement;
    public PlayableElement CurrentHoveredElement => _currentHoveredElement;

    public bool IsMovingElement { get; private set; }
    public bool IsOverElement { get; private set; }
    public bool IsSelectPressed { get; private set; }
    public bool IsSelectHold { get; private set; }

    public bool IsMovePressed { get; private set; }
    private Vector2 _currentMoveDirection;

    [SerializeField] private Vector2 _pointerScreenPos;
    [SerializeField] private Vector3 _pointerWorldPos;

    private GameEventChannel _eventsChannel;

    #region Pointer/Button Properties

    public Vector2 PointerScreenPosition => _pointerScreenPos;

    public Vector3 PointerWorldPosition => _pointerWorldPos;

    public float MoveSpeed = 10f;

    #endregion

    #region MonoBehaviour methods

    private void Awake() {
      if (_eventsChannel == null) {
        _eventsChannel = ServiceLocator.Get<GameEventChannel>();
      }
    }

    private void Start()
    {
      if (_eventsChannel == null) return;
      _eventsChannel.AddListener<InputActionEventArgs>(InputPointerPosition, UpdatePointerPosition);
      _eventsChannel.AddListener<InputActionEventArgs>(InputMove, MoveActiveElement);
      _eventsChannel.AddListener<InputActionEventArgs>(InputSelected, HandleSelect);
      _eventsChannel.AddListener<InputActionEventArgs>(InputRotateCW, RotateCW);
      _eventsChannel.AddListener<InputActionEventArgs>(InputRotateCCW, RotateCCW);
      _eventsChannel.AddListener<InputActionEventArgs>(InputFlippedX, FlipX);
      _eventsChannel.AddListener<InputActionEventArgs>(InputFlippedY, FlipY);
    }

    private void OnDestroy()
    {
      if (_eventsChannel == null) return;
      _eventsChannel.RemoveListener<InputActionEventArgs>(InputPointerPosition, UpdatePointerPosition);
      _eventsChannel.RemoveListener<InputActionEventArgs>(InputMove, MoveActiveElement);
      _eventsChannel.RemoveListener<InputActionEventArgs>(InputSelected, HandleSelect);
      _eventsChannel.RemoveListener<InputActionEventArgs>(InputRotateCW, RotateCW);
      _eventsChannel.RemoveListener<InputActionEventArgs>(InputRotateCCW, RotateCCW);
      _eventsChannel.RemoveListener<InputActionEventArgs>(InputFlippedX, FlipX);
      _eventsChannel.RemoveListener<InputActionEventArgs>(InputFlippedY, FlipY);
    }

    #endregion

    #region Core Update Logic

    public void Update() {
      if(!_enableInput) return;
      
      // Update hover detection
      UpdateHover();

      // Handle element position updates during dragging
      UpdateDrag(_pointerWorldPos);
      
      // To handle 'hold' of arrow keys or dpad for moving elements,
      // we need to continuously update the position while the input is held.
      if(IsMovePressed) UpdateActiveElementPosition(_currentMoveDirection);
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

    #region Active Element Management
    
    /// <summary>
    /// Sets the specified <see cref="PlayableElement"/> as the active element, deactivating the previously active
    /// element if necessary.
    /// </summary>
    /// <remarks>This method ensures that only one <see cref="PlayableElement"/> is active at a time. If the
    /// specified element is already active, no changes are made. When an element is activated, its <see
    /// cref="PlayableElement.OnSelect"/> method is invoked. Similarly, when an element is deactivated, its <see
    /// cref="PlayableElement.OnDeselect"/> method is invoked.</remarks>
    /// <param name="element">The <see cref="PlayableElement"/> to activate. If <c>null</c>, the currently active element will be deactivated.</param>
    protected virtual void SetActiveElement(PlayableElement element) {
      if (element != null && element != _activeElement) {
        // Deactivate previous
        if (_activeElement != null && _activeElement != element) {
          _activeElement.IsActive = false;
          _activeElement.OnDeselect();
        }

        // Activate new
        _activeElement = element;
        if (_activeElement != null) {
          _activeElement.IsActive = true;
          _activeElement.OnSelect();
        }
      }
      else {
        // Deactivate current
        if (_activeElement != null) {
          _activeElement.IsActive = false;
          _activeElement.OnDeselect();
          _activeElement = null;
        }
      }
      DebugLog($"Active object changed to " + (element != null ? element.name : "null"));
    }

    /// <summary>
    /// Obtains the object of type T located at the current pointer position.<br/>
    /// This method uses a raycast to detect objects under the pointer and attempts to retrieve the component of type T from the hit collider or its parent.<br/>
    /// </summary>
    /// <typeparam name="T"> </typeparam>
    /// <returns></returns>
    protected virtual T GetObjectAtPointer<T>() where T : IDraggable, IHoverable, ISelectable
          => TryGetObjectAtPointer(out T obj) ? obj : default(T);

    /// <summary>
    /// Tries to obtain the object of type T located at the current pointer position.<br/>
    /// This method uses a raycast to detect objects under the pointer and attempts to retrieve the component of type T from the hit collider or its parent.<br/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    protected virtual bool TryGetObjectAtPointer<T>(out T obj) where T : IDraggable, IHoverable, ISelectable {

      Vector2 worldPos = _pointerWorldPos;
      RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

      if (hit.collider != null) {
        //attempt to get obj from collider
        if (hit.collider.TryGetComponent(out obj)) {
          return true;
        }
        //attempt to get it from parent
        else if (hit.collider.transform.parent.TryGetComponent(out obj)) {
          return true;
        }
      }
      obj = default;
      return false;
    }

    private Vector2 ToScreenPosition(Vector3 worldPos) {
      Camera mainCamera = Camera.main;
      if (mainCamera == null) {
        this.LogWarning("[PlayableElementInputHandler] No main camera found for world to screen conversion");
        return Vector2.zero;
      }
      Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
      return new(screenPos.x, screenPos.y);
    }

    #endregion

    #region Input Event Listeners

    private void UpdatePointerPosition(InputActionEventArgs inputArgs) {
      _pointerScreenPos = inputArgs.ScreenPos;
      _pointerWorldPos = inputArgs.WorldPos;
    }

    /// <summary>
    /// Moves the active element in the direction of the arrow key or dpad pressed.
    /// This input exists for accessibility purposes, when a player can't hold and move the pointer simultaneously.
    /// The move inputs are expected as press only, therefore, the 'hold' interaction is implemented internally,
    /// because we assume the player might be unable to perform it. 
    /// </summary>
    /// <param name="inputArgs"></param>
    private void MoveActiveElement(InputActionEventArgs inputArgs)
    {
      this.Log($"MoveActiveElement - control: {inputArgs.Context.action.activeControl.name} | phase: {inputArgs.Phase} | " +
               $"IsMovePressed : {IsMovePressed} | CurrentDirection: {_currentMoveDirection} ");
      var phase = inputArgs.Phase;
      switch (phase)
      {
        // arrow key or dpad is pressed
        case InputActionPhase.Started:
          IsMovePressed = TryStartDrag(ActiveElement);
          break;
        // arrow key or dpad is released
        // because the input is expected to be a button press,
        // we consider both Performed and Canceled as the end of the movement, and reset the move state.
        case InputActionPhase.Performed:
          var inputValue = inputArgs.Context.ReadValue<Vector2>();
          this.Log($"MoveActiveElement - direction {inputValue} | IsMovePressed: {IsMovePressed}");
          if(IsMovePressed) UpdateActiveElementPosition(inputValue);
          break;
        case InputActionPhase.Canceled:
          var currentElement = ActiveElement;
          if (TryStopDrag())
          {
            //_activeElement = currentElement; // Keep the element selected after stopping drag
            IsMovePressed = false;
            _currentMoveDirection = Vector2.zero;
          }

          break;
      }

    }

    private void UpdateActiveElementPosition(Vector2 direction)
    {
      // only updates if active element exists, is selected and is draggable. Otherwise, the input is ignored.
      if (ActiveElement == null || !ActiveElement.IsSelected || !ActiveElement.IsDraggable) return;

      // direction is expected as normalized (0,1), (0,-1), (1,0) or (-1,0)
      // because comes from arrow keys or a dpad
      _currentMoveDirection = direction.normalized;
      
      // ignore zero
      if (_currentMoveDirection == Vector2.zero) return;
      
      var moveVector = _currentMoveDirection; //* (MoveSpeed * Time.deltaTime);
      var newPosition = (Vector2)ActiveElement.transform.position + moveVector ;
      // Update element position directly
      ActiveElement.transform.position = newPosition;
    }

    private void HandleSelect(InputActionEventArgs inputArgs) {
      var phase = inputArgs.Phase;
      var interaction = inputArgs.Context.interaction;
      
      // Handle Started phase for both Press and Hold interactions
      // determine what is pointer is over and select it
      if (phase is InputActionPhase.Started)
      {
        HandleSelectStartedPhase();
      }

      // HoldInteraction is triggered when the button is held down for a certain duration (default 0.5s)
      if (interaction is HoldInteraction) 
      {
        // Select button held down for hold threshold duration, trigger dragging
        if (phase is InputActionPhase.Performed)
        {
          if (!IsDragging)
          {
            // If there is an active element that is draggable, start dragging it
            if (ActiveElement && ActiveElement.IsDraggable)
            {
              IsSelectHold = TryStartDrag(ActiveElement);
            }
          }
        }

        // Hold button released before hold threshold met (canceled)
        if (phase is InputActionPhase.Canceled)
        {
          if (IsDragging)
          {
            TryStopDrag();
            IsSelectPressed = false;
            IsSelectHold = false;
          }
        }
      } // end of HoldInteraction handling
      
      
      // Handle PressInteraction for selection/deselection
      if (interaction is PressInteraction) // PressInteraction is triggered on button press and release
      {
        
        // Press performed: button pressed and released before hold threshold
        if (phase is InputActionPhase.Performed)
        {
          if (IsDragging)
          {
            TryStopDrag();
            IsSelectHold = false;
          }
          else
          {
            HandleSelectStartedPhase();
          }
        }
 
        // Press canceled (button released without triggering a hold)
        if (phase is InputActionPhase.Canceled)
        {
          IsSelectPressed = !TryDeselect();
        }
      } // end of PressInteraction handling
      
    }

    private void HandleSelectStartedPhase()
    {
      IsSelectPressed = true;
      PlayableElement oldSelected = null;
      // If there is no currently hovered element, try to update hover state to find one under the pointer.
      if (!CurrentHoveredElement) UpdateHover();

      // Deselect current selected if any
      if (SelectedElement)
      {
        oldSelected = SelectedElement;
        if (TryDeselect()) // The no-arg Deselect, deselects the currently selected element
        {
          SetActiveElement(null);
        }
      }

      // Try selecting the currently hovered element.
      // hovered element must be selectable and different from the previously selected.
      if (CurrentHoveredElement && CurrentHoveredElement.CanSelect && !CurrentHoveredElement.Equals(oldSelected))
      {
        TrySelect(CurrentHoveredElement);
      }
    }

    private void RotateCW(InputActionEventArgs inputArgs) {
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

    private void RotateCCW(InputActionEventArgs inputArgs) {
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
    
    private void FlipX(InputActionEventArgs inputArgs) {
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

    private void FlipY(InputActionEventArgs inputArgs) {
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
      if (_activeElement != null) action.Invoke(_activeElement);
    }

    #endregion


    private void DebugLog(string message) { if (_debugLogging) this.Log($"{_activeElement.name} - {message}"); }

    /// <summary>
    ///   Utility method to validate a RaycastHit2D is suited to for dragging.
    /// </summary>
    /// <returns>
    ///   <see langword="true"/> if <c>hit</c> is not-null and not-destroyed,
    ///   and has a non-null collider with a valid GameObject. Otherwise, <see langword="false"/>
    /// </returns>
    private static bool ValidRaycastHit(RaycastHit2D hit) => hit && hit.collider && hit.collider.gameObject;

    private static Camera GetActiveCamera()
    {
      var activeCamera = Camera.main;
      if (activeCamera != null) return activeCamera;
      
      // If no main camera is tagged, fallback to the first available camera in the scene
      activeCamera = Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null;
      foreach (var camera in Camera.allCameras)
      {
        if (!camera || !camera.isActiveAndEnabled) continue;
        activeCamera = camera;
        break;
      }
      if (activeCamera == null) { 
        Debug.LogWarning("[PlayableElementInputHandler] No active camera found for screen/world position conversions");
      }
      return activeCamera;
    }
    
#if UNITY_EDITOR
    public void TriggerHoveredEvent() => TriggerEvent(InputPointerPosition, InputActionPhase.Performed, 
      ToScreenPosition(_activeElement.transform.position), _activeElement.transform.position);

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