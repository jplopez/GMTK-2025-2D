using System;
using Ameba;

/// This file contains several general purpose interfaces in use for this game
namespace GMTK {

  /// <summary>
  /// The Broadcaster interface is for objects coordinating multiple Events. For example a specific sequence.
  /// This interface offers most methods with and without the EventsChannel parameter, depending if you're using
  /// a single or multiple EventsChannel instances.
  /// For raising delayed events (over coroutines), <seealso cref="DelayedEventsChannelBroadcaster"/>
  /// </summary>
  /// <typeparam name="Tenum"></typeparam>
  public interface IEventsChannelBroadcaster<Tenum> where Tenum : Enum {

    public EventChannel<Tenum> Channel { get; set; }

    public void AddAllListeners();

    public void RemoveAllListeners();

    //whether this Broadcaster has an EventChannel and its listeners are added to it
    public bool IsConnected();

    //Raise a single event
    public void RaiseEvent(Tenum eventType);

    //Raise all events from this broadcaster
    public void RaiseAllEvents();

    //Finite events raised simultaneously
    public void RaiseEvents(Tenum[] eventType);
    public void RaiseEvents(Tenum eventType1, Tenum eventType2);
    public void RaiseEvents(Tenum eventType1, Tenum eventType2, Tenum eventType3);

  }

  /// <summary>
  /// Interfaces for object who can trigger a change in the GameState.
  /// </summary>
  /// <typeparam name="Tenum"></typeparam>
  public interface IGameStatesChanger<Tenum> where Tenum : Enum {
    public void ChangeState(StateMachine<Tenum> stateMachine);
    public void ChangeState();
  }

  /// <summary>
  /// This interface is for object who can trigger a score. 
  /// Is intended to be used locally, to keep track of a local or temporary score.
  /// For global scores, see <seealso cref="ScoreGateKeeper"/>
  /// </summary>
  public interface IScorer {

    //The default value this scorer adds to the game score
    public int DefaultScore { get; set; }

    public bool HasScored();

    public void Score();

    public void Score(int score);

    //overrides the scored amount from this scorer to 'score'
    public void SetScore(int score);

    //returns the amount scored from this scorer
    public int GetScore();
  }
}