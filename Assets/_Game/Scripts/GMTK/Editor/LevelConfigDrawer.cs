#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace GMTK {

  [CustomPropertyDrawer(typeof(LevelService.LevelConfig))]
  public class LevelConfigDrawer : PropertyDrawer {

    private readonly Dictionary<string, bool> _foldoutStates = new();
    private string[] _gameStateNames;
    private string[] _sceneTypeNames;
    private string[] _allSceneNames;

    private static GUIStyle _labelHeaderStyle = new GUIStyle(EditorStyles.boldLabel) {
      fontSize = 12,
      normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
    };

    private void CacheEnumValues() {
      if (_gameStateNames == null) {
        _gameStateNames = System.Enum.GetNames(typeof(GameStates));
      }

      if (_sceneTypeNames == null) {
        _sceneTypeNames = System.Enum.GetNames(typeof(LevelService.SceneType));
      }

      if (_allSceneNames == null) {
        _allSceneNames = GetAllSceneNames();
      }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      CacheEnumValues();

      var foldoutKey = $"levelconfig_{property.propertyPath}";
      if (!_foldoutStates.ContainsKey(foldoutKey)) {
        _foldoutStates[foldoutKey] = false;
      }

      EditorGUI.BeginProperty(position, label, property);
      var contentRect = new Rect(position.x, position.y + 2, position.width-25f, position.height - 2f );
      DrawLevelConfigFields(contentRect, property);
      EditorGUI.EndProperty();
    }

    private void DrawLevelConfigFields(Rect rect, SerializedProperty property) {
      var fieldHeight = EditorGUIUtility.singleLineHeight;
      var spacing = 1.5f;
      var currentY = rect.y;
      var indentWidth = 8f;

      // Indent the content
      var contentRect = new Rect(rect.x + indentWidth, currentY, rect.width - indentWidth, fieldHeight);
      float fieldX = contentRect.x + indentWidth;

      // Basic Info Section
      DrawSectionHeader(ref currentY, contentRect.width, "Basic Information", contentRect.x);

      var sceneNameProp = property.FindPropertyRelative("SceneName");
      var displayNameProp = property.FindPropertyRelative("DisplayName");
      var sceneTypeProp = property.FindPropertyRelative("Type");

      // Scene Name with dropdown
      var sceneRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      DrawSceneNameField(sceneRect, sceneNameProp);
      currentY += fieldHeight + spacing;

      // Display Name
      var displayRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      EditorGUI.PropertyField(displayRect, displayNameProp);
      currentY += fieldHeight + spacing;

      // Scene Type
      var typeRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      DrawEnumPopup(typeRect, sceneTypeProp, _sceneTypeNames, "Type");
      currentY += fieldHeight + spacing * 2;

      // Game State Section
      DrawSectionHeader(ref currentY, contentRect.width, "Game State Configuration", contentRect.x);

      var initialStateProp = property.FindPropertyRelative("InitialGameState");
      var setStateOnLoadProp = property.FindPropertyRelative("SetStateOnLoad");

      // Initial Game State
      var stateRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      DrawEnumPopup(stateRect, initialStateProp, _gameStateNames, "Initial Game State");
      currentY += fieldHeight + spacing;

      // Set State On Load
      var setStateRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      EditorGUI.PropertyField(setStateRect, setStateOnLoadProp);
      currentY += fieldHeight + spacing * 2;

      // Scene Management Section
      DrawSectionHeader(ref currentY, contentRect.width, "Scene Management", contentRect.x);

      var isUnlockedProp = property.FindPropertyRelative("IsUnlocked");
      var canRestartProp = property.FindPropertyRelative("CanRestart");
      var canSkipProp = property.FindPropertyRelative("CanSkip");

      // Toggles in a horizontal group
      var toggleWidth = contentRect.width / 3f - 5f;
      var unlockedRect = new Rect(fieldX, currentY, toggleWidth, fieldHeight);
      DrawToggleLeft(unlockedRect, isUnlockedProp, "Unlocked");

      var restartRect = new Rect(fieldX + toggleWidth + 5f, currentY, toggleWidth, fieldHeight);
      DrawToggleLeft(restartRect, canRestartProp, "Can Restart");

      var skipRect = new Rect(fieldX + (toggleWidth + 5f) * 2, currentY, toggleWidth, fieldHeight);
      DrawToggleLeft(skipRect, canSkipProp, "Can Skip");

      currentY += fieldHeight + spacing * 2;

      // Progression Section
      DrawSectionHeader(ref currentY, contentRect.width, "Level Progression", contentRect.x);

      var nextSceneProp = property.FindPropertyRelative("NextSceneName");
      var prevSceneProp = property.FindPropertyRelative("PreviousSceneName");

      // Next Scene
      var nextRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      DrawSceneNameField(nextRect, nextSceneProp, "Next Scene");
      currentY += fieldHeight + spacing;

      // Previous Scene  
      var prevRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      DrawSceneNameField(prevRect, prevSceneProp, "Previous Scene");
      currentY += fieldHeight + spacing * 2;

      // Advanced Section
      DrawSectionHeader(ref currentY, contentRect.width, "Advanced Options", contentRect.x);

      var loadDelayProp = property.FindPropertyRelative("LoadDelay");
      var customLoadProp = property.FindPropertyRelative("UseCustomLoadMethod");
      var customMethodProp = property.FindPropertyRelative("CustomLoadMethod");

      // Load Delay
      var delayRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      EditorGUI.PropertyField(delayRect, loadDelayProp);
      currentY += fieldHeight + spacing;

      // Custom Load Method
      var customRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
      DrawToggle(customRect, customLoadProp, "Use Custom Load");
      //EditorGUI.PropertyField(customRect, customLoadProp);
      currentY += fieldHeight + spacing;

      // Custom Method Name (only if enabled)
      if (customLoadProp.boolValue) {
        var methodRect = new Rect(fieldX, currentY, contentRect.width, fieldHeight);
        EditorGUI.PropertyField(methodRect, customMethodProp);
        currentY += fieldHeight + spacing;
      }
    }

    private void DrawSectionHeader(ref float currentY, float width, string title, float x) {
      var headerRect = new Rect(x, currentY, width, EditorGUIUtility.singleLineHeight);

      // Draw a subtle background for the section header
      var oldColor = GUI.backgroundColor;
      GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);
      GUI.Box(headerRect, "");
      GUI.backgroundColor = oldColor;

      EditorGUI.LabelField(headerRect, title, _labelHeaderStyle);
      currentY += EditorGUIUtility.singleLineHeight + 4;
    }

    private void DrawSceneNameField(Rect rect, SerializedProperty property, string label = "Scene Name") {
      var currentIndex = System.Array.IndexOf(_allSceneNames, property.stringValue);

      EditorGUI.BeginProperty(rect, new GUIContent(label), property);
      var newIndex = EditorGUI.Popup(rect, label, currentIndex, _allSceneNames);
      if (newIndex >= 0 && newIndex < _allSceneNames.Length) {
        property.stringValue = _allSceneNames[newIndex];
      }
      EditorGUI.EndProperty();
    }

    private void DrawEnumPopup(Rect rect, SerializedProperty property, string[] enumNames, string label) {
      var currentIndex = property.enumValueIndex;
      EditorGUI.BeginProperty(rect, new GUIContent(label), property);
      var newIndex = EditorGUI.Popup(rect, label, currentIndex, enumNames);
      property.enumValueIndex = newIndex;
      EditorGUI.EndProperty();
    }

    private void DrawToggle(Rect rect, SerializedProperty property, string label) {
      var guiContent = EditorGUI.BeginProperty(rect, new GUIContent(label), property);
      bool currentValue = property.boolValue;
      property.boolValue = EditorGUI.Toggle(rect, guiContent, currentValue);
      EditorGUI.EndProperty();
    }

    private void DrawToggleLeft(Rect rect, SerializedProperty property, string label) {
      var guiContent = EditorGUI.BeginProperty(rect, new GUIContent(label), property);
      bool currentValue = property.boolValue;
      property.boolValue = EditorGUI.ToggleLeft(rect, guiContent, currentValue);
      EditorGUI.EndProperty();
    }

    private string[] GetAllSceneNames() {
      var sceneGuids = UnityEditor.AssetDatabase.FindAssets("t:Scene");
      var sceneNames = new List<string> { "None" };

      foreach (var guid in sceneGuids) {
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        sceneNames.Add(sceneName);
      }

      return sceneNames.ToArray();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
      var foldoutKey = $"levelconfig_{property.propertyPath}";
      if (_foldoutStates.TryGetValue(foldoutKey, out var isExpanded) && isExpanded) {
        // Calculate height based on all fields
        var baseHeight = EditorGUIUtility.singleLineHeight + 4; // Header
        var fieldsHeight = 14 * EditorGUIUtility.singleLineHeight + 28; // All fields + spacing
        var sectionsHeight = 4 * EditorGUIUtility.singleLineHeight + 16; // Section headers

        // Check if custom load method is enabled to add extra height
        var customLoadProp = property.FindPropertyRelative("UseCustomLoadMethod");
        if (customLoadProp != null && customLoadProp.boolValue) {
          fieldsHeight += EditorGUIUtility.singleLineHeight + 2;
        }

        return baseHeight + fieldsHeight + sectionsHeight;
      }
      return EditorGUIUtility.singleLineHeight + 4;
    }
  }
}
#endif
