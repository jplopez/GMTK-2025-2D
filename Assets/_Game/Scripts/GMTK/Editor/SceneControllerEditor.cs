#if UNITY_EDITOR
using Ameba;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GMTK {

  [CustomEditor(typeof(SceneController), true)]
  public class SceneControllerEditor : Editor {

    private SerializedProperty _sceneNameProp;
    private SerializedProperty _overrideInitialStateProp;
    private SerializedProperty _overrideInitialStateValueProp;
    private SerializedProperty _overrideLoadBehaviorProp;
    private SerializedProperty _overrideLoadDelayProp;
    private SerializedProperty _onSceneLoadEventsProp;
    private SerializedProperty _enableDebugLoggingProp;

    private LevelService _levelService;
    private LevelService.LevelConfig _selectedConfig;
    private string[] _gameStateNames;
    private string[] _gameEventTypeNames;
    private bool _showAdvancedOptions = false;

    private void OnEnable() {
      // Get properties
      _sceneNameProp = serializedObject.FindProperty("SceneName");
      _overrideInitialStateProp = serializedObject.FindProperty("OverrideInitialState");
      _overrideInitialStateValueProp = serializedObject.FindProperty("_overrideInitialState");
      _overrideLoadBehaviorProp = serializedObject.FindProperty("OverrideLoadBehavior");
      _overrideLoadDelayProp = serializedObject.FindProperty("_overrideLoadDelay");
      _onSceneLoadEventsProp = serializedObject.FindProperty("OnSceneLoadEvents");
      _enableDebugLoggingProp = serializedObject.FindProperty("EnableDebugLogging");

      // Cache enum values
      _gameStateNames = System.Enum.GetNames(typeof(GameStates));
      _gameEventTypeNames = System.Enum.GetNames(typeof(GameEventType));

      // Try to get LevelService
      LoadLevelService();
    }

    private void LoadLevelService() {
      try {
        _levelService = Services.Get<LevelService>();
        if (_levelService != null) {
          RefreshSelectedConfig();
        }
      }
      catch {
        // Services might not be initialized in edit mode
        _levelService = null;
      }

      // Fallback: try to find in Resources
      if (_levelService == null) {
        _levelService = Resources.Load<LevelService>("LevelService");
      }
    }

    private void RefreshSelectedConfig() {
      if (_levelService != null && !string.IsNullOrEmpty(_sceneNameProp.stringValue)) {
        _selectedConfig = _levelService.GetLevelConfig(_sceneNameProp.stringValue);
      }
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      DrawControllerHeader();
      EditorGUILayout.Space();

      DrawLevelConfigurationSelection();
      EditorGUILayout.Space();

      DrawCurrentConfiguration();
      EditorGUILayout.Space();

      DrawOverrides();
      EditorGUILayout.Space();

      DrawSceneEvents();
      EditorGUILayout.Space();

      DrawAdvancedOptions();
      EditorGUILayout.Space();

      DrawUtilityButtons();

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawControllerHeader() {
      EditorGUILayout.LabelField("Scene Manager Configuration", EditorStyles.boldLabel);

      var sceneManager = target as SceneController;
      var currentSceneName = string.IsNullOrEmpty(_sceneNameProp.stringValue) ? "Auto-Detect" : _sceneNameProp.stringValue;

      using (new EditorGUI.DisabledScope(true)) {
        EditorGUILayout.TextField("Scene Name", currentSceneName);
        EditorGUILayout.TextField("Configuration Status", _selectedConfig != null ? "Loaded" : "Not Found");
      }
    }

    private void DrawLevelConfigurationSelection() {
      EditorGUILayout.LabelField("Level Configuration", EditorStyles.boldLabel);

      if (_levelService == null) {
        EditorGUILayout.HelpBox("LevelService not found! Make sure it exists in Resources folder and is registered in Services.", MessageType.Warning);

        if (GUILayout.Button("Reload LevelService")) {
          LoadLevelService();
        }
        return;
      }

      // Level configuration dropdown
      var levelConfigs = _levelService.Levels ?? new LevelService.LevelConfig[0];
      var configNames = levelConfigs.Select(l => $"{l.DisplayName} ({l.SceneName})").ToArray();
      var currentIndex = System.Array.FindIndex(levelConfigs, c => c.SceneName == _sceneNameProp.stringValue);

      EditorGUI.BeginChangeCheck();
      var newIndex = EditorGUILayout.Popup("Select Configuration", currentIndex, configNames);
      if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < levelConfigs.Length) {
        _sceneNameProp.stringValue = levelConfigs[newIndex].SceneName;
        RefreshSelectedConfig();
      }

      // Manual scene name override
      EditorGUILayout.PropertyField(_sceneNameProp, new GUIContent("Manual Scene Name Override"));

      // Apply configuration button
      if (_selectedConfig != null && GUILayout.Button("Apply Selected Configuration", GUILayout.Height(25))) {
        ApplyLevelConfiguration();
      }
    }

    private void DrawCurrentConfiguration() {
      EditorGUILayout.LabelField("Current Configuration", EditorStyles.boldLabel);

      if (_selectedConfig == null) {
        EditorGUILayout.HelpBox("No configuration selected or scene not found in LevelService.", MessageType.Info);
        return;
      }
      using (new EditorGUI.DisabledScope(true)) {
        EditorGUILayout.TextField("Display Name", _selectedConfig.DisplayName);
        EditorGUILayout.EnumPopup("Scene Type", _selectedConfig.Type);
        EditorGUILayout.EnumPopup("Initial Game State", _selectedConfig.InitialGameState);
        EditorGUILayout.Toggle("Set State On Load", _selectedConfig.SetStateOnLoad);
        EditorGUILayout.Toggle("Is Unlocked", _selectedConfig.IsUnlocked);
        EditorGUILayout.FloatField("Load Delay", _selectedConfig.LoadDelay);
      }
    }

    private void DrawOverrides() {
      EditorGUILayout.LabelField("Local Overrides", EditorStyles.boldLabel);

      // Initial State Override
      EditorGUILayout.PropertyField(_overrideInitialStateProp, new GUIContent("Override Initial State"));
      if (_overrideInitialStateProp.boolValue) {
        using (new EditorGUI.IndentLevelScope()) {
          DrawEnumPopup(_overrideInitialStateValueProp, _gameStateNames, "Override State");
        }
      }

      // Load Behavior Override
      EditorGUILayout.PropertyField(_overrideLoadBehaviorProp, new GUIContent("Override Load Behavior"));
      if (_overrideLoadBehaviorProp.boolValue) {
        using (new EditorGUI.IndentLevelScope()) {
          EditorGUILayout.PropertyField(_overrideLoadDelayProp, new GUIContent("Override Load Delay"));
        }
      }

      DrawEffectiveValues();
    }

    private void DrawEffectiveValues() {
      EditorGUILayout.LabelField("Effective Values", EditorStyles.boldLabel);

      // Show effective values
      if (_selectedConfig != null) {
        using (new EditorGUI.DisabledScope(true)) {
          var effectiveState = _overrideInitialStateProp.boolValue
              ? (GameStates)_overrideInitialStateValueProp.enumValueIndex
              : _selectedConfig.InitialGameState;
          var effectiveDelay = _overrideLoadBehaviorProp.boolValue
              ? _overrideLoadDelayProp.floatValue
              : _selectedConfig.LoadDelay;

          EditorGUILayout.EnumPopup("Effective Initial State", effectiveState);
          EditorGUILayout.FloatField("Effective Load Delay", effectiveDelay);
        }
      }
    }

    private void DrawSceneEvents() {
      EditorGUILayout.LabelField("Scene Load Events", EditorStyles.boldLabel);

      EditorGUILayout.PropertyField(_onSceneLoadEventsProp, new GUIContent("Events to Raise on Load"), true);

      // Quick add buttons for common events
      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("+ LevelStart")) {
          AddEventToArray(GameEventType.LevelStart);
        }
        if (GUILayout.Button("+ SceneLoadingComplete")) {
          AddEventToArray(GameEventType.SceneLoadingComplete);
        }
        if (GUILayout.Button("Clear All")) {
          _onSceneLoadEventsProp.arraySize = 0;
        }
      }
    }

    private void DrawAdvancedOptions() {
      _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options", true);

      if (_showAdvancedOptions) {
        using (new EditorGUI.IndentLevelScope()) {
          EditorGUILayout.PropertyField(_enableDebugLoggingProp, new GUIContent("Enable Debug Logging"));

          // Runtime information
          if (Application.isPlaying) {
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);

            var sceneManager = target as SceneController;
            var levelConfig = sceneManager.GetLevelConfig();

            using (new EditorGUI.DisabledScope(true)) {
              EditorGUILayout.TextField("Runtime Scene Name", sceneManager.GetSceneName());
              EditorGUILayout.EnumPopup("Runtime Scene Type", sceneManager.GetSceneType());

              // Now we can display the LevelConfig using our custom property drawer!
              if (levelConfig != null) {
                EditorGUILayout.LabelField("Level Configuration", EditorStyles.boldLabel);

                // Create a SerializedObject for the runtime config to display it properly
                // Since LevelConfig is not a UnityEngine.Object, we'll display it as read-only text fields
                EditorGUILayout.LabelField("Configuration Summary", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope()) {
                  EditorGUILayout.TextField("Scene Name", levelConfig.SceneName ?? "");
                  EditorGUILayout.TextField("Display Name", levelConfig.DisplayName ?? "");
                  EditorGUILayout.EnumPopup("Scene Type", levelConfig.Type);
                  EditorGUILayout.EnumPopup("Initial State", levelConfig.InitialGameState);
                  EditorGUILayout.Toggle("Set State On Load", levelConfig.SetStateOnLoad);
                  EditorGUILayout.Toggle("Is Unlocked", levelConfig.IsUnlocked);
                  EditorGUILayout.FloatField("Load Delay", levelConfig.LoadDelay);

                  if (!string.IsNullOrEmpty(levelConfig.NextSceneName)) {
                    EditorGUILayout.TextField("Next Scene", levelConfig.NextSceneName);
                  }
                  if (!string.IsNullOrEmpty(levelConfig.PreviousSceneName)) {
                    EditorGUILayout.TextField("Previous Scene", levelConfig.PreviousSceneName);
                  }
                }
              }
              else {
                EditorGUILayout.TextField("Level Configuration", "No configuration loaded");
              }
            }
          }
        }
      }
    }

    private void DrawUtilityButtons() {
      EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Refresh Configuration", GUILayout.Height(25))) {
          LoadLevelService();
          RefreshSelectedConfig();
        }

        if (GUILayout.Button("Create Missing Configuration", GUILayout.Height(25))) {
          CreateMissingConfiguration();
        }
      }

      if (_selectedConfig != null) {
        using (new EditorGUILayout.HorizontalScope()) {
          if (GUILayout.Button("Reset Overrides", GUILayout.Height(25))) {
            ResetOverrides();
          }

          if (GUILayout.Button("Open LevelService", GUILayout.Height(25))) {
            Selection.activeObject = _levelService;
          }
        }
      }
    }

    private void DrawEnumPopup(SerializedProperty property, string[] enumNames, string label) {
      var currentIndex = property.enumValueIndex;
      var newIndex = EditorGUILayout.Popup(label, currentIndex, enumNames);
      property.enumValueIndex = newIndex;
    }

    private void AddEventToArray(GameEventType eventType) {
      var arraySize = _onSceneLoadEventsProp.arraySize;
      _onSceneLoadEventsProp.arraySize = arraySize + 1;
      var newElement = _onSceneLoadEventsProp.GetArrayElementAtIndex(arraySize);
      newElement.enumValueIndex = (int)eventType;
    }

    private void ApplyLevelConfiguration() {
      if (_selectedConfig == null) return;

      // Reset overrides and apply configuration values
      _overrideInitialStateProp.boolValue = false;
      _overrideLoadBehaviorProp.boolValue = false;
      _overrideInitialStateValueProp.enumValueIndex = (int)_selectedConfig.InitialGameState;
      _overrideLoadDelayProp.floatValue = _selectedConfig.LoadDelay;

      // Clear existing events and add any defaults based on scene type
      _onSceneLoadEventsProp.arraySize = 0;

      switch (_selectedConfig.Type) {
        case LevelService.SceneType.Level:
          AddEventToArray(GameEventType.LevelStart);
          break;
        case LevelService.SceneType.Start:
          // Add menu-specific events if needed
          break;
      }

      EditorUtility.SetDirty(target);
      Debug.Log($"[SceneControllerEditor] Applied configuration for {_selectedConfig.SceneName}");
    }

    private void CreateMissingConfiguration() {
      if (_levelService == null || string.IsNullOrEmpty(_sceneNameProp.stringValue)) return;

      var newConfig = new LevelService.LevelConfig {
        SceneName = _sceneNameProp.stringValue,
        DisplayName = _sceneNameProp.stringValue,
        Type = LevelService.SceneType.Level,
        InitialGameState = GameStates.Preparation,
        SetStateOnLoad = true,
        IsUnlocked = true,
        CanRestart = true,
        CanSkip = false
      };

      var levelsList = _levelService.Levels?.ToList() ?? new List<LevelService.LevelConfig>();
      levelsList.Add(newConfig);
      _levelService.Levels = levelsList.ToArray();

      EditorUtility.SetDirty(_levelService);
      RefreshSelectedConfig();

      Debug.Log($"[SceneControllerEditor] Created configuration for {_sceneNameProp.stringValue}");
    }

    private void ResetOverrides() {
      _overrideInitialStateProp.boolValue = false;
      _overrideLoadBehaviorProp.boolValue = false;

      EditorUtility.SetDirty(target);
      Debug.Log("[SceneControllerEditor] Reset all overrides");
    }
  }
}
#endif
