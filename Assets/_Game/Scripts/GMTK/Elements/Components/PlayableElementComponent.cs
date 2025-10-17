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

    protected PlayableElement _playableElement;
    protected LevelGrid _levelGrid;
    protected GameEventChannel _gameEventChannel;

    protected bool isInitialized = false;
    private Coroutine _delayedUpdateCoroutine;

    private void OnValidate() => InitDependencies();

    private void Awake() {
      TryInitialize();
    }

    private void OnDestroy() => RemoveListeners();

    public void TryInitialize() {
      if (isInitialized) return;
      InitDependencies();
      AddListeners();
      Initialize();
      isInitialized = true;
    }

    private void InitDependencies() {
      _playableElement = (_playableElement == null) ? gameObject.GetComponent<PlayableElement>() : _playableElement;
      if (_playableElement == null) Debug.LogWarning($"PlayableElementComponent {name} is missing PlayableElement. This component will not function");

      _levelGrid = (_levelGrid == null) ? FindAnyObjectByType<LevelGrid>() : _levelGrid;

      _gameEventChannel = (_gameEventChannel == null) ? ServiceLocator.Get<GameEventChannel>() : _gameEventChannel;
      if (_gameEventChannel == null) Debug.LogWarning($"PlayableElementComponent {name} can't find GameEventChannel. This component won't listen or trigger game events");
    }

    private void AddListeners() {
      if (_gameEventChannel == null) return;

      // Listen to global game events
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementSelected, HandleGlobalElementSelected);
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementDropped, HandleGlobalElementDropped);
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementHovered, HandleGlobalElementHovered);
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, HandleGlobalElementUnhovered);

      // Listen to PlayableElement events through the GameEventChannel
      _gameEventChannel.AddListener<PlayableElementEventArgs>(GameEventType.PlayableElementEvent, HandlePlayableElementEvent);
      
      // Listen to direct PlayableElement events
      if (_playableElement != null) _playableElement.AddComponentListener(this);
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
    protected virtual void HandlePlayableElementEvent(PlayableElementEventArgs args) {
      // Only handle events for our own PlayableElement
      if (args.Element != _playableElement) return;

      // Use reflection to look for a method named as "On" + PlayableElementEventType name in args
      // For example OnDragStart for PlayableElementEventType.DragStart, that receives a PlayableElementEventArgs argument
      string methodName = "On" + args.EventType.ToString();
      System.Reflection.MethodInfo method = GetType().GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
      
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

    private void RemoveListeners() {
      if (_gameEventChannel == null) return;

      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementSelected, HandleGlobalElementSelected);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementDropped, HandleGlobalElementDropped);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementHovered, HandleGlobalElementHovered);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, HandleGlobalElementUnhovered);

      _gameEventChannel.RemoveListener<PlayableElementEventArgs>(GameEventType.PlayableElementEvent, HandlePlayableElementEvent);

      if (_playableElement != null) _playableElement.RemoveComponentListener(this);
    }

    // Bridge methods for backward compatibility with GridSnappable events
    private void HandleGlobalElementSelected(GridSnappableEventArgs evt) {
      // Convert to PlayableElement events if this component's element was selected
      // This is for backward compatibility during transition
      HandleElementSelected(evt);
    }

    private void HandleGlobalElementDropped(GridSnappableEventArgs evt) {
      HandleElementDropped(evt);
    }

    private void HandleGlobalElementHovered(GridSnappableEventArgs evt) {
      HandleElementHovered(evt);
    }

    private void HandleGlobalElementUnhovered(GridSnappableEventArgs evt) {
      HandleElementUnhovered(evt);
    }

    // PlayableElement event handler - now broadcasts to GameEventChannel and uses reflection
    public virtual void OnPlayableElementEvent(PlayableElementEventArgs eventArgs) {
      if (eventArgs.Element != _playableElement) return;

      // Broadcast the event through GameEventChannel for other components to listen
      _gameEventChannel?.Raise(GameEventType.PlayableElementEvent, eventArgs);

      // Handle the event locally using reflection
      HandlePlayableElementEvent(eventArgs);
    }

    // Abstract methods for legacy compatibility - these still need to be implemented
    protected abstract void HandleElementSelected(GridSnappableEventArgs evt);
    protected abstract void HandleElementDropped(GridSnappableEventArgs evt);
    protected abstract void HandleElementHovered(GridSnappableEventArgs evt);
    protected abstract void HandleElementUnhovered(GridSnappableEventArgs evt);

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
    protected virtual void PlayFeedback(MMF_Player feedback, bool playInReverse=false) {
      if (feedback != null && feedback.gameObject.activeInHierarchy) {
        if (playInReverse) {
          feedback.PlayFeedbacksInReverse();
        } else {
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
}