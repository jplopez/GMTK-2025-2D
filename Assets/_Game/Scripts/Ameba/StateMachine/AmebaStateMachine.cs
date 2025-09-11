using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Ameba {

  /// <summary>
  /// <see cref="AmebaStateMachine"/> is a ScriptableObject that implements a finite state machine (FSM) pattern.<br/>
  /// Uses <see cref="GameState"/> to represent states, and supports defining valid transitions between states.<br/>
  /// </summary>
  [CreateAssetMenu(fileName = "AmebaStateMachine", menuName = "Ameba/State Machine", order = 1)]
  public class AmebaStateMachine : ScriptableObject {

    [Tooltip("The starting state for every instance of the State machine")]
    public string StartingState;
    [Tooltip("Current State. Read Only")]
    public string Current;
    [Tooltip("If true, there are no restriction on state transitions")]
    public bool NoRestrictions = false;

    [Tooltip("Add here the valid transitions between states")]
    [SerializeField]
    protected List<StateTransition<GameState>> _serializedTransitions = new();

    protected Dictionary<GameState, List<GameState>> _transitions = new();
    protected GameState _startingState = new("Start");
    protected GameState _currentState = GameState.NullState;
    protected UnityEvent<StateChangePayload<GameState>> OnStateChanged;

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
        entry.Targets.ForEach(to => { AddTransition(entry.From, to); });
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
    public void ResetToStartingState() => _currentState = new GameState(StartingState);


    #endregion

    #region Listeners

    public void AddListener(Action<StateChangePayload<GameState>> action) {
      if (action == null) return;
      OnStateChanged.AddListener(new UnityAction<StateChangePayload<GameState>>(action));
    }

    public void RemoveListener(Action<StateChangePayload<GameState>> action) {
      if (action == null) return;
      OnStateChanged.RemoveListener(new UnityAction<StateChangePayload<GameState>>(action));
    }

    public void RemoveAllListeners() => OnStateChanged?.RemoveAllListeners();

    #endregion

    #region Transitions

    public virtual void AddTransition(GameState fromState, GameState targetState) {

      fromState = fromState ?? default;
      targetState = targetState ?? default;

      if (!_transitions.TryGetValue(fromState, out List<GameState> validTransitions)) {
        validTransitions = new();
        _transitions.Add(fromState, validTransitions);
      }
      if (!validTransitions.Contains(targetState))
        validTransitions.Add(targetState);

      SyncAddedTransition(fromState, targetState);
    }

    public virtual void RemoveTransition(GameState fromState, GameState targetState) {

      fromState = fromState ?? default;
      targetState = targetState ?? default;

      if (_transitions.TryGetValue(fromState, out List<GameState> validTransitions)) {
        validTransitions.Remove(targetState);
        SyncRemovedTransition(fromState, targetState);
      }
#if UNITY_EDITOR
      else {
        Debug.Log($"There are no transitions for state {fromState}");
      }
#endif
    }

    public virtual void ClearStateTransitions(GameState state) {
      if (_transitions.TryGetValue(state, out List<GameState> validTransitions)) {
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

    public virtual bool TestTransition(GameState fromState, GameState toState)
      => NoRestrictions || (_transitions.TryGetValue(fromState, out List<GameState> validTransitions) && validTransitions.Contains(toState));

    public IReadOnlyList<GameState> GetValidTransitions(GameState state)
      => _transitions.TryGetValue(state, out var list) ? list : Array.Empty<GameState>();

    public IEnumerable<GameState> GetStatesWithTransitions() => _transitions.Keys;

    protected virtual void SyncAddedTransition(GameState fromState, GameState toState) {
      var entry = _serializedTransitions.Find(t => EqualityComparer<GameState>.Default.Equals(t.From, fromState));
      if (entry == null) {
        entry = new StateTransition<GameState>(fromState);
        _serializedTransitions.Add(entry);
      }
      if (!entry.Targets.Contains(toState))
        entry.Targets.Add(toState);
    }

    protected virtual void SyncRemovedTransition(GameState fromState, GameState targetState) {
      var entry = _serializedTransitions.Find(t => EqualityComparer<GameState>.Default.Equals(t.From, fromState));
      if (!StateTransition<GameState>.IsNullOrEmpty(entry)) {
        entry = new StateTransition<GameState>(fromState);
        _serializedTransitions.Add(entry);
      }
      if (entry.Targets.Contains(targetState))
        entry.Targets.Remove(targetState);
    }

    #endregion

    #region Change State

    public virtual bool ChangeState(GameState newState) {

      if (EqualityComparer<GameState>.Default.Equals(_currentState, newState))
        return true; //no change
      if (EqualityComparer<GameState>.Default.Equals(newState, default))
        return false; //cannot change to null/default state

      if (TestTransition(_currentState, newState)) {
        var oldState = _currentState;
        _currentState = newState;
        OnStateChanged?.Invoke(new StateChangePayload<GameState>(oldState, newState));
        return true;
      }
#if UNITY_EDITOR
      Debug.LogWarning($"Invalid transition from {_currentState} to {newState}");
#endif
      return false;
    }

    #endregion


  }

}