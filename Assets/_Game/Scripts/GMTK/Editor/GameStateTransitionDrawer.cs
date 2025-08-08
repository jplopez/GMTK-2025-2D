using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GMTK {


  //[CustomPropertyDrawer(typeof(GameStateTransition))]
  public class GameStateTransitionDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
      var enumValues = Enum.GetValues(typeof(GameStates));
      return EditorGUIUtility.singleLineHeight * (enumValues.Length + 2); // +2 for label and dropdown
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      CacheEnumValues();
      EditorGUI.BeginProperty(position, label, property);

      var enumValues = _cachedEnumValues;
      var fromProp = property.FindPropertyRelative("From");
      var toProp = property.FindPropertyRelative("To");

      // Draw 'Serialized Transitions' label
      position.height = EditorGUIUtility.singleLineHeight;
      EditorGUI.LabelField(position, label);
      position.y += EditorGUIUtility.singleLineHeight;

      DrawSerializedTransitions(position, property, enumValues, fromProp, toProp);
      EditorGUI.EndProperty();
    }

    private void DrawSerializedTransitions(Rect position, SerializedProperty property, List<GameStates> enumValues, SerializedProperty fromProp, SerializedProperty toProp) {
      
      // Draw 'From' dropdown
      EditorGUI.BeginChangeCheck();
      var newFrom = (GameStates)EditorGUI.EnumPopup(position, "From", (GameStates)fromProp.enumValueIndex);
      position.y += EditorGUIUtility.singleLineHeight;

      // Validate 'From' uniqueness
      if (EditorGUI.EndChangeCheck()) {
        if (!EnsureUniqueFromState(property, fromProp, newFrom)) {
          EditorGUI.HelpBox(position, $"Duplicate transition entry for {newFrom}. Only one allowed.", MessageType.Warning);
          position.y += EditorGUIUtility.singleLineHeight * 2;
        }
      }

      // Draw toggles for each enum value
      DrawToStateToggles(position, enumValues, toProp);
    }

    private static void DrawToStateToggles(Rect position, List<GameStates> enumValues, SerializedProperty toProp) {
      for (int i = 0; i < enumValues.Count; i++) {
        var toState = enumValues[i];
        var isActive = false;
        for(int j = 0; j < toProp.arraySize; j++) {
          if(toProp.GetArrayElementAtIndex(j).enumValueIndex.Equals(toState)) {
            isActive = true;
            break;
          }
        }

        bool toggled = EditorGUI.ToggleLeft(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), toState.ToString(), isActive);
        position.y += EditorGUIUtility.singleLineHeight;

        if (toggled && !toProp.enumNames.Contains(toState.ToString())) {
          toProp.InsertArrayElementAtIndex(toProp.arraySize);
          toProp.GetArrayElementAtIndex(toProp.arraySize - 1).enumValueIndex = Convert.ToInt32(toState);
        }
        else if (!toggled) {
          for (int j = 0; j < toProp.arraySize; j++) {
            if (toProp.GetArrayElementAtIndex(j).enumValueIndex == Convert.ToInt32(toState)) {
              toProp.DeleteArrayElementAtIndex(j);
              break;
            }
          }
        }
      }
    }

    private bool EnsureUniqueFromState(SerializedProperty property, SerializedProperty fromProp, GameStates newFrom) {
      fromProp.enumValueIndex = Convert.ToInt32(newFrom);

      // Validate uniqueness
      var parentArray = property.serializedObject.FindProperty(property.propertyPath.Split('.')[0]);
      int count = 0;
      for (int i = 0; i < parentArray.arraySize; i++) {
        var other = parentArray.GetArrayElementAtIndex(i).FindPropertyRelative("From");
        count += (other.enumValueIndex == fromProp.enumValueIndex) ? 1 : 0;
        //if (count > 1) {
        //  EditorGUI.HelpBox(position, $"Duplicate transition entry for {newFrom}. Only one allowed.", MessageType.Warning);
        //  position.y += EditorGUIUtility.singleLineHeight * 2;
        //}
      }
      return count <= 1;
    }

    private List<GameStates> _cachedEnumValues;

    private void CacheEnumValues() {
      if (_cachedEnumValues == null) {
        _cachedEnumValues = Enum.GetValues(typeof(GameStates)).Cast<GameStates>().ToList();
      }
    }

  }
}