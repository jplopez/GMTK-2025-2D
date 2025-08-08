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

    [Tooltip("The starting state for every instance of the State machine")]
    public T StartingState = default;

    [Tooltip("Current State. Read Only")]
    public T Current => _currentState; 

    [Tooltip("If true, there are no restriction on state transitions")]
    public bool NoRestrictions = false;

    protected Dictionary<T, List<T>> _transitions = new();

    [SerializeField]
    protected List<Transition<T>> _serializedTransitions = new();
    
    protected T _currentState = default;


    public UnityEvent<StateMachineEventArg<T>> OnStateChanged;


    protected virtual void OnEnable() {
      Reset();
      _transitions.Clear();

      //serialized transitions is accessible from Editor
      //on enable we populate the dictionary for better lookup
      foreach (var entry in _serializedTransitions) {
        entry.To.ForEach( to => { AddTransition(entry.From, to); });
      }
    }

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

    public virtual void ClearState(T state) {
      if (_transitions.TryGetValue(state, out List<T> validTransitions)) {
        validTransitions.Clear();
      }
#if UNITY_EDITOR
      else {
        Debug.Log($"There are no transitions for state {state}");
      }
#endif
    }

    public virtual void ClearAll() => _transitions.Clear();

    public virtual bool TestTransition(T fromState, T toState) 
      => NoRestrictions || (_transitions.TryGetValue(fromState, out List<T> validTransitions) && validTransitions.Contains(toState));

    public bool ChangeState(T newState) {
      if(TestTransition(_currentState, newState)) {
        var oldState = _currentState;
        _currentState = newState;
        OnStateChanged?.Invoke(new StateMachineEventArg<T>(oldState, newState));
        return true;
      }
#if UNITY_EDITOR
      Debug.LogWarning($"Invalid transition from {_currentState} to {newState}");
#endif
      return false;
    }

    public void Reset() => _currentState = StartingState;


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