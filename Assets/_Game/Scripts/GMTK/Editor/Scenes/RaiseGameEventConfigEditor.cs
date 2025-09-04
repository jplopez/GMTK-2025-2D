using Ameba;
using System;
using UnityEditor;
using UnityEngine;

namespace GMTK {
  [CustomEditor(typeof(RaiseGameEventConfig), true)]
  public class RaiseGameEventConfigEditor : Editor {

    //cached list of GameEventTypes
    protected static GameEventType[] _gameEventTypes;

    public override void OnInspectorGUI() {
      serializedObject.Update();

      EditorGUI.BeginDisabledGroup(true);
      EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((RaiseGameEventConfig)target), typeof(MonoScript), false);
      EditorGUI.EndDisabledGroup();

      DrawPropertiesExcluding(serializedObject,
        "m_Script", "Payload", "intParam", "boolParam", "floatParam", "stringParam");
      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("GameEvent Settings", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(serializedObject.FindProperty("Payload"));
      PayloadType payload = (PayloadType)serializedObject.FindProperty("Payload").enumValueIndex;
      GameEventType gameEventType = (GameEventType)serializedObject.FindProperty("EventType").enumValueIndex;
      DrawPayloadField(payload);
      EditorGUILayout.Space(4f);
      DrawTestButton(payload, gameEventType);

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawTestButton(PayloadType payload, GameEventType gameEventType) {
      //Test button to try event type with selected payload.
      if (GUILayout.Button("Test Event")) {
        var eventChannel = serializedObject.FindProperty("_eventChannel").objectReferenceValue as GameEventChannel;
        if (eventChannel == null) {
          Debug.LogError("[RaiseGameEventConfigEditor] No EventChannel assigned.");
          return;
        }
        Debug.Log($"[RaiseGameEventConfigEditor] Testing event {gameEventType} with payload {payload}");
        switch (payload) {
          case PayloadType.Void:
            eventChannel.Raise(gameEventType);
            break;
          case PayloadType.Int:
            int intParam = serializedObject.FindProperty("intParam").intValue;
            eventChannel.Raise(gameEventType, intParam);
            break;
          case PayloadType.Bool:
            bool boolParam = serializedObject.FindProperty("boolParam").boolValue;
            eventChannel.Raise(gameEventType, boolParam);
            break;
          case PayloadType.Float:
            float floatParam = serializedObject.FindProperty("floatParam").floatValue;
            eventChannel.Raise(gameEventType, floatParam);
            break;
          case PayloadType.String:
            string stringParam = serializedObject.FindProperty("stringParam").stringValue;
            eventChannel.Raise(gameEventType, stringParam);
            break;
          case PayloadType.EventArg:
            EditorGUILayout.HelpBox("Testing EventArgs payloads is not supported in the editor.", MessageType.Info);
            break;
          default:
            Debug.LogError("[RaiseGameEventConfigEditor] Unknown payload type. Event not raised.");
            break;
        }
      }

    }

    private void DrawPayloadField(PayloadType payload) {
      switch (payload) {
        case PayloadType.Void:
          break;
        case PayloadType.Int:
          EditorGUILayout.PropertyField(serializedObject.FindProperty("intParam"));
          break;
        case PayloadType.Bool:
          EditorGUILayout.PropertyField(serializedObject.FindProperty("boolParam"));
          break;
        case PayloadType.Float:
          EditorGUILayout.PropertyField(serializedObject.FindProperty("floatParam"));
          break;
        case PayloadType.String:
          EditorGUILayout.PropertyField(serializedObject.FindProperty("stringParam"));
          break;
        case PayloadType.EventArg:
          EditorGUILayout.HelpBox("EventArgs payloads are not supported in the component.", MessageType.Info);
          break;
        default:
          throw new ArgumentException("Unknown payload type");
      }
    }
  }
}