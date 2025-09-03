using System;
using Ameba;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// <para>
  /// Base implementation of <c>IGameStateHandler</c> for <c>GameStates</c>.<br/>
  /// You should extend this class and override the methods for the GameStates you need.<br/>
  /// There is a method for each state, and each state has a From and To method.
  /// </para> 
  /// <para>
  /// For example, the <c>'Playing'</c> state has the methods <c>FromPlaying</c> and <c>ToPlaying</c>:<br/>
  /// - <c>FromPlaying</c> should be overriden if you want to handle when the game is coming out of the Playing state<br/>
  /// - <c>ToPlaying</c> should be overriden if you want o handle when the game is entering the Playing state. 
  /// </para>
  /// </summary>
  public abstract class GameStateHandler : MonoBehaviour, IGameStateHandler<GameStates> {

    public bool IsEnabled { get; set; } = true;
    public string HandlerName { get; set; } = nameof(GameStateHandler);
    public int Priority { get; set; } = 0;

    protected bool _isInitialized = false;
    protected bool _inTransition = false;
    protected StateMachineEventArg<GameStates> _currentArgs;

    protected GameEventChannel _eventsChannel;
    private void Awake() {
      _isInitialized = false;
     
      if (_eventsChannel == null) {
        _eventsChannel = Services.Get<GameEventChannel>();
      }
      Init();
      _isInitialized = true;
    }

    /// <summary>
    /// Override this method if you need to add specific logic to the Initialization of this handler.<br/>
    /// Init is called once Resources are loaded, so is guaranteed you'll have SOs available.
    /// </summary>
    protected virtual void Init() { }

    private void Update() {
      if(!_isInitialized) Init();

      if(_inTransition) {
        //If Try method is successful we want inTransition to be false, signaling the transition was finalized successfully
        //If Try fails, we keep inTransition true, and try again in the next frame
        _inTransition = !TryGameStateTransition();
      }
    }

    private bool TryGameStateTransition() {
      if (_currentArgs == null) return false;
      return TryHandleFromState(_currentArgs) && TryHandleToState(_currentArgs);
    }

    public virtual void HandleStateChange(StateMachineEventArg<GameStates> eventArg) {
      if (!_isInitialized) Init();
      if(eventArg != null) {
        _currentArgs = eventArg;
        //This flag tells the Update method to attempt the game state transition
        _inTransition = true;
      } else {
        Debug.LogWarning($"GameStateHandler: '{name}': Can't resolve state change request because StateMachineEventArg is null");
      }
    }

    private bool TryHandleFromState(StateMachineEventArg<GameStates> eventArg) {
     try {
        HandleFromState(eventArg.FromState);
        return true;
      } catch (Exception ex) {
        Debug.LogError($"GameStateHandler: '{name}' could not transition from state {eventArg.FromState}: {ex.Message}");
#if UNITY_EDITOR
        Debug.LogException(ex);
#endif
      }
      return false;
    }

    private bool TryHandleToState(StateMachineEventArg<GameStates> eventArg) {
      try {
        HandleToState(eventArg.ToState);
        return true;
      }
      catch (Exception ex) {
        Debug.LogError($"GameStateHandler: '{name}' could not transition to state {eventArg.ToState}: {ex.Message}");
#if UNITY_EDITOR
        Debug.LogException(ex);
#endif
      }
      return false;
    }

    /// <summary>
    /// Internal handler for the FromState inside the StateMachineEventArg
    /// </summary>
    /// <exception cref="ArgumentException">If state is not recognized or supported</exception>
    protected virtual void HandleFromState(GameStates state) {
      switch (state) {
        case GameStates.Start:
          FromStart(); break;
          case GameStates.Options:
          FromOptions(); break;
          case GameStates.Pause:
          FromPause(); break;
        case GameStates.Preparation:
          FromPreparation(); break;
        case GameStates.Playing:
          FromPlaying(); break;
        case GameStates.Reset:
          FromReset(); break;
        case GameStates.LevelComplete:
          FromLevelComplete(); break;
        case GameStates.Gameover:
          FromGameOver(); break;
        default:
          throw new ArgumentException($"The GameState {state} sent as FromState is not supported.");
      }
    }

    protected virtual void FromStart() { }
    protected virtual void FromOptions() { }
    protected virtual void FromPause() { }
    protected virtual void FromPreparation() { }
    protected virtual void FromPlaying() { }
    protected virtual void FromReset() { }
    protected virtual void FromLevelComplete() { }
    protected virtual void FromGameOver() { }


    /// <summary>
    /// Internal handler for the ToState inside the StateMachineEventArg
    /// </summary>
    /// <exception cref="ArgumentException">If state is not recognized or supported</exception>
    protected virtual void HandleToState(GameStates state) {
      switch (state) {
        case GameStates.Start:
          ToStart(); break;
        case GameStates.Options:
          ToOptions(); break;
        case GameStates.Pause:
          ToPause(); break;
        case GameStates.Preparation:
          ToPreparation(); break;
        case GameStates.Playing:
          ToPlaying(); break;
        case GameStates.Reset:
          ToReset(); break;
        case GameStates.LevelComplete:
          ToLevelComplete(); break;
        case GameStates.Gameover:
          ToGameOver(); break;
        default:
          throw new ArgumentException($"The GameState {state} setn as ToState is not supported.");
      }
    }

    protected virtual void ToStart() { }
    protected virtual void ToOptions() { }
    protected virtual void ToPause() { }
    protected virtual void ToPreparation() { }
    protected virtual void ToPlaying() { }
    protected virtual void ToReset() { }
    protected virtual void ToLevelComplete() { }
    protected virtual void ToGameOver() { }
  }
}