using Ameba;
using System;
using System.Collections;
using UnityEngine;

namespace GMTK {

  [RequireComponent(typeof(GridSnappable))]
  public abstract class SnappableComponent : MonoBehaviour {

    [Header("Snappable Component")]
    [Tooltip("Main switch to turn on/off a Component. Useful for debugging")]
    public bool IsActive = true;

    [Header("Delay Run")]
    [Tooltip("if true, this component will run on a coroutine, executing the logic on OnDelayedUpdateRun ")]
    public bool DelayRun = false;
    [Tooltip("Delay in seconds before executing the OnDelayedUpdateRun logic")]
    public float InitialDelay = 0.1f;


    protected GridSnappable _snappable;
    protected LevelGrid _levelGrid;
    protected GameEventChannel _gameEventChannel;

    protected bool isInitialized = false;
    private Coroutine _delayedUpdateCoroutine;

    private void OnValidate() => InitDependencies();

    private void Awake() {
      TryInitialize();
    }

    private void OnDestroy() => RemoveInputListeners();

    public void TryInitialize() {
      if (isInitialized) return;
      InitDependencies();
      AddInputListeners();
      Initialize();
      isInitialized = true;
    }

    private void InitDependencies() {
      _snappable = (_snappable == null) ? gameObject.GetComponent<GridSnappable>() : _snappable;
      if (_snappable == null) Debug.LogWarning($"SnappableComponent {name} is missing GridSnappable. This component will not function");

      _levelGrid = (_levelGrid == null) ? FindAnyObjectByType<LevelGrid>() : _levelGrid;

      _gameEventChannel = (_gameEventChannel == null) ? Resources.Load<GameEventChannel>("GameEventChannel") : _gameEventChannel;
      if (_gameEventChannel == null) Debug.LogWarning($"SnappableComponent {name} can't find GameEventChannel. This component won't listen or trigger game events");
    }

    private void AddInputListeners() {
      if (_gameEventChannel == null) return;
      
      // Change these to explicitly specify GridSnappableEventArgs
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementSelected, EventArgsHandlerAdapter);
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementDropped, EventArgsHandlerAdapter);
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementHovered, EventArgsHandlerAdapter);
      _gameEventChannel.AddListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, EventArgsHandlerAdapter);

      if (_snappable != null) _snappable.AddComponentListener(this);
    }

    private void RemoveInputListeners() {
      if (_gameEventChannel == null) return;
      
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementSelected, EventArgsHandlerAdapter);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementDropped, EventArgsHandlerAdapter);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementHovered, EventArgsHandlerAdapter);
      _gameEventChannel.RemoveListener<GridSnappableEventArgs>(GameEventType.ElementUnhovered, EventArgsHandlerAdapter);

      if (_snappable != null) _snappable.RemoveComponentListener(this);
    }

    // Update the method signature to be type-specific
    private void EventArgsHandlerAdapter(GridSnappableEventArgs eventArgs) {
      switch (eventArgs.GameEvent) {
        case GameEventType.ElementSelected:
          HandleElementSelected(eventArgs); break;
        case GameEventType.ElementDropped:
          HandleElementDropped(eventArgs); break;
        case GameEventType.ElementUnhovered:
          HandleElementUnhovered(eventArgs); break;
        case GameEventType.ElementHovered:
          HandleElementHovered(eventArgs); break;
      }
    }

    protected abstract void HandleElementSelected(GridSnappableEventArgs evt);
    protected abstract void HandleElementDropped(GridSnappableEventArgs evt);
    protected abstract void HandleElementHovered(GridSnappableEventArgs evt);
    protected abstract void HandleElementUnhovered(GridSnappableEventArgs evt);


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
    public virtual void OnSnappableEvent(GridSnappableEventArgs eventArgs) { }
  }
}