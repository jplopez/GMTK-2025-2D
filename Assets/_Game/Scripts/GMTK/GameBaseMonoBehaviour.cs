using System.Collections.Generic;
using Ameba;
using UnityEngine;

namespace GMTK {

  public class GameBaseMonoBehaviour : MonoBehaviour {

    protected GameContext _context;
    protected GameEventChannel _eventsChannel;
    protected LevelSequence _levelSequence;
    protected ScoreGateKeeper _marbleScore;

    private List<GameEventType> events = new();

    protected virtual void OnEnable() {
      if (_context == null) _context = Game.Context;
      if (_context != null) {
        _eventsChannel = _context.EventsChannel;
        _levelSequence = _context.LevelSequence;
        _marbleScore = _context.MarbleScoreKeeper;
      }
      events.Clear();
    }

    protected virtual void AddEventListener(GameEventType eventType) {
      events.Add(eventType);
      //TODO implement reflection to find public method called the same as the eventType
     // _eventsChannel.AddListener(eventType,)
    }

    protected virtual void RemoveEventListener(GameEventType eventType) {
      events.Remove(eventType);
      //TODO remove from _eventsChannel;
    }

    protected virtual void AddEventListeners(GameEventType[] events) {

      foreach (GameEventType eventType in events) {
        AddEventListener(eventType);
      }
    }

    protected virtual void RemoveAllListeners() => events.ForEach(e => RemoveEventListener(e));

    protected virtual void RaiseGameStateChange(GameStates newState) {
      if (_context != null && _context.StateMachine != null) 
           _context.StateMachine.ChangeState(newState);
    }
  }
}