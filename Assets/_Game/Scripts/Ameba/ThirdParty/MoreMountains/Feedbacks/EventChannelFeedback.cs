using System;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace Ameba.MoreMountains.Feedbacks {

  //[AddComponentMenu("MM Feedbacks/Events/MM Feedback Event Channel Advanced")]
  [FeedbackHelp("Advanced EventChannel feedback with runtime event type selection")]
  public abstract class EventChannelFeedback<Tenum> : MMFeedback where Tenum : Enum {

    [Header("Event Channel")]
    [Tooltip("The ScriptableObject EventChannel to use")]
    public EventChannel<Tenum> EventChannelObject;

    [Header("Event Configuration")]
    [Tooltip("Event type name (string representation of enum value)")]
    public string EventTypeName;

    [Tooltip("Payload type for the event")]
    public PayloadType PayloadType = PayloadType.Void;

    [Header("Payload Values")]
    [MMFEnumCondition("PayloadType", (int)PayloadType.Int)]
    public int IntValue;
    [MMFEnumCondition("PayloadType", (int)PayloadType.Bool)]
    public bool BoolValue;
    [MMFEnumCondition("PayloadType", (int)PayloadType.Float)]
    public float FloatValue;
    [MMFEnumCondition("PayloadType", (int)PayloadType.String)]
    public string StringValue = "";

    //private MethodInfo _raiseMethod;
    private Tenum _eventTypeValue;

    public override void Initialization(GameObject owner) {
      base.Initialization(owner);
      PrepareEventChannel();
    }

    private void PrepareEventChannel() {
      if (EventChannelObject == null) {
        Debug.LogError($"[{Label}] EventChannelObject is null");
        return;
      }

      //var channelType = EventChannelObject.GetType();
      var enumType = typeof(Tenum);

      // Use Enum.Parse and cast to Tenum to avoid CS0453
      try {
        object parsedValue = Enum.Parse(enumType, EventTypeName);
        _eventTypeValue = (Tenum)parsedValue;

        //// Get the appropriate Raise method based on payload type
        //if (PayloadType == PayloadType.Void) {
        //  _raiseMethod = channelType.GetMethod("Raise", new[] { enumType });
        //}
        //else {
        //  var payloadSystemType = GetSystemTypeFromPayloadType(PayloadType);
        //  _raiseMethod = channelType.GetMethod("Raise", new[] { enumType, payloadSystemType });
        //}
      }
      catch (Exception) {
        Debug.LogError($"[{Label}] Could not parse event type '{EventTypeName}' for enum {enumType.Name}");
      }
    }

    //private Type GetSystemTypeFromPayloadType(PayloadType payloadType) {
    //  return payloadType switch {
    //    PayloadType.Int => typeof(int),
    //    PayloadType.Bool => typeof(bool),
    //    PayloadType.Float => typeof(float),
    //    PayloadType.String => typeof(string),
    //    _ => typeof(void)
    //  };
    //}

    protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1) {
      if (_eventTypeValue == null) {
        Debug.LogWarning($"[{Label}] Event channel not properly initialized");
        return;
      }

      try {
        switch (PayloadType) {
          case PayloadType.Void:
            EventChannelObject.Raise(_eventTypeValue);
            //_raiseMethod.Invoke(EventChannelObject, new[] { _eventTypeValue });
            break;
          case PayloadType.Int:
            EventChannelObject.Raise(_eventTypeValue, Mathf.RoundToInt(IntValue * feedbacksIntensity));
            //_raiseMethod.Invoke(EventChannelObject, new[] { _eventTypeValue, Mathf.RoundToInt(IntValue * feedbacksIntensity) });
            break;
          case PayloadType.Bool:
            EventChannelObject.Raise(_eventTypeValue, BoolValue);
            //_raiseMethod.Invoke(EventChannelObject, new[] { _eventTypeValue, BoolValue });
            break;
          case PayloadType.Float:
            EventChannelObject.Raise(_eventTypeValue, FloatValue * feedbacksIntensity);
            //_raiseMethod.Invoke(EventChannelObject, new[] { _eventTypeValue, FloatValue * feedbacksIntensity });
            break;
          case PayloadType.String:
            EventChannelObject.Raise(_eventTypeValue, StringValue);
            //_raiseMethod.Invoke(EventChannelObject, new[] { _eventTypeValue, StringValue });
            break;
        }

        Debug.Log($"[{Label}] Event '{EventTypeName}' triggered successfully");
      }
      catch (Exception ex) {
        Debug.LogError($"[{Label}] Failed to trigger event: {ex.Message}");
      }
    }
  }
}