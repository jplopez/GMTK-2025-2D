using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ameba.Input {

  [CustomEditor(typeof(InputActionRegistry))]
  public class InputActionRegistryEditor : Editor {

    private ReorderableList _orderedBindings;
    private SerializedProperty _bindingsProp;

    private void OnEnable() => BuildReordenableList();
    public override void OnInspectorGUI() {

      SerializedProperty actionMapProp = serializedObject.FindProperty("ActionMapName");
      EditorGUI.BeginChangeCheck();

      EditorGUILayout.PropertyField(actionMapProp);
      EditorGUILayout.Space(5);

      if (EditorGUI.EndChangeCheck()) {
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
      }

      serializedObject.Update();
      _orderedBindings.DoLayoutList();
      serializedObject.ApplyModifiedProperties();
    }

    private void BuildReordenableList() {
      _bindingsProp = serializedObject.FindProperty("Bindings");
      _orderedBindings = new ReorderableList(serializedObject, _bindingsProp, true, true, true, true);
      _orderedBindings.elementHeight = EditorGUIUtility.singleLineHeight;

      _orderedBindings.onAddCallback = list => {
        _bindingsProp.arraySize++;
        serializedObject.ApplyModifiedProperties(); // Force Unity to serialize the new element

        var newElement = _bindingsProp.GetArrayElementAtIndex(_bindingsProp.arraySize - 1);
        var actionNameProp = newElement.FindPropertyRelative("ActionName");

        if (actionNameProp != null)
          actionNameProp.stringValue = ""; // Safe to assign now

        serializedObject.ApplyModifiedProperties(); // Save changes
      };
      _orderedBindings.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {

        var element = _orderedBindings.serializedProperty.GetArrayElementAtIndex(index);

        var startedProp = element.FindPropertyRelative("Started");
        var performedProp = element.FindPropertyRelative("Performed");
        var canceledProp = element.FindPropertyRelative("Canceled");

        rect.y += 2f;
        float padding = 4f;
        float labelWidth = 120f;
        float toggleWidth = 80f;

        //Action name field
        EditorGUI.PropertyField(new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight),
          element.FindPropertyRelative("ActionName"), GUIContent.none);

        // Checkboxes for phases
        // Read-only toggles based on null check
        GUI.enabled = false;

        EditorGUI.ToggleLeft(
            new Rect(rect.x + labelWidth + padding, rect.y, toggleWidth, EditorGUIUtility.singleLineHeight),
            "Started", startedProp != null );

        EditorGUI.ToggleLeft(
            new Rect(rect.x + labelWidth + padding + 90, rect.y, toggleWidth, EditorGUIUtility.singleLineHeight),
            "Performed", performedProp != null );

        EditorGUI.ToggleLeft(
            new Rect(rect.x + labelWidth + padding + 180, rect.y, toggleWidth, EditorGUIUtility.singleLineHeight),
            "Canceled", canceledProp != null );

        GUI.enabled = true;

      };
    }
  }

}