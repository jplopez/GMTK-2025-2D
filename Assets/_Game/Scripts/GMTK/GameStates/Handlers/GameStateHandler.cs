using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Ameba;


namespace GMTK {

  [AddComponentMenu("GMTK/GameStates/Handlers/GameStateHandler")]
  public class GameStateHandler : BaseGameStateHandler {
    
    [Header("Default Transitions")]
    public bool HandleAllTransitions = false;
    
    [MMCondition("HandleAllTransitions", true)]
    public GameEventType DefaultEvent = GameEventType.GameStarted;
    
    [MMCondition("HandleAllTransitions", true)]
    public MMF_Player DefaultFeedback;

    [Header("GameState Transitions")]
    public List<GameStateTransitionConfig> Transitions = new();

    // Simple lookup for O(1) performance
    private HashSet<(GameStates from, GameStates to)> _supportedTransitions;
    private GameStateConfig _allTransitionsConfig;

    protected override void Init() {
        base.Init();
        HandlerName = nameof(GameStateHandler);
        Priority = 0;
        
        BuildTransitionCache();
        
        if (HandleAllTransitions) {
            _allTransitionsConfig = new() {
                TriggersEvent = true,
                GameStateEvent = DefaultEvent,
                GameStateFeedback = DefaultFeedback
            };
        }
    }

    private void BuildTransitionCache() {
        _supportedTransitions = new HashSet<(GameStates, GameStates)>();
        
        foreach (var transition in Transitions) {
            if (transition.AnySourceState && transition.AnyDestinationState) {
                // Add all possible combinations
                foreach (GameStates from in System.Enum.GetValues(typeof(GameStates))) {
                    foreach (GameStates to in System.Enum.GetValues(typeof(GameStates))) {
                        _supportedTransitions.Add((from, to));
                    }
                }
            } else if (transition.AnySourceState) {
                // Any source to specific destination
                foreach (GameStates from in System.Enum.GetValues(typeof(GameStates))) {
                    _supportedTransitions.Add((from, transition.DestinationState));
                }
            } else if (transition.AnyDestinationState) {
                // Specific source to any destination
                foreach (GameStates to in System.Enum.GetValues(typeof(GameStates))) {
                    _supportedTransitions.Add((transition.SourceState, to));
                }
            } else {
                // Specific source to specific destination
                _supportedTransitions.Add((transition.SourceState, transition.DestinationState));
            }
        }
    }

    public override void NotifyStateChange(StateMachineEventArg<GameStates> eventArg) {
        if (!IsEnabled) return;
        
        if (TransitionSupported(eventArg.FromState, eventArg.ToState)) {
            // Use base class pattern - set the args and let Update handle it
            _currentArgs = eventArg;
            _inTransition = true;
            this.LogDebug($"Accepted state change: {eventArg.FromState} to {eventArg.ToState}");
        } else {
            this.LogDebug($"Unsupported state change: {eventArg.FromState} to {eventArg.ToState} - ignoring");
        }
    }

    private bool TransitionSupported(GameStates from, GameStates to) {
        return HandleAllTransitions || _supportedTransitions.Contains((from, to));
    }

    // Override the base class's transition handling
    protected override void HandleFromState(GameStates state) {
        if (HandleAllTransitions) {
            ProcessGameStateConfig(_allTransitionsConfig);
        } else {
            ProcessMatchingTransitionsForState(state, isFromState: true);
        }
    }

    protected override void HandleToState(GameStates state) {
        if (HandleAllTransitions) {
            ProcessGameStateConfig(_allTransitionsConfig);
        } else {
            ProcessMatchingTransitionsForState(state, isFromState: false);
        }
    }

    private void ProcessMatchingTransitionsForState(GameStates state, bool isFromState) {
        foreach (var transition in GetMatchingTransitions(_currentArgs)) {
            if (isFromState) {
                ProcessGameStateConfig(transition.SourceStateConfig);
            } else {
                ProcessGameStateConfig(transition.DestinationStateConfig);
            }
        }
    }

    private IEnumerable<GameStateTransitionConfig> GetMatchingTransitions(StateMachineEventArg<GameStates> eventArg) {
        foreach (var transition in Transitions) {
            if ((transition.AnySourceState || transition.SourceState == eventArg.FromState) &&
                (transition.AnyDestinationState || transition.DestinationState == eventArg.ToState)) {
                yield return transition;
            }
        }
    }

    private void ProcessGameStateConfig(GameStateConfig config) {
        if (config == null) return;
        
        try {
            if (config.TriggersEvent && _eventsChannel != null) {
                _eventsChannel.Raise(config.GameStateEvent);
            }
            
            if (config.GameStateFeedback != null) {
                config.GameStateFeedback.PlayFeedbacks();
            }
        } catch (System.Exception ex) {
            this.LogError($"Failed to process state config: {ex.Message}");
#if UNITY_EDITOR
            this.LogException(ex);
#endif
        }
    }
  }

  [Serializable]
  public class GameStateTransitionConfig {

    [Header("Source State")]
    public GameStateConfig SourceStateConfig;

    [Header("Destination State")]
    public GameStateConfig DestinationStateConfig;

    public bool AnySourceState => SourceStateConfig.AllGameStates;
    public bool AnyDestinationState => DestinationStateConfig.AllGameStates;

    public GameStates SourceState => SourceStateConfig.State;
    public GameStates DestinationState => DestinationStateConfig.State;

    public GameStateTransitionConfig(bool anySource = true, bool anyDestination = true) {
      SourceStateConfig = new(anySource);
      DestinationStateConfig = new(anyDestination);
    }

    public GameStateTransitionConfig(GameStates source, GameStates destination) {
      SourceStateConfig = new(source);
      DestinationStateConfig = new(destination);
    }

    public override string ToString() {
      string source = SourceStateConfig.AllGameStates ? "Any" : SourceStateConfig.State.ToString();
      string destination = DestinationStateConfig.AllGameStates ? "Any" : DestinationStateConfig.State.ToString();
      return $"{source} -> {destination}";
    }
  }

  [Serializable]
  public class GameStateConfig {

    [Tooltip("If true, this config applies to any state")]
    public bool AllGameStates;
    [Tooltip("The specific game state this config applies to. Ignored if AllGameStates is true")]
    public GameStates State;

    public bool TriggersEvent;
    [MMCondition("TriggersEvent", true)]
    public GameEventType GameStateEvent;

    public MMF_Player GameStateFeedback;


    public GameStateConfig(bool anyState=true) {
      AllGameStates = anyState;
      State = default;
      TriggersEvent = false;
      GameStateEvent = GameEventType.GameStarted;
      GameStateFeedback = null;
    }

    public GameStateConfig(GameStates levelState) {
      AllGameStates = false;
      State = levelState;
      TriggersEvent = false;
      GameStateEvent = GameEventType.GameStarted;
      GameStateFeedback = null;
    }

    public override string ToString() {
      string str = AllGameStates ? "Any" : State.ToString()
       + (TriggersEvent ? $" (Event: {GameStateEvent})" : "")
       + (GameStateFeedback != null ? $" (Feedback: {GameStateFeedback.name})" : "");
      return str;
    }
  }

}