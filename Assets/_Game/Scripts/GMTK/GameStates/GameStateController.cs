using Ameba;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace GMTK {

  /// <summary>
  /// This MonoBehaviour triggers a call directly to a service findable by the ServiceLocator. This script is intended for UnityEvents or similar to trigger calls to services.
  /// </summary>
  public class ServiceLocatorTrigger : MonoBehaviour {

    private void Start() {
      //ensure ServiceLocator is initialized
      if (!ServiceLocator.IsInitialized) {
        this.LogError($"ServiceLocator not initialized. Triggers won't work");
      }
    }

    public void ChangeState(GameStates newState) {
      if (ServiceLocator.TryGet<GameStateMachine>(out var stateMachine)) {
        stateMachine.ChangeState(newState);
      } else {
        this.LogError($"Can't change state to {newState}. GameStateMachine not found");
      }
    }

    public void GameEventRaise(GameEventType eventType) {
      if(ServiceLocator.TryGet<GameEventChannel>(out var eventChannel)) {
        eventChannel.Raise(eventType);
      } else {
        this.LogError($"Can't raise event {eventType}. GameEventChannel not found");
      }
    }
  }

  public struct ServiceCall {
    public string name;
    public string eventType;
    public object payload;

      public T GetPayload<T>() {
        if (payload is T t) {
          return t;
        }
        return default;
      }
    }
  }