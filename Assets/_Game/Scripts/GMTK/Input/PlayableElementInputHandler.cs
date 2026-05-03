using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using Ameba;
using static GMTK.GameEventType;

namespace GMTK
{
  [Flags]
  public enum SelectionTrigger
  {
    None = 0,
    OnClick = 1,
    OnKeyPress = 2
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
  public partial class PlayableElementInputHandler : MonoBehaviour
  {
    [SerializeField] private bool _enableInput = true;
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

    [Range(0f, 1f)] public float PointerTolerance = 0.25f;

    #endregion

    #region MonoBehaviour methods

    private void Awake()
    {
      if (_eventsChannel == null)
      {
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

    public void Update()
    {
      if (!_enableInput) return;

      // Update hover detection
      UpdateHover();

      // Handle element position updates during dragging
      UpdateDrag(_pointerWorldPos);

      // To handle 'hold' of arrow keys or dpad for moving elements,
      // we need to continuously update the position while the input is held.
      if (IsMovePressed) UpdateActiveElementPosition(_currentMoveDirection);
    }

    // The element dragging is disabled during Playing, to prevent players
    // from moving elements while the level is running
    public void UpdateFromGameState(GameStates state)
    {
      switch (state)
      {
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
    protected virtual void SetActiveElement(PlayableElement element)
    {
      if (element != null && element != _activeElement)
      {
        // Deactivate previous
        if (_activeElement != null && _activeElement != element)
        {
          _activeElement.IsActive = false;
          _activeElement.OnDeselect();
        }

        // Activate new
        _activeElement = element;
        if (_activeElement != null)
        {
          _activeElement.IsActive = true;
          _activeElement.OnSelect();
        }
      }
      else
      {
        // Deactivate current
        if (_activeElement != null)
        {
          _activeElement.IsActive = false;
          _activeElement.OnDeselect();
          _activeElement = null;
        }
      }

      this.LogDebug($"Active object changed to {(element != null ? element.name : "null")}");
    }

    #endregion

    
    #region Input Event Listeners

    private void UpdatePointerPosition(InputActionEventArgs inputArgs)
    {
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
      this.Log(
        $"MoveActiveElement - control: {inputArgs.Context.action.activeControl.name} | phase: {inputArgs.Phase} | " +
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
          if (IsMovePressed) UpdateActiveElementPosition(inputValue);
          break;
        case InputActionPhase.Canceled:
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
      var newPosition = (Vector2)ActiveElement.transform.position + moveVector;
      // Update element position directly
      ActiveElement.transform.position = newPosition;
    }

    private void HandleSelect(InputActionEventArgs inputArgs)
    {
      var phase = inputArgs.Phase;
      var interaction = inputArgs.Context.interaction;

      switch (interaction)
      {
        case HoldInteraction:
          HandleSelectHoldInteraction(phase);
          break;
        case PressInteraction:
          HandleSelectPressInteraction(phase);
          break;
        default:
          this.LogDebug(
            $"[HandleSelect] Unhandled interaction type {interaction?.GetType().Name ?? "null"} for InputSelected event");
          break;
      }
    }

    private void HandleSelectPressInteraction(InputActionPhase phase)
    {
      switch (phase)
      {
        // Handle Started phase for both Press and Hold interactions
        // determine what is pointer is over and select it
        case InputActionPhase.Started:
          HandleSelectStartedPhase();
          break;
        // Press performed: button pressed and released before hold threshold
        case InputActionPhase.Performed:
        {
          if (IsDragging)
          {
            TryStopDrag();
            IsSelectHold = false;
          }
          break;
        }
        // Press canceled (button released without triggering a hold)
        case InputActionPhase.Canceled:
          IsSelectPressed = false;
          break;
        case InputActionPhase.Disabled:
        case InputActionPhase.Waiting:
        default:
          this.LogDebug($"Unhandled input phase {phase} for PressInteraction in HandleSelect");
          break;
      }
    }

    private void HandleSelectHoldInteraction(InputActionPhase phase)
    {
      switch (phase)
      {
        case InputActionPhase.Started:
          if (IsDragging)
          {
            this.LogWarning(
              "[HandleSelect] Received HoldInteraction Started phase while already dragging an element. " +
              "We will stop the current drag operation to prevent " +
              "getting stuck in a dragging state.");
            TryStopDrag();
          }

          break;
        // Select button held down for hold threshold duration, trigger dragging
        case InputActionPhase.Performed:
        {
          if (!IsDragging)
          {
            // If there is an active element that is draggable, start dragging it
            if (ActiveElement && ActiveElement.IsDraggable)
            {
              IsSelectHold = TryStartDrag(ActiveElement);
            }
          }

          break;
        }
        // Hold button released before hold threshold met (canceled)
        case InputActionPhase.Canceled:
        {
          if (IsDragging)
          {
            TryStopDrag();
            IsSelectPressed = false;
            IsSelectHold = false;
          }

          break;
        }
        case InputActionPhase.Disabled:
        case InputActionPhase.Waiting:
        default:
          this.LogDebug($"Unhandled input phase {phase} for HoldInteraction in HandleSelect");
          break;
      }
    }

    private void HandleSelectStartedPhase()
    {
      IsSelectPressed = true;

      PlayableElement currentlySelected = SelectedElement;

      // Resolve exact selectable target under pointer (element vs controls vs none).
      //if (TryGetObjectAtPointer<ISelectable>(out var hitSelectable))
      if(TryGetSelectableTargetAtPointer(out var hitSelectable))
      {
        switch (hitSelectable)
        {
          // If hit a control, delegate the selection to the control's own logic, without affecting element selection.
          case ElementUIControlsComponent controlsComponent:
            controlsComponent.OnSelect();
            break;
          // if hit element that cannot be selected, proceed with deselect current
          case PlayableElement { CanSelect: false }:
          {
            if (currentlySelected && TryDeselect())
            {
              SetActiveElement(null);
            }
            break;
          }
          // if the pointer hit the same currently selected element, is deselected.
          case PlayableElement hitElement when currentlySelected && currentlySelected == hitElement && TryDeselect():
            SetActiveElement(null);
            break;
          // Replace previous selection with the new hit element.
          case PlayableElement hitElement:
          {
            if (currentlySelected && TryDeselect())
            {
              SetActiveElement(null);
            }
            TrySelect(hitElement);
            break;
          }
        }
      }
      // nothing hit
      else
      {
        SetActiveElement(null);
        TryDeselect();
      }
    }
    
  

    /// <summary>
    /// Handles the input event of pressing the rotate clockwise button.
    /// If there is no selected element, or the element cannot perform the action, this method does nothing
    /// </summary>
    /// <param name="inputArgs"></param>
    private void RotateCW(InputActionEventArgs inputArgs) =>
      ExecuteOnSelectedElement((e) => { e.RotateClockwise(); }, inputArgs);

    /// <summary>
    /// Handles the input event of pressing the rotate counterclockwise button.
    /// If there is no selected element, or the element cannot perform the action, this method does nothing
    /// </summary>
    /// <param name="inputArgs"></param>
    private void RotateCCW(InputActionEventArgs inputArgs) =>
      ExecuteOnSelectedElement((e) => { e.RotateCounterClockwise(); }, inputArgs);

    /// <summary>
    /// Handles the input event of pressing the flip horizontally button.
    /// If there is no selected element, or the element cannot perform the action, this method does nothing
    /// </summary>
    /// <param name="inputArgs"></param>
    private void FlipX(InputActionEventArgs inputArgs) =>
      ExecuteOnSelectedElement((e) => { e.FlipX(); }, inputArgs);

    /// <summary>
    /// Handles the input event of pressing the flip vertically button.
    /// If there is no selected element, or the element cannot perform the action, this method does nothing
    /// </summary>
    /// <param name="inputArgs"></param>
    private void FlipY(InputActionEventArgs inputArgs) =>
      ExecuteOnSelectedElement((e) => { e.FlipY(); }, inputArgs);


    /// <summary>
    /// Helper method to execute methods on PlayableElement based on input events, if the element is selected and the input phase matches the expected phase.
    /// </summary>
    /// <param name="action">Delegate with the action over the element</param>
    /// <param name="inputArgs">The arguments received from the input event</param>
    /// <param name="phase">The expected phase for the input. If not specified, assumes <c>InputActionPhase.Performed</c> </param>
    private void ExecuteOnSelectedElement(Action<PlayableElement> action, InputActionEventArgs inputArgs,
      InputActionPhase phase = InputActionPhase.Performed)
    {
      if (!SelectedElement) return;
      if (inputArgs.Phase == phase) action.Invoke(SelectedElement);
    }

    #endregion


    /// <summary>
    /// Attempts to resolve if there is a valid <see cref="ISelectable"/> in the <c>_pointerWorldPos</c> position,
    /// and returns it in the out parameter. <br/>
    /// This method is constraint to return true if the found object is of type <see cref="ElementUIControlsComponent"/>
    /// or <see cref="PlayableElement"/>.<br/> 
    /// Also, uses the <see cref="PointerTolerance"/> value to be more forgiving with the raycast detection. <br/>
    /// </summary>
    /// <param name="foundSelectable">out parameter with the reference to the found <c>ISelectable</c>.
    ///     null if the method returns <see langword="false"/></param>
    /// <returns>
    ///   <see langword="true"/> if there is a valid <c>ISelectable</c>. Otherwise, <see langword="false"/>
    /// </returns>
    private bool TryGetSelectableTargetAtPointer(out ISelectable foundSelectable)
    {
      foundSelectable = null;
      Vector2 worldPos2D = new(_pointerWorldPos.x, _pointerWorldPos.y);

      if (!TryResolveRaycastHit2D(worldPos2D, out RaycastHit2D hit)) return false;

      Transform hitTransform = hit.collider.transform;

      // Prioritize controls when the hit is inside their instantiated controls hierarchy.
      ElementUIControlsComponent controlsComponent = hitTransform.GetComponentInParent<ElementUIControlsComponent>();
      if (controlsComponent && controlsComponent.CanSelect && controlsComponent.SelectTransform &&
          hitTransform.IsChildOf(controlsComponent.SelectTransform))
      {
        foundSelectable = controlsComponent;
        return true;
      }

      // Fallback to PlayableElement selection.
      PlayableElement element = hitTransform.GetComponentInParent<PlayableElement>();
      if (element && element.CanSelect)
      {
        foundSelectable = element;
        return true;
      }

      return false;
    }

    /// <summary>
    /// <para>
    /// Resolves the Raycast strategy to find an element at the current pointer position.
    /// </para>
    /// <para>
    /// This method is based on the PointerTolerance parameter.<br/>
    /// If PointerTolerance is zero, a single raycast is performed.<br/>
    /// If PointerTolerance is greater than zero, a circle cast is performed with the specified radius,
    /// allowing for more forgiving detection around the pointer position.
    /// </para>
    /// </summary>
    /// <param name="worldPos2D">The world position to resolve the raycast</param>
    /// <param name="resolvedHit">RaycastHit2D with the collider and GameObject reference</param>
    /// <returns>true if the RaycastHit is not null and has a collider with a GameObject reference. Otherwise, false</returns>
    private bool TryResolveRaycastHit2D(Vector2 worldPos2D, out RaycastHit2D resolvedHit)
    {
      resolvedHit = PointerTolerance > 0f
        ? Physics2D.CircleCast(worldPos2D, PointerTolerance, Vector2.zero)
        : Physics2D.Raycast(worldPos2D, Vector2.zero);
      return ValidRaycastHit(resolvedHit);
    }


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

      if (activeCamera == null)
      {
        Debug.LogWarning(
          "[PlayableElementInputHandler] No active camera found for screen/world position conversions");
      }

      return activeCamera;
    }

#if UNITY_EDITOR
    
    private Vector2 ToScreenPosition(Vector3 worldPos)
    {
      Camera mainCamera = GetActiveCamera();
      if (mainCamera == null)
      {
        this.LogWarning("[PlayableElementInputHandler] No main camera found for world to screen conversion");
        return Vector2.zero;
      }

      Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
      return new(screenPos.x, screenPos.y);
    }
    
    public void TriggerHoveredEvent() => TriggerEvent(InputPointerPosition, InputActionPhase.Performed,
      ToScreenPosition(_activeElement.transform.position), _activeElement.transform.position);

    public void TriggerUnhoveredEvent() => TriggerEvent(InputPointerPosition, InputActionPhase.Performed);

    public void TriggerSelectedEvent() => TriggerEvent(InputSelected, InputActionPhase.Performed);

    public void TriggerDragEvent() => TriggerEvent(InputPointerPosition, InputActionPhase.Performed);

    public void TriggerDroppedEvent() => TriggerEvent(InputSelected, InputActionPhase.Performed);

    private void TriggerEvent(GameEventType gameType, InputActionPhase phase, Vector2 screenPos = default,
      Vector3 worldPos = default)
      => _eventsChannel.Raise(gameType,
        new InputActionEventArgs(gameType, phase, new InputAction.CallbackContext(),
          Equals(screenPos, default) ? _pointerScreenPos : screenPos,
          Equals(worldPos, default) ? _pointerWorldPos : worldPos));

#endif
  }
}


