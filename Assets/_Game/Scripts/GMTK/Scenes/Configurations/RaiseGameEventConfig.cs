using Ameba;
using System;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This configuration component can be added to any SceneController to trigger a GameEvent when the scene loads.
  /// Unlike SceneController events, this component can be used with additional parameters and can be extended to add custom logic.
  /// </summary>
  [AddComponentMenu("GMTK/Scenes/Raise GameEvent Component")]
  public class RaiseGameEventConfig : MonoBehaviour, ISceneConfigExtension {

    public GameEventChannel eventChannel;
    public GameEventType EventType;

    [Tooltip("Parameter to pass with the event. If the event does not require a parameter, this can be left as default.")]
    public PayloadType Payload = PayloadType.Void;

    [SerializeField] protected int intParam;
    [SerializeField] protected bool boolParam;
    [SerializeField] protected float floatParam;
    [SerializeField] protected string stringParam;

    private void Awake() {
      if (eventChannel == null) eventChannel = ServiceLocator.Get<GameEventChannel>();
      if (eventChannel == null) {
        this.LogError("No EventChannel found in ServiceLocator. Please ensure a GameEventChannel is registered.");
      }
    }
    public void ApplyConfig(SceneController controller) {
      
      if (controller == null) return;
      if (eventChannel == null) return;
      if (!ValidateEventParam()) {
        this.LogError("Event parameter validation failed. Event not raised.");
        return;
      }
      this.Log($"Raising event {EventType} with payload {Payload}");
      switch (Payload) {
        case PayloadType.Void:
          eventChannel.Raise(EventType);
          break;
        case PayloadType.Int:
          eventChannel.Raise(EventType, intParam);
          break;
        case PayloadType.Bool:
          eventChannel.Raise(EventType, boolParam);
          break;
        case PayloadType.Float:
          eventChannel.Raise(EventType, floatParam);
          break;
        case PayloadType.String:
          eventChannel.Raise(EventType, stringParam);
          break;
        case PayloadType.EventArg:
          break;
        default:
          this.LogError("Unknown payload type. Event not raised.");
          break;
      }
    }

    private bool ValidateEventParam() {
      // Basic validation to ensure the parameter matches the expected payload type
      switch (Payload) {
        case PayloadType.Void:
          return true; // No parameter needed
        case PayloadType.Int:
          return true; // intParam is always valid
        case PayloadType.Bool:
          return true; // boolParam is always valid
        case PayloadType.Float:
          return true; // floatParam is always valid
        case PayloadType.String:
          return stringParam != null; // stringParam should not be null
        case PayloadType.EventArg:
          return false; // eventParam is not supported
        default:
          this.LogError("Unknown payload type.");
          return false;
      }
    }

    //this configuration can be applied to any scene type
    public bool CanApplyOnType(SceneType type) => true;

  }

}