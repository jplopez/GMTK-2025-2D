
using MoreMountains.Tools;
using UnityEngine;
using Ameba;

namespace GMTK {

  public class FeelToEventChannel : MonoBehaviour, MMEventListener<MMGameEvent> {

    public GameEventChannel eventChannel;

    public GameStateMachine stateMachine;

    protected virtual void OnEnable() => this.MMEventStartListening();

    protected virtual void OnDisable() => this.MMEventStopListening();

    private void Start() {
      if (eventChannel == null) {
        if (ServiceLocator.TryGet<GameEventChannel>(out var channel)) {
          eventChannel = channel;
        }
        else {
          Debug.LogError("No Event Channel found for FeelToEventChannel on " + gameObject.name);
        }
      }

      if (stateMachine == null) {
        if (ServiceLocator.TryGet<GameStateMachine>(out var gsm)) {
          stateMachine = gsm;
        }
        else {
          Debug.LogError("No GameStateMachine found for FeelToEventChannel on " + gameObject.name);
        }
      }
    }
    public void OnMMEvent(MMGameEvent gameEvent) {
      if (string.IsNullOrEmpty(gameEvent.StringParameter)) return;
      this.Log($"Received MMGameEvent: {gameEvent.EventName} with StringParameter: {gameEvent.StringParameter}");
      // Game State change
      if (gameEvent.StringParameter.Equals("GameStates")) {
        if (System.Enum.TryParse<GameStates>(gameEvent.EventName, out var state)) {
          if (stateMachine != null) stateMachine.ChangeState(state);
          return;
        }
        else {
          this.LogWarning($"Unable to parse GameStates from event name '{gameEvent.EventName}'");
        }
      }
      //Event Channel
      else if (gameEvent.StringParameter.Equals("GameEvent")) {
        if (System.Enum.TryParse<GameEventType>(gameEvent.EventName, out var eventType)) {
          if (eventChannel != null) eventChannel.Raise(eventType);
        }
        else {
          this.LogWarning($"Unable to parse GameEventType from event name '{gameEvent.EventName}'");
        }
      }
      else {
        this.LogWarning($"Unsupported StringParameter '{gameEvent.StringParameter}'");
      }
    }

  }

}