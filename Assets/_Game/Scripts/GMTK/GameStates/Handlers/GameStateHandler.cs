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

    public virtual void HandleStateChange(StateMachineEventArg<GameStates> eventArg) {
      HandleFromState(eventArg.FromState);
      HandleToState(eventArg.ToState);
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