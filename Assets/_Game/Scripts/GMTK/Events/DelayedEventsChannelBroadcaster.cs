using System.Collections.Generic;
using UnityEngine;
using Ameba;
using System;
using System.Linq;

namespace GMTK {

  public class DelayedEventsChannelBroadcaster : MonoBehaviour, IEventsChannelBroadcaster<GameEventType> {

    [Header("Broadcaster Settings")]
    [Tooltip("Max number of events acting in parallel. Use this field to tune the performance")]
    public int MaxConcurrentEvents = 10;
    [Tooltip("Default delay when raising an event, if not explicit on the event itself")]
    public float DefaultInitialDelay = 0.1f;
    [Tooltip("All the events this broadcast should listen for")]
    public List<GameEventType> Events = new();

    protected Coroutine _broadcasterRoutine;

    public EventChannel<GameEventType> Channel { 
      get => _eventChannel;
      set {
        if(value is GameEventChannel gameChannel) 
          _eventChannel = gameChannel;
      } 
    }

    protected List<BroadcasterListener> _broadcasters = new();
    protected GameEventChannel _eventChannel;

    protected void Awake() {
      if (_eventChannel == null) {
        _eventChannel = Game.Context.EventsChannel;
      }
      AddAllListeners();
    }

    private void AddBroadcastListeners() {
      //Adds listeners found in the broadcasters list
      foreach (var eventType in Events) {
        List<BroadcasterListener> listeners = _broadcasters.Where(b => b.EventType == eventType).ToList();
        listeners.ForEach(bListener => {
          if (bListener.Callback != null)
            _eventChannel.AddListener(bListener.EventType, bListener.Callback);
        });
      }
    }

    public void AddAllListeners() {
      AddBroadcastListeners();
    }

    public bool IsConnected() {
      throw new System.NotImplementedException();
    }

    public void RaiseAllEvents() {
      throw new System.NotImplementedException();
    }

    public void RaiseEvent(GameEventType eventType) {
      throw new System.NotImplementedException();
    }

    public void RaiseEvents(GameEventType eventType1, GameEventType eventType2) {
      throw new System.NotImplementedException();
    }

    public void RaiseEvents(GameEventType eventType1, GameEventType eventType2, GameEventType eventType3) {
      throw new System.NotImplementedException();
    }

    public void RemoveAllListeners() {
      throw new System.NotImplementedException();
    }

    public void RaiseEvents(GameEventType[] eventType) {
      throw new NotImplementedException();
    }
  }


  [Serializable]
  public class BroadcasterListener {

    [Header("Broadcaster Agent Settings")]
    public GameEventType EventType { get; set; }
    public float InitialDelay = 0f;
    public Action<EventArgs> Callback { get; set; }

    public BroadcasterListener(GameEventType eventType, float delay)  {
      EventType = eventType;
      InitialDelay = delay;
    }

    public BroadcasterListener WithCallback<T>(Action<T> callback) where T : EventArgs {
      if (callback is Action<EventArgs> castedCallback) {
        Callback = castedCallback;
      }
      else {
        throw new ArgumentException($"'{typeof(T)}' is not supported, because is not derived from 'EventArgs'");
      }
      return this;
    }
  }
}