using UnityEditor;
using UnityEngine;

namespace GMTK {

  [CustomEditor(typeof(SnappableInputHandler))]
  public class SnappableInputHandlerEditor : UnityEditor.Editor {
    private SerializedProperty inputActionRegistry;
    private bool showTransformFoldout = true;

    public override void OnInspectorGUI() {
      SnappableInputHandler handler = (SnappableInputHandler)target;

      EditorGUILayout.Space();
      DrawRegistrySection(handler);

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
      EditorGUI.indentLevel++;
      EditorGUILayout.Vector2Field("Pointer Position", GetPointerWorldPosition(handler));

      EditorGUILayout.Space();
      DrawMovingSection(handler);

      EditorGUILayout.Space();
      DrawHoverSection(handler);

      EditorGUILayout.Space();
      DrawEventButtons(handler);
      EditorGUI.indentLevel--;
    }

    private void DrawRegistrySection(SnappableInputHandler handler) {

      EditorGUILayout.LabelField("Input Registry", EditorStyles.boldLabel);
      inputActionRegistry = serializedObject.FindProperty("Registry");
      if (inputActionRegistry != null) {
        EditorGUILayout.PropertyField(inputActionRegistry);
        GUI.enabled = handler.Registry != null;
        if (GUILayout.Button("Initialize Handler")) handler.Initialize();
        GUI.enabled = true;
      }
      serializedObject.ApplyModifiedProperties();
    }

    private void DrawMovingSection(SnappableInputHandler handler) {
      EditorGUILayout.LabelField("Moving Elements", EditorStyles.boldLabel);
      EditorGUILayout.Toggle("Is Moving", handler.IsMoving);

      GridSnappable current = GetCurrentElement(handler);
      string currentName = current != null ? current.name : "(None)";
      EditorGUILayout.LabelField("Current Element", currentName);

      if (current != null) {
        showTransformFoldout = EditorGUILayout.Foldout(showTransformFoldout, "Transform Status");
        if (showTransformFoldout) {
          EditorGUI.indentLevel++;

          float rotationZ = current.transform.eulerAngles.z;
          bool flipX = current.transform.localScale.x < 0f;
          bool flipY = current.transform.localScale.y < 0f;

          EditorGUILayout.LabelField("Rotation (Z)", rotationZ.ToString("F2") + "°");
          EditorGUILayout.LabelField("Flip X", flipX.ToString());
          EditorGUILayout.LabelField("Flip Y", flipY.ToString());

          EditorGUI.indentLevel--;
        }
      }
    }

    private void DrawHoverSection(SnappableInputHandler handler) {
      EditorGUILayout.LabelField("Hover", EditorStyles.boldLabel);
      EditorGUILayout.Toggle("Is Over Element", handler.IsOverElement);

      GridSnappable lastOver = GetLastElementOver(handler);
      string lastName = lastOver != null ? lastOver.name : "(None)";
      EditorGUILayout.LabelField("Last Element Over", lastName);
    }

    private void DrawEventButtons(SnappableInputHandler handler) {
      EditorGUILayout.LabelField("Manual Event Triggers", EditorStyles.boldLabel);

      GridSnappable current = GetCurrentElement(handler);
      Vector2 pointerPos = GetPointerWorldPosition(handler);

      GUI.enabled = current != null;

      if (GUILayout.Button("Trigger OnElementHovered"))
        handler.TriggerHoveredEvent();

      if (GUILayout.Button("Trigger OnElementUnhovered"))
        handler.TriggerUnhoveredEvent();

      if (GUILayout.Button("Trigger OnElementSelected"))
        handler.TriggerSelectedEvent();

      if (GUILayout.Button("Trigger OnElementDropped"))
        handler.TriggerDroppedEvent();

      if (GUILayout.Button("Trigger OnElementSecondary"))
        handler.TriggerSecondaryEvent();
      GUI.enabled = true;
    }

    private GridSnappable GetCurrentElement(SnappableInputHandler handler) {
      var field = typeof(SnappableInputHandler).GetField("_currentElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      return field?.GetValue(handler) as GridSnappable;
    }

    private GridSnappable GetLastElementOver(SnappableInputHandler handler) {
      var field = typeof(SnappableInputHandler).GetField("_lastElementOver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      return field?.GetValue(handler) as GridSnappable;
    }

    private Vector2 GetPointerWorldPosition(SnappableInputHandler handler) {
      var field = typeof(SnappableInputHandler).GetField("_pointerWorldPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      return field != null ? (Vector2)(Vector3)field.GetValue(handler) : Vector2.zero;
    }
  }
}