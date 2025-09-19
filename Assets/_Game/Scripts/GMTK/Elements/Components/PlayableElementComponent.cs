using Ameba;
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

      // Listen to PlayableElement events
      if (_playableElement != null) _playableElement.AddComponentListener(this);
    }

    private void RemoveListeners() {
      if (_gameEventChannel == null) return;

      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementSelected, HandleGlobalElementSelected);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementDropped, HandleGlobalElementDropped);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementHovered, HandleGlobalElementHovered);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, HandleGlobalElementUnhovered);

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

    // PlayableElement event handler
    public virtual void OnPlayableElementEvent(PlayableElementEventArgs eventArgs) {
      if (eventArgs.Element != _playableElement) return;

      switch (eventArgs.EventType) {
        case PlayableElementEventType.DragStart:
          HandleDragStart(eventArgs); break;
        case PlayableElementEventType.DragUpdate:
          HandleDragUpdate(eventArgs); break;
        case PlayableElementEventType.DragEnd:
          HandleDragEnd(eventArgs); break;
        case PlayableElementEventType.DropSuccess:
          HandleDropSuccess(eventArgs); break;
        case PlayableElementEventType.DropInvalid:
          HandleDropInvalid(eventArgs); break;
        case PlayableElementEventType.PointerOver:
          HandlePointerOver(eventArgs); break;
        case PlayableElementEventType.PointerOut:
          HandlePointerOut(eventArgs); break;
        case PlayableElementEventType.BecomeActive:
          HandleBecomeActive(eventArgs); break;
        case PlayableElementEventType.BecomeInactive:
          HandleBecomeInactive(eventArgs); break;
        case PlayableElementEventType.RotateCW:
          HandleRotateClockwise(eventArgs); break;
        case PlayableElementEventType.RotateCCW:
          HandleRotateCounterClockwise(eventArgs); break;
        case PlayableElementEventType.FlippedX:
          HandleFlipX(eventArgs); break;
        case PlayableElementEventType.FlippedY:
          HandleFlipY(eventArgs); break;
        case PlayableElementEventType.Selected:
          HandleSelected(eventArgs); break;
        case PlayableElementEventType.Deselected:
          HandleDeselected(eventArgs); break;
      }
    }

    // Abstract methods for legacy compatibility
    protected abstract void HandleElementSelected(GridSnappableEventArgs evt);
    protected abstract void HandleElementDropped(GridSnappableEventArgs evt);
    protected abstract void HandleElementHovered(GridSnappableEventArgs evt);
    protected abstract void HandleElementUnhovered(GridSnappableEventArgs evt);

    // Virtual methods for PlayableElement events - can be overridden as needed
    protected virtual void HandleDragStart(PlayableElementEventArgs evt) { }
    protected virtual void HandleDragUpdate(PlayableElementEventArgs evt) { }
    protected virtual void HandleDragEnd(PlayableElementEventArgs evt) { }
    protected virtual void HandleDropSuccess(PlayableElementEventArgs evt) { }
    protected virtual void HandleDropInvalid(PlayableElementEventArgs evt) { }
    protected virtual void HandlePointerOver(PlayableElementEventArgs evt) { }
    protected virtual void HandlePointerOut(PlayableElementEventArgs evt) { }
    protected virtual void HandleBecomeActive(PlayableElementEventArgs evt) { }
    protected virtual void HandleBecomeInactive(PlayableElementEventArgs evt) { }
    protected virtual void HandleRotateClockwise(PlayableElementEventArgs evt) { }
    protected virtual void HandleRotateCounterClockwise(PlayableElementEventArgs evt) { }
    protected virtual void HandleFlipX(PlayableElementEventArgs evt) { }
    protected virtual void HandleFlipY(PlayableElementEventArgs evt) { }
    protected virtual void HandleSelected(PlayableElementEventArgs evt) { }
    protected virtual void HandleDeselected(PlayableElementEventArgs evt) { }

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
  }
}