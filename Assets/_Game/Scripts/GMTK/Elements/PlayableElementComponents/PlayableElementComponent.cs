using Ameba;
using MoreMountains.Feedbacks;
using System;
using System.Collections;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// <para>
  /// Base class to implement components that enhance <see cref="PlayableElement"/> functionality (Physics, Controls, Pointer, etc).<br/>
  /// <see cref="PlayableElementComponent"/> (PEC) integrates into the PlayableElement lifecycle and event system.
  /// </para>
  /// <para>
  /// <b>How to use it:</b><br/>
  ///     1. Create a new class that inherits from <see cref="PlayableElementComponent"/>.<br/>
  ///     2. Override the lifecycle methods needed to implement custom behavior.<br/>
  ///     3. Implement event handlers for UnityEvents if needed.<br/>
  ///     4. Add the new component to a GameObject with a <see cref="PlayableElement"/> component.
  /// </para>
  /// <para>
  /// <b>Life cycle methods</b><br/>
  /// The following methods can be override to customize the PEC:
  /// <list type="bullet">
  ///     <item><c>Initialize()</c>: called once when the component is initialized.</item>
  ///     <item><c>Validate()</c>: called before each update to check if the component is ready to run.</item>
  ///     <item><c>BeforeUpdate()</c></item>
  ///     <item><c>OnUpdate()</c></item>
  ///     <item><c>OnDelayedUpdate()</c>: called instead of OnUpdate, if the component has an initial delay</item>
  ///     <item><c>AfterUpdate()</c></item>
  ///     <item><c>FinalizeComponent()</c>: called when the PlayableElement is destroyed</item>
  ///     <item><c>ResetComponent()</c>: called when the PlayableElement is reseted</item>
  /// </list>
  /// </para>
  /// <para>
  /// <b>UnityEvents hooks</b><br/>
  /// The following methods can be used to hook into UnityEvents triggered by a <see cref="PlayableElement"/>:
  /// <list type="bullet">
  ///     <item><c>OnSelected(PlayableElementEventArgs args)</c></item>
  ///     <item><c>OnDeselected(PlayableElementEventArgs args)</c></item>
  ///     <item><c>OnHovered(PlayableElementEventArgs args) </c></item>
  ///     <item><c>OnUnhovered(PlayableElementEventArgs args) </c></item>
  ///     <item><c>OnDragStart(PlayableElementEventArgs args) </c></item>
  ///     <item><c>OnDragging(PlayableElementEventArgs args) </c></item>
  ///     <item><c>OnDragEnd(PlayableElementEventArgs args) </c></item>
  ///     <item><c>OnPlayerInput(PlayableElementEventArgs args) </c></item>
  ///     <item><c>OnRotate(PlayableElementEventArgs args) </c></item>
  ///     <item><c>OnFlip(PlayableElementEventArgs args) </c></item>
  /// </list>
  /// </para>
  /// </summary>
  [RequireComponent(typeof(PlayableElement))]
  public abstract class PlayableElementComponent : MonoBehaviour {

    [Header("Common Component Settings")]
    // this object is to group common settings in a foldable section in the inspector <see cref="CommonComponentSettings"/>
    public CommonComponentSettings CommonSettings = new();

    public bool IsActive => CommonSettings.IsActive;

    protected PlayableElement _playableElement;
    protected LevelGrid _levelGrid;
    protected GameEventChannel _gameEventChannel;
    protected bool isInitialized = false;
    private Coroutine _delayedUpdateCoroutine;

    internal void SetPlayableElement(PlayableElement playableElement) => _playableElement = playableElement;

    public void ExcludeAllEvents(bool exclude = true) {
      CommonSettings.ExcludeInitializeEvents = exclude;
      CommonSettings.ExcludeSelectionEvents = exclude;
      CommonSettings.ExcludeHoverEvents = exclude;
      CommonSettings.ExcludeDragStartEvents = exclude;
      CommonSettings.ExcludeDuringDraggingEvents = exclude;
      CommonSettings.ExcludeDragEndEvents = exclude;
      CommonSettings.ExcludeInputControlsEvents = exclude;
    }

    private void OnValidate() => InitDependencies();

    private void Awake() => TryInitialize();

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

    #region EventChannel

    /// <summary>
    /// Handles an event related to the current playable element.<br/>
    /// <para>
    /// This method processes the event only if the <see cref="PlayableElementEventArgs.Element"/> matches the current playable element.<br/> 
    /// It also broadcasts the event internally through the game event channel for other components to listen and handles the event locally (chain events).
    /// </para>
    /// </summary>
    /// <param name="eventArgs">The event data containing information about the playable element and the event.</param>
    //public virtual void OnPlayableElementEvent(PlayableElementEventArgs eventArgs) {
    //  if (eventArgs.Element != _playableElement) return;

    //  // Handle the event locally using reflection
    //  HandlePlayableElementEvent(eventArgs);

    //  if (CommonSettings.TriggersInnerEvent) {
    //    _gameEventChannel.Raise(GameEventType.PlayableElementInternalEvent, eventArgs);
    //  }
    //}

    /// <summary>
    /// <para>
    /// Common method to handle broadcasted events of the type <c>GameEventType.PlayableElementEvent</c> through the <see cref="GameEventChannel"/> event channel.<br/> 
    /// Internally, <see cref="PlayableElementComponent"/> handle these events as <see cref="PlayableElementEventArgs"/>, using the <see cref="PlayableElementEventType"/> enum.
    /// </para>
    /// <para>
    /// To handle a <c>PlayableElementEvent</c> the <see cref="PlayableElementComponent"/> must have a method named as "On" + PlayableElementEventType name in args.<br/>
    /// For example, the method <c>DragStart</c> handles <c>PlayableElementEventType.DragStart</c> events. The method must receive a single <see cref="PlayableElementEventArgs"/> argument.
    /// </para>
    /// </summary>
    /// <param name="args"></param>
    //private void HandlePlayableElementEvent(PlayableElementEventArgs args) {
    //  // Only handle events for our own PlayableElement
    //  if (args.Element != _playableElement) return;

    //  // Use reflection to look for a method named as "On" + PlayableElementEventType name in args
    //  // For example DragStart for PlayableElementEventType.DragStart, that receives a PlayableElementEventArgs argument
    //  string methodName = "On" + args.EventType.ToString();
    //  System.Reflection.MethodInfo method = GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
    //  this.LogDebug($"handling event '{args.EventType}' using method '{methodName}'");
    //  if (method != null) {
    //    try {
    //      method.Invoke(this, new object[] { args });
    //    }
    //    catch (Exception ex) {
    //      this.LogError($"Error invoking method {methodName}: {ex.Message}");
    //    }
    //  }
    //  else {
    //    // Optional: Log when no handler method is found (useful for debugging)
    //    // this.LogWarning($"No handler method found for {methodName}");
    //  }
    //}

    #endregion

    #region Lifecycle

    public void RunBeforeUpdate() { if (CommonSettings.IsActive && isInitialized) BeforeUpdate(); }
    public void RunOnUpdate() {
      if (!CommonSettings.IsActive || !isInitialized || !Validate()) return;
      if (CommonSettings.DelayRun) {
        RunDelayOnUpdate();
      }
      else {
        OnUpdate();
      }
    }
    public void RunAfterUpdate() { if (CommonSettings.IsActive && isInitialized) AfterUpdate(); }
    public void RunFinalize() { if (CommonSettings.IsActive && isInitialized) FinalizeComponent(); }

    public void RunDelayOnUpdate() {
      if (!CommonSettings.IsActive || !isInitialized || !Validate()) return;
      if (_delayedUpdateCoroutine != null)
        StopCoroutine(_delayedUpdateCoroutine);

      _delayedUpdateCoroutine = StartCoroutine(DelayedUpdateRoutine(CommonSettings.InitialDelay));
    }

    public void RunResetComponent() { if (CommonSettings.IsActive) ResetComponent(); }

    protected virtual IEnumerator DelayedUpdateRoutine(float delay) {
      yield return new WaitForSeconds(delay);
      OnDelayedUpdate();
    }

    public void CancelDelayRun() {
      if (CommonSettings.IsActive && isInitialized) StopAllCoroutines();
    }

    #endregion

    #region EventListeners

    protected virtual void AddListeners() {
      if (_gameEventChannel == null) return;

      // Listen to PlayableElement events through the GameEventChannel
      //_gameEventChannel.AddListener<PlayableElementEventArgs>(GameEventType.PlayableElementInternalEvent, OnPlayableElementEvent);

      //Add UnityEvents subscription to PlayableElement
      if (_playableElement == null) return;
      int listenersAdded = 0;

      if (!CommonSettings.ExcludeSelectionEvents) {
        _playableElement.OnSelected.AddListener(OnSelected);
        _playableElement.OnDeselected.AddListener(OnDeselected);
        listenersAdded += 2;
      }

      if (!CommonSettings.ExcludeHoverEvents) {
        _playableElement.OnHovered.AddListener(OnHovered);
        _playableElement.OnUnhovered.AddListener(OnUnhovered);
        listenersAdded += 2;
      }

      if (!CommonSettings.ExcludeDragStartEvents) {
        _playableElement.OnDragStart.AddListener(OnDragStart);
        listenersAdded++;
      }

      if (!CommonSettings.ExcludeDuringDraggingEvents) {
        _playableElement.OnDragging.AddListener(OnDragging);
        listenersAdded++;
      }

      if (!CommonSettings.ExcludeDragEndEvents) {
        _playableElement.OnDragEnd.AddListener(OnDragEnd);
        listenersAdded++;
      }

      if (!CommonSettings.ExcludeInputControlsEvents) {
        _playableElement.OnPlayerInput.AddListener(OnPlayerInput);
        _playableElement.OnFlip.AddListener(OnFlip);
        _playableElement.OnRotate.AddListener(OnRotate);
        listenersAdded += 3;
      }
      this.Log($" Added {listenersAdded} UnityEvent Listeners to '{_playableElement.name}'");
    }

    protected virtual void RemoveListeners() {
      if (_gameEventChannel == null) return;

      //_gameEventChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.PlayableElementInternalEvent, OnPlayableElementEvent);

      //remove UnityEvents subscription to PlayableElement
      if (_playableElement != null) {

        _playableElement.OnSelected.RemoveListener(OnSelected);
        _playableElement.OnDeselected.RemoveListener(OnDeselected);
        _playableElement.OnHovered.RemoveListener(OnHovered);
        _playableElement.OnUnhovered.RemoveListener(OnUnhovered);
        _playableElement.OnDragStart.RemoveListener(OnDragStart);
        _playableElement.OnDragging.RemoveListener(OnDragging);
        _playableElement.OnDragEnd.RemoveListener(OnDragEnd);
        _playableElement.OnPlayerInput.RemoveListener(OnPlayerInput);
        _playableElement.OnFlip.RemoveListener(OnFlip);
        _playableElement.OnRotate.RemoveListener(OnRotate);

      }
    }

    #endregion

    #region Lifecycle hooks
    protected abstract void Initialize();
    protected virtual void BeforeUpdate() { }
    protected abstract bool Validate(); // Return true if component is ready to run on Update
    protected virtual void OnUpdate() { }
    protected virtual void AfterUpdate() { }
    protected virtual void FinalizeComponent() { }
    protected virtual void OnDelayedUpdate() { }
    protected virtual void ResetComponent() { }

    #endregion

    #region UnityEvent Hooks

    public virtual void OnSelected(PlayableElementEventArgs args) { }
    public virtual void OnDeselected(PlayableElementEventArgs args) { }
    public virtual void OnHovered(PlayableElementEventArgs args) { }
    public virtual void OnUnhovered(PlayableElementEventArgs args) { }
    public virtual void OnDragStart(PlayableElementEventArgs args) { }
    public virtual void OnDragging(PlayableElementEventArgs args) { }
    public virtual void OnDragEnd(PlayableElementEventArgs args) { }
    public virtual void OnPlayerInput(PlayableElementEventArgs args) { }
    public virtual void OnRotate(PlayableElementEventArgs args) { }
    public virtual void OnFlip(PlayableElementEventArgs args) { }

    #endregion

    #region UTILS
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
  }

  [Serializable]
  /// <summary>
  /// Common settings for all PlayableElement components. Encapsulated in this class for UnityEditor to show it as a foldable section
  /// </summary>
  public class CommonComponentSettings {
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
    [Tooltip("Excludes OnSelect and OnDeselected")]
    public bool ExcludeSelectionEvents = false;
    [Tooltip("Excludes OnHovered and OnUnhovered")]
    public bool ExcludeHoverEvents = false;
    [Tooltip("Excludes BeforeDragStart and DragStart")]
    public bool ExcludeDragStartEvents = false;
    [Tooltip("Excludes OnDragging")]
    public bool ExcludeDuringDraggingEvents = false;
    [Tooltip("Excludes BeforeDragEnd and DragEnd")]
    public bool ExcludeDragEndEvents = false;
    [Tooltip("Excludes BeforeInput and PlayerInput")]
    public bool ExcludeInputControlsEvents = false;

    [Header("Inner Events")]
    [Tooltip("If true, this component will raise a PlayableElementInnerEvent after handling an event, relaying the same event args")]
    public bool TriggersInnerEvent = false;
  }

}