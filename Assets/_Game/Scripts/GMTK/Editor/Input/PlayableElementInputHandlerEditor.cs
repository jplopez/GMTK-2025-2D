using UnityEditor;
using UnityEngine;

namespace GMTK {

  [CustomEditor(typeof(PlayableElementInputHandler))]
  public class PlayableElementInputHandlerEditor : UnityEditor.Editor {

    private SerializedProperty pointerWorldPosProp;
    private bool showTransformFoldout = true;

    private void OnEnable() {
      pointerWorldPosProp = serializedObject.FindProperty("_pointerWorldPos");
    }

    public override void OnInspectorGUI() {
      PlayableElementInputHandler handler = (PlayableElementInputHandler)target;

      //EditorGUILayout.Space();
      //DrawRegistrySection(handler);

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
      EditorGUI.indentLevel++;
      EditorGUILayout.Vector2Field("Pointer Position", GetPointerWorldPosition());
      EditorGUI.indentLevel--;

      EditorGUILayout.Space();
      DrawMovingSection(handler);

      EditorGUILayout.Space();
      DrawHoverSection(handler);

      EditorGUILayout.Space();
      DrawEventButtons(handler);
    }


    private void DrawMovingSection(PlayableElementInputHandler handler) {
      EditorGUILayout.LabelField("Moving Elements", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.LabelField("Is Moving", handler.IsMoving.ToString());

        PlayableElement current = handler.CurrentElement;
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
    }

    private void DrawHoverSection(PlayableElementInputHandler handler) {
      EditorGUILayout.LabelField("Hover", EditorStyles.boldLabel);

      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.LabelField("Is Over Element", handler.IsOverElement.ToString());

        //GridSnappable lastOver = GetLastElementOver(handler);
        PlayableElement lastOver = handler.LastElementOver;
        string lastName = lastOver != null ? lastOver.name : "(None)";
        EditorGUILayout.LabelField("Last Element Over", lastName);
      }
    }

    private void DrawEventButtons(PlayableElementInputHandler handler) {
      EditorGUILayout.LabelField("Manual Event Triggers", EditorStyles.boldLabel);

      PlayableElement current = handler.CurrentElement;
      //Vector2 pointerPos = GetPointerWorldPosition();

      GUI.enabled = current != null;

      if (GUILayout.Button("Trigger OnElementHovered"))
        handler.TriggerHoveredEvent();

      if (GUILayout.Button("Trigger OnElementUnhovered"))
        handler.TriggerUnhoveredEvent();

      if (GUILayout.Button("Trigger OnSelect"))
        handler.TriggerSelectedEvent();

      if (GUILayout.Button("Trigger OnElementDropped"))
        handler.TriggerDroppedEvent();

      GUI.enabled = true;
    }

    private Vector2 GetPointerWorldPosition() {
      if (pointerWorldPosProp == null) return Vector2.zero;
      return (Vector2)pointerWorldPosProp.vector3Value;
    }
  }
}