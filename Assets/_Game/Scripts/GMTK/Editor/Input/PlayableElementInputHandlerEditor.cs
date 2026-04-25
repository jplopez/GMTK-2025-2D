using System;
using UnityEditor;
using UnityEngine;

namespace GMTK {

  [CustomEditor(typeof(PlayableElementInputHandler))]
  public class PlayableElementInputHandlerEditor : UnityEditor.Editor {

    private SerializedProperty _enableInputProp;
    private SerializedProperty _pointerWorldPosProp;
    private SerializedProperty _selectionTriggersProp;
    
    private SerializedProperty _activeElementProp;
    private SerializedProperty _currentHoveredElementProp;
    
    private bool _showTransformFoldout = true;

    private void OnEnable() {
      _enableInputProp = serializedObject.FindProperty("_enableInput");
      _pointerWorldPosProp = serializedObject.FindProperty("_pointerWorldPos");
      _selectionTriggersProp = serializedObject.FindProperty("_selectionTriggers");
      _activeElementProp = serializedObject.FindProperty("_activeElement");
      _currentHoveredElementProp = serializedObject.FindProperty("_currentHoveredElement");
    }

    public override void OnInspectorGUI() {
      PlayableElementInputHandler handler = (PlayableElementInputHandler)target;

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope())
      {
        EditorGUILayout.PropertyField(_enableInputProp);
        EditorGUILayout.PropertyField(_selectionTriggersProp);
      }
      
      EditorGUILayout.Space(10);
      DrawInputs(handler);
      
      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("Pointer", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope())
      {
        EditorGUILayout.Vector2Field("Position", GetPointerWorldPosition());
        EditorGUILayout.LabelField("Is Hovering", handler.IsOverElement.ToString());
        DrawPropertyAsLabel(_currentHoveredElementProp);
      }

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Active Element", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope())
      {
        DrawPropertyAsLabel(_activeElementProp);
        if (_activeElementProp != null && _activeElementProp.objectReferenceValue != null)
        {
          EditorGUILayout.LabelField("Is Moving", handler.IsMovingElement.ToString());
          DrawActiveElement(handler);        
        }
      }

      EditorGUILayout.Space();
      DrawEventButtons(handler);
    }

    private void DrawInputs(PlayableElementInputHandler handler)
    {
      EditorGUILayout.LabelField("Inputs", EditorStyles.boldLabel);
      using (new EditorGUI.IndentLevelScope())
      {
        var primaryLabel = handler.IsSelectPressed ? "Pressed" : "Released";
        EditorGUILayout.LabelField("Primary Button", primaryLabel);
      }
    }
    private void DrawActiveElement(PlayableElementInputHandler handler)
    {
      PlayableElement current = handler.ActiveElement;
      if (current != null)
      {
        _showTransformFoldout = EditorGUILayout.Foldout(_showTransformFoldout, "Transform Status");
        if (_showTransformFoldout)
        {
          EditorGUI.indentLevel++;

          float rotationZ = current.transform.eulerAngles.z;
          bool flipX = current.transform.localScale.x < 0f;
          bool flipY = current.transform.localScale.y < 0f;

          EditorGUILayout.LabelField("Rotation (Z)", rotationZ.ToString("F2") + "�");
          EditorGUILayout.LabelField("Flip X", flipX.ToString());
          EditorGUILayout.LabelField("Flip Y", flipY.ToString());

          EditorGUI.indentLevel--;
        }
      }
    }

    private static void DrawPropertyAsLabel(SerializedProperty property, string label="Name")
    {
      var propName = property != null && property.objectReferenceValue != null
        ? property.objectReferenceValue.name
        : "(None)";
      EditorGUILayout.LabelField(label, propName);
    }
 
    private static void DrawEventButtons(PlayableElementInputHandler handler) {
      EditorGUILayout.LabelField("Manual Event Triggers", EditorStyles.boldLabel);

      PlayableElement current = handler.ActiveElement;
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
      if (_pointerWorldPosProp == null) return Vector2.zero;
      return _pointerWorldPosProp.vector3Value;
    }
  }
}