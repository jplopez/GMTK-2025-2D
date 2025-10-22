using Ameba;
using MoreMountains.Feedbacks;
using System;
using System.Collections;
using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Base class for components that extend PlayableElement functionality.
  /// This is the new component system that works with PlayableElement.
  /// </summary>
  [RequireComponent(typeof(PlayableElement))]
  public abstract class PlayableElementComponent : MonoBehaviour {

    [Header("Playable Element Component")]
    [Tooltip("Main switch to turn on/off a Component. Useful for debugging")]
    public bool IsActive = true;

    [Header("Delay Run")]
    [Tooltip("if true, this component will run on a coroutine, executing the logic on OnDelayedUpdateRun ")]
    public bool DelayRun = false;
    [Tooltip("Delay in seconds before executing the OnDelayedUpdateRun logic")]
    public float InitialDelay = 0.1f;

    [Header("Exclude Events")]

    [Tooltip("Excludes BeforeInitialize and AfterInitialize")]
    public bool ExcludeInitializeEvents = false;
    [Tooltip("Excludes OnSelected and OnDeselected")]
    public bool ExcludeSelectionEvents = false;
    [Tooltip("Excludes OnHovered and OnUnhovered")]
    public bool ExcludeHoverEvents = false;
    [Tooltip("Excludes BeforeDragStart and AfterDragStart")]
    public bool ExcludeDragStartEvents = false;
    [Tooltip("Excludes DuringDragging")]
    public bool ExcludeDuringDraggingEvents = false;
    [Tooltip("Excludes BeforeDragEnd and AfterDragEnd")]
    public bool ExcludeDragEndEvents = false;
    [Tooltip("Excludes BeforeInput and AfterInput")]
    public bool ExcludeInputControlsEvents = false;

    [Header("Inner Events")]
    [Tooltip("If true, this component will raise a PlayableElementInnerEvent after handling an event, relaying the same event args")]
    public bool TriggersInnerEvent = false;

    protected PlayableElement _playableElement;
    protected LevelGrid _levelGrid;
    protected GameEventChannel _gameEventChannel;

    protected bool isInitialized = false;
    private Coroutine _delayedUpdateCoroutine;

    public void ExcludeAllEvents(bool exclude = true) {
      ExcludeInitializeEvents = exclude;
      ExcludeSelectionEvents = exclude;
      ExcludeHoverEvents = exclude;
      ExcludeDragStartEvents = exclude;
      ExcludeDuringDraggingEvents = exclude;
      ExcludeDragEndEvents = exclude;
      ExcludeInputControlsEvents = exclude;
    }


    private void OnValidate() => InitDependencies();

    private void Awake() {
      TryInitialize();
    }

    private void OnDestroy() => RemoveListeners();

    #region Initialization

    public void TryInitialize() {
      if (isInitialized) return;
      InitDependencies();
      Initialize();
      AddListeners();
      isInitialized = true;
    }
    protected virtual void InitDependencies() {
      _playableElement = (_playableElement == null) ? gameObject.GetComponent<PlayableElement>() : _playableElement;
      if (_playableElement == null) Debug.LogWarning($"PlayableElementComponent {name} is missing PlayableElement. This component will not function");

      _levelGrid = (_levelGrid == null) ? FindAnyObjectByType<LevelGrid>() : _levelGrid;

      _gameEventChannel = (_gameEventChannel == null) ? ServiceLocator.Get<GameEventChannel>() : _gameEventChannel;
      if (_gameEventChannel == null) Debug.LogWarning($"PlayableElementComponent {name} can't find GameEventChannel. This component won't listen or trigger game events");
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Handles an event related to the current playable element.<br/>
    /// <para>
    /// This method processes the event only if the <see cref="PlayableElementEventArgs.Element"/> matches the current playable element.<br/> 
    /// It also broadcasts the event internally through the game event channel for other components to listen and handles the event locally (chain events).
    /// </para>
    /// </summary>
    /// <param name="eventArgs">The event data containing information about the playable element and the event.</param>
    public virtual void OnPlayableElementEvent(PlayableElementEventArgs eventArgs) {
      if (eventArgs.Element != _playableElement) return;

      //// Broadcast the event internally over GameEventChannel for other components to listen
      //_gameEventChannel.Raise(GameEventType.PlayableElementInternalEvent, eventArgs);

      // Handle the event locally using reflection
      HandlePlayableElementEvent(eventArgs);

      if (TriggersInnerEvent) {
        _gameEventChannel.Raise(GameEventType.PlayableElementInternalEvent, eventArgs);
      }
    }

    /// <summary>
    /// <para>
    /// Common method to handle broadcasted events of the type <c>GameEventType.PlayableElementEvent</c> through the <see cref="GameEventChannel"/> event channel.<br/> 
    /// Internally, <see cref="PlayableElementComponent"/> handle these events as <see cref="PlayableElementEventArgs"/>, using the <see cref="PlayableElementEventType"/> enum.
    /// </para>
    /// <para>
    /// To handle a <c>PlayableElementEvent</c> the <see cref="PlayableElementComponent"/> must have a method named as "On" + PlayableElementEventType name in args.<br/>
    /// For example, the method <c>OnDragStart</c> handles <c>PlayableElementEventType.DragStart</c> events. The method must receive a single <see cref="PlayableElementEventArgs"/> argument.
    /// </para>
    /// </summary>
    /// <param name="args"></param>
    private void HandlePlayableElementEvent(PlayableElementEventArgs args) {
      // Only handle events for our own PlayableElement
      if (args.Element != _playableElement) return;

      // Use reflection to look for a method named as "On" + PlayableElementEventType name in args
      // For example OnDragStart for PlayableElementEventType.DragStart, that receives a PlayableElementEventArgs argument
      string methodName = "On" + args.EventType.ToString();
      System.Reflection.MethodInfo method = GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
      this.LogDebug($"handling event '{args.EventType}' using method '{methodName}'");
      if (method != null) {
        try {
          method.Invoke(this, new object[] { args });
        }
        catch (Exception ex) {
          this.LogError($"Error invoking method {methodName}: {ex.Message}");
        }
      }
      else {
        // Optional: Log when no handler method is found (useful for debugging)
        // this.LogWarning($"No handler method found for {methodName}");
      }
    }

    #endregion

    #region Event Listeners

    protected virtual void AddListeners() {
      if (_gameEventChannel == null) return;

      // Listen to PlayableElement events through the GameEventChannel
      _gameEventChannel.AddListener<PlayableElementEventArgs>(GameEventType.PlayableElementInternalEvent, OnPlayableElementEvent);

      //// Listen to global game events
      //_gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementSelected, HandleGlobalElementSelected);
      //_gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementDropped, HandleGlobalElementDropped);
      //_gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementHovered, HandleGlobalElementHovered);
      //_gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, HandleGlobalElementUnhovered);

      //Add UnityEvents subscription to PlayableElement
      if (_playableElement == null) return;
      int listenersAdded = 0;
      if (!ExcludeInitializeEvents) {
        _playableElement.BeforeInitialize.AddListener(OnPlayableElementEvent);
        _playableElement.AfterInitialize.AddListener(OnPlayableElementEvent);
        listenersAdded += 2;
      }

      if (!ExcludeSelectionEvents) {
        _playableElement.OnSelected.AddListener(OnPlayableElementEvent);
        _playableElement.OnDeselected.AddListener(OnPlayableElementEvent);
        listenersAdded += 2;
      }

      if (!ExcludeHoverEvents) {
        _playableElement.OnHovered.AddListener(OnPlayableElementEvent);
        _playableElement.OnUnhovered.AddListener(OnPlayableElementEvent);
        listenersAdded += 2;
      }

      if (!ExcludeDragStartEvents) {
        _playableElement.BeforeDragStart.AddListener(OnPlayableElementEvent);
        _playableElement.AfterDragStart.AddListener(OnPlayableElementEvent);
        listenersAdded += 2;
      }

      if (!ExcludeDuringDraggingEvents) {
        _playableElement.DuringDragging.AddListener(OnPlayableElementEvent);
        listenersAdded++;
      }

      if (!ExcludeDragEndEvents) {
        _playableElement.BeforeDragEnd.AddListener(OnPlayableElementEvent);
        _playableElement.AfterDragEnd.AddListener(OnPlayableElementEvent);
        listenersAdded += 2;
      }

      if (!ExcludeInputControlsEvents) {
        _playableElement.BeforeInput.AddListener(OnPlayableElementEvent);
        _playableElement.AfterInput.AddListener(OnPlayableElementEvent);
        listenersAdded += 2;
      }
      this.Log($" Added {listenersAdded} UnityEvent Listeners to '{_playableElement.name}'");
    }

    protected virtual void RemoveListeners() {
      if (_gameEventChannel == null) return;

      //_gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementSelected, HandleGlobalElementSelected);
      //_gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementDropped, HandleGlobalElementDropped);
      //_gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementHovered, HandleGlobalElementHovered);
      //_gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, HandleGlobalElementUnhovered);

      _gameEventChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.PlayableElementInternalEvent, OnPlayableElementEvent);

      //remove UnityEvents subscription to PlayableElement
      if (_playableElement != null) {
        _playableElement.BeforeInitialize.RemoveListener(OnPlayableElementEvent);
        _playableElement.AfterInitialize.RemoveListener(OnPlayableElementEvent);

        _playableElement.OnSelected.RemoveListener(OnPlayableElementEvent);
        _playableElement.OnDeselected.RemoveListener(OnPlayableElementEvent);

        _playableElement.OnHovered.RemoveListener(OnPlayableElementEvent);
        _playableElement.OnUnhovered.RemoveListener(OnPlayableElementEvent);

        _playableElement.BeforeDragStart.RemoveListener(OnPlayableElementEvent);
        _playableElement.AfterDragStart.RemoveListener(OnPlayableElementEvent);

        _playableElement.DuringDragging.RemoveListener(OnPlayableElementEvent);

        _playableElement.BeforeDragEnd.RemoveListener(OnPlayableElementEvent);
        _playableElement.AfterDragEnd.RemoveListener(OnPlayableElementEvent);

        _playableElement.BeforeInput.RemoveListener(OnPlayableElementEvent);
        _playableElement.AfterInput.RemoveListener(OnPlayableElementEvent);
      }
    }

    #endregion

    #region Component Lifecycle

    public void RunBeforeUpdate() { if (IsActive && isInitialized) BeforeUpdate(); }
    public void RunOnUpdate() {
      if (!IsActive || !isInitialized || !Validate()) return;
      if (DelayRun) {
        RunDelayOnUpdate();
      }
      else {
        OnUpdate();
      }
    }
    public void RunAfterUpdate() { if (IsActive && isInitialized) AfterUpdate(); }
    public void RunFinalize() { if (IsActive && isInitialized) FinalizeComponent(); }

    public void RunDelayOnUpdate() {
      if (!IsActive || !isInitialized || !Validate()) return;
      if (_delayedUpdateCoroutine != null)
        StopCoroutine(_delayedUpdateCoroutine);

      _delayedUpdateCoroutine = StartCoroutine(DelayedUpdateRoutine(InitialDelay));
    }

    public void RunResetComponent() { if (IsActive) ResetComponent(); }

    protected virtual IEnumerator DelayedUpdateRoutine(float delay) {
      yield return new WaitForSeconds(delay);
      OnDelayedUpdate();
    }

    public void CancelDelayRun() {
      if (IsActive && isInitialized) StopAllCoroutines();
    }

    // Lifecycle hooks
    protected abstract void Initialize();
    protected virtual void BeforeUpdate() { }
    protected abstract bool Validate(); // Return true if component is ready to run on Update
    protected virtual void OnUpdate() { }
    protected virtual void AfterUpdate() { }
    protected virtual void FinalizeComponent() { }
    protected virtual void OnDelayedUpdate() { }
    protected virtual void ResetComponent() { }

    /// <summary>
    /// null safe method to play a MMF_Player feedback
    /// </summary>
    /// <param name="feedback"></param>
    protected virtual void PlayFeedback(MMF_Player feedback, bool playInReverse = false) {
      if (feedback != null && feedback.gameObject.activeInHierarchy) {
        if (playInReverse) {
          feedback.PlayFeedbacksInReverse();
        }
        else {
          feedback.PlayFeedbacks();
        }
      }
    }
    protected virtual void StopFeedback(MMF_Player feedback) {
      if (feedback != null && feedback.gameObject.activeInHierarchy) {
        feedback.StopFeedbacks();
      }
    }
    protected virtual void RaisePlayableElementEvent(PlayableElementEventType eventType, Vector3? worldPosition = null, GameObject otherObject = null) {
      var position = worldPosition ?? _playableElement.SnapTransform.position;
      PlayableElementEventArgs eventArgs = new(_playableElement, position, eventType, otherObject);
      _gameEventChannel.Raise(GameEventType.PlayableElementEvent, eventArgs);
    }

    #endregion

    #region UnityEvent Handlers

    public virtual void OnBeforeInitialize(PlayableElementEventArgs args) { }
    public virtual void OnAfterInitialize(PlayableElementEventArgs args) { }
    public virtual void OnSelected(PlayableElementEventArgs args) { }
    public virtual void OnDeselected(PlayableElementEventArgs args) { }
    public virtual void OnHovered(PlayableElementEventArgs args) { }
    public virtual void OnUnhovered(PlayableElementEventArgs args) { }
    public virtual void OnBeforeDragStart(PlayableElementEventArgs args) { }
    public virtual void OnAfterDragStart(PlayableElementEventArgs args) { }
    public virtual void OnBeforeDragEnd(PlayableElementEventArgs args) { }
    public virtual void OnAfterDragEnd(PlayableElementEventArgs args) { }
    public virtual void OnBeforeInput(PlayableElementEventArgs args) { }
    public virtual void OnAfterInput(PlayableElementEventArgs args) { }

    internal void SetPlayableElement(PlayableElement playableElement) {
      _playableElement = playableElement;
    }

    #endregion

    #region Obsolete

    //// Bridge methods for backward compatibility with GridSnappable events
    //[Obsolete("Use UnityEvents instead")]
    //private void HandleGlobalElementSelected(GridSnappableEventArgs evt) {
    //  // Convert to PlayableElement events if this component's element was selected
    //  // This is for backward compatibility during transition
    //  HandleElementSelected(evt);
    //}

    //[Obsolete("Use UnityEvents instead")]
    //private void HandleGlobalElementDropped(GridSnappableEventArgs evt) {
    //  HandleElementDropped(evt);
    //}

    //[Obsolete("Use UnityEvents instead")]
    //private void HandleGlobalElementHovered(GridSnappableEventArgs evt) {
    //  HandleElementHovered(evt);
    //}

    //[Obsolete("Use UnityEvents instead")]
    //private void HandleGlobalElementUnhovered(GridSnappableEventArgs evt) {
    //  HandleElementUnhovered(evt);
    //}

    // Abstract methods for legacy compatibility - these still need to be implemented
    //[Obsolete("Use UnityEvents instead")]
    //protected virtual void HandleElementSelected(GridSnappableEventArgs evt) { }
    //[Obsolete("Use UnityEvents instead")]
    //protected virtual void HandleElementDropped(GridSnappableEventArgs evt) { }
    //[Obsolete("Use UnityEvents instead")]
    //protected virtual void HandleElementHovered(GridSnappableEventArgs evt) { }
    //[Obsolete("Use UnityEvents instead")]
    //protected virtual void HandleElementUnhovered(GridSnappableEventArgs evt) { }

    #endregion

  }
}