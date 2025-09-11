using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ameba {

  /// <summary>
  /// Simple StateMachine implementation based on Enum. 
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class StateMachine<T> : ScriptableObject where T : Enum {

    [Header("StateMachine Settings")]
    [Tooltip("The starting state for every instance of the State machine")]
    public T StartingState = default;
    [Tooltip("Current State. Read Only")]
    public T Current => _currentState;
    [Tooltip("If true, there are no restriction on state transitions")]
    public bool NoRestrictions = false;

    [Header("Transitions")]
    [Tooltip("Add here the valid transitions between states")]
    [SerializeField]
    protected List<Transition<T>> _serializedTransitions = new();

    protected Dictionary<T, List<T>> _transitions = new();
    protected T _currentState = default;
    protected UnityEvent<StateMachineEventArg<T>> OnStateChanged;

    protected bool _inTransition = false;
    protected Queue<StateMachineEventArg<T>> _queuedChanges = new();

    #region Object Lifecycle

    /// <summary>
    /// Resets to StartingState, and populates from clean all transactions 
    /// found in the UnityEditor 
    /// </summary>
    protected virtual void OnEnable() {
      ResetToStartingState();
      ClearAllTransitions();

      //serialized transitions is accessible from Editor
      //on enable we populate the dictionary for better lookup
      foreach (var entry in _serializedTransitions) {
        entry.To.ForEach(to => { AddTransition(entry.From, to); });
      }
    }

    /// <summary>
    /// Clears all Transactions from the StateMachine
    /// </summary>
    protected virtual void OnDisable() {
      ClearAllTransitions();
    }

    /// <summary>
    /// Sets current state to StartingState
    /// </summary>
    public void ResetToStartingState() => _currentState = StartingState;

    #endregion

    #region Listeners

    public void AddListener(Action<StateMachineEventArg<T>> action) {
      if (action == null) return;
      OnStateChanged.AddListener(new UnityAction<StateMachineEventArg<T>>(action));
    }

    public void RemoveListener(Action<StateMachineEventArg<T>> action) {
      if (action == null) return;
      OnStateChanged.RemoveListener(new UnityAction<StateMachineEventArg<T>>(action));
    }

    public void RemoveAllListeners() => OnStateChanged?.RemoveAllListeners();

    #endregion

    #region Transitions

    public virtual void AddTransition(T fromState, T toState) {
      if (!_transitions.TryGetValue(fromState, out List<T> validTransitions)) {
        validTransitions = new();
        _transitions.Add(fromState, validTransitions);
      }
      if (!validTransitions.Contains(toState))
        validTransitions.Add(toState);

      SyncAddedTransition(fromState, toState);
    }

    public virtual void RemoveTransition(T fromState, T toState) {
      if (_transitions.TryGetValue(fromState, out List<T> validTransitions)) {
        validTransitions.Remove(toState);

        SyncRemovedTransition(fromState, toState);
      }
#if UNITY_EDITOR
      else {
        Debug.Log($"There are no transitions for state {fromState}");
      }
#endif
    }

    public virtual void ClearStateTransitions(T state) {
      if (_transitions.TryGetValue(state, out List<T> validTransitions)) {
        validTransitions.Clear();
      }
#if UNITY_EDITOR
      else {
        Debug.Log($"There are no transitions for state {state}");
      }
#endif
    }

    public virtual void ClearAllTransitions() {
      _transitions.Clear();
      _serializedTransitions.Clear();
    }

    public virtual bool TestTransition(T fromState, T toState)
      => NoRestrictions || (_transitions.TryGetValue(fromState, out List<T> validTransitions) && validTransitions.Contains(toState));

    public IReadOnlyList<T> GetValidTransitions(T state)
=> _transitions.TryGetValue(state, out var list) ? list : Array.Empty<T>();

    public IEnumerable<T> GetStatesWithTransitions() => _transitions.Keys;

    protected virtual void SyncAddedTransition(T fromState, T toState) {
      var entry = _serializedTransitions.Find(t => EqualityComparer<T>.Default.Equals(t.From, fromState));
      if (entry == null) {
        entry = new Transition<T> { From = fromState };
        _serializedTransitions.Add(entry);
      }
      if (!entry.To.Contains(toState))
        entry.To.Add(toState);
    }

    protected virtual void SyncRemovedTransition(T fromState, T toState) {
      var entry = _serializedTransitions.Find(t => EqualityComparer<T>.Default.Equals(t.From, fromState));
      if (entry == null) {
        entry = new Transition<T> { From = fromState };
        _serializedTransitions.Add(entry);
      }
      if (entry.To.Contains(toState))
        entry.To.Remove(toState);
    }


    #endregion

    #region Change State

    //TODO : consider making this private and force usage of a queue system (define queue size, delay between changes, retry attempts, etc)
    //TODO : remember to include methods to empty queue, pause queue processing, resume queue processing, etc
    //TODO : consider adding a priority system to the queued changes (e.g. ChangeState(T newState, int priority = 0) )
    //TODO : consider adding an event for invalid transition attempts (e.g. OnInvalidTransitionAttempt(T fromState, T toState) )
    //TODO : consider making this async and awaitable (Task<bool> ChangeStateAsync(T newState) )
    //TODO : consider adding an optional parameter to force the change even if invalid transition (e.g. ChangeState(T newState, bool force = false), or put the force flag in the EventArgs )

    public virtual bool ChangeState(T newState) {
      if (TestTransition(_currentState, newState)) {
        var oldState = _currentState;
        _currentState = newState;
        var args = new StateMachineEventArg<T>(oldState, newState);
        _queuedChanges.Enqueue(args);
        _inTransition = true;
        OnStateChanged?.Invoke(args);
        return true;
      }
#if UNITY_EDITOR
      Debug.LogWarning($"Invalid transition from {_currentState} to {newState}");
#endif
      return false;
    }

    #endregion


  }

  [Serializable]
  public class StateMachineEventArg<T> : EventArgs where T : Enum {
    public T FromState { get; }
    public T ToState { get; }
    public StateMachineEventArg(T fromState, T toState) {
      FromState = fromState;
      ToState = toState;
    }
  }

  [Serializable]
  public class Transition<T> where T : Enum {
    public T From;
    public List<T> To = new();
  }

}