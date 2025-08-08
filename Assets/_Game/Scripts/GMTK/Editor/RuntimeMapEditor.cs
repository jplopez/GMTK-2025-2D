using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GMTK.Editor {
  [CustomEditor(typeof(RuntimeMap))]
  public class RuntimeMapEditor : UnityEditor.Editor {
    private RuntimeMap _map;
    private Dictionary<string, object> _pendingRegistry;
    private string _newId = "";
    private Object _newObj;

    private void OnEnable() {
      _map = (RuntimeMap)target;
      RebuildRegistry();
    }

    private void RebuildRegistry() {
      _pendingRegistry = new Dictionary<string, object>();
      foreach (var id in _map.GetAllIds()) {
        _pendingRegistry[id] = _map.Get(id);
      }
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();
      EditorGUILayout.LabelField("Runtime Registry", EditorStyles.boldLabel);

      if (_pendingRegistry.Count == 0) {
        EditorGUILayout.HelpBox("Registry is empty.", MessageType.Info);
      }
      else {
        EditorGUILayout.Space();
        EditorGUI.indentLevel++;

        List<string> idsToRemove = new();
        Dictionary<string, (string, Object)> edits = new();
        List<(string, string)> duplicates = new();

        foreach (var kvp in _pendingRegistry) {
          EditorGUILayout.BeginVertical();

          EditorGUILayout.BeginHorizontal();
          var newKey = EditorGUILayout.TextField("ID", kvp.Key, EditorStyles.label);
          var newObj = EditorGUILayout.ObjectField("Object", kvp.Value as Object, typeof(Object), true);
          EditorGUILayout.EndHorizontal();

          EditorGUILayout.BeginHorizontal();
          if (GUILayout.Button("Remove")) idsToRemove.Add(kvp.Key);
          if (GUILayout.Button("Duplicate")) duplicates.Add((kvp.Key, newKey));
          EditorGUILayout.EndHorizontal();

          if (newKey != kvp.Key || newObj != kvp.Value as Object) {
            edits[kvp.Key] = (newKey, newObj);
          }

          EditorGUILayout.EndVertical();
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        foreach (var key in idsToRemove) {
          Undo.RecordObject(_map, "Remove Registry Entry");
          _map.Unregister(key);
        }

        foreach (var edit in edits) {
          Undo.RecordObject(_map, "Edit Registry Entry");
          _map.Unregister(edit.Key);
          _map.Register(edit.Value.Item1, edit.Value.Item2);
        }

        foreach (var dup in duplicates) {
          Undo.RecordObject(_map, "Duplicate Registry Entry");
          if (_map.Contains(dup.Item1)) {
            object original = _map.Get(dup.Item1);
            string newId = dup.Item2 + "_copy";
            _map.Register(newId, original);
          }
        }

        RebuildRegistry();
      }

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Add New Entry", EditorStyles.boldLabel);
      _newId = EditorGUILayout.TextField("New ID", _newId);
      _newObj = EditorGUILayout.ObjectField("New Object", _newObj, typeof(Object), true);

      if (GUILayout.Button("Add")) {
        if (!string.IsNullOrEmpty(_newId) && _newObj != null) {
          Undo.RecordObject(_map, "Add Registry Entry");
          _map.Register(_newId, _newObj);
          _newId = "";
          _newObj = null;
          RebuildRegistry();
        }
        else {
          EditorUtility.DisplayDialog("Invalid Entry", "ID must not be empty and Object must not be null.", "OK");
        }
      }

      EditorGUILayout.Space();
      if (GUILayout.Button("Clear Map")) {
        if (EditorUtility.DisplayDialog("Clear RuntimeMap", "Are you sure you want to clear all Entries?", "Yes", "Cancel")) {
          Undo.RecordObject(_map, "Clear RuntimeMap");
          _map.Clear();
          RebuildRegistry();
        }
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
