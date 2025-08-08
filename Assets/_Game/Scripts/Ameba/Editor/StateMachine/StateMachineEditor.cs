using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ameba {

  [CustomEditor(typeof(ScriptableObject), true)]
  public class StateMachineEditor : Editor {

    private FieldInfo startingStateField;
    private FieldInfo noRestrictionsField;
    private Type enumType;


    public override void OnInspectorGUI() {
      var targetType = target.GetType();
      var baseType = targetType.BaseType;

      if (baseType == null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(StateMachine<>)) {
        DrawDefaultInspector();
        return;
      }


      serializedObject.Update();

      var currentStateField = baseType.GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
      var currentState = currentStateField.GetValue(target);
      EditorGUILayout.LabelField("Current State", currentState.ToString(), EditorStyles.label);

      DrawPropertiesExcluding(serializedObject, "_transitions");
      serializedObject.ApplyModifiedProperties();

      enumType = baseType.GetGenericArguments()[0];
      startingStateField = baseType.GetField("StartingState", BindingFlags.NonPublic | BindingFlags.Instance);
      //var transitionsField = baseType.GetField("_serializedTransitions", BindingFlags.NonPublic | BindingFlags.Instance);

      //EditorGUILayout.LabelField("State Transitions", EditorStyles.boldLabel);

      //if (transitionsField?.GetValue(target) is IDictionary transitions) {
      //  foreach (var key in transitions.Keys) {
      //    EditorGUILayout.LabelField($"From: {key}");
      //    if (transitions[key] is IEnumerable<object> list) {
      //      foreach (var toState in list) {
      //        EditorGUILayout.LabelField($"  → {toState}");
      //      }
      //    }
      //  }
      //}

      if (GUILayout.Button("Reset States")) {
        var resetMethod = baseType.GetMethod("Reset");
        resetMethod?.Invoke(target, null);
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}