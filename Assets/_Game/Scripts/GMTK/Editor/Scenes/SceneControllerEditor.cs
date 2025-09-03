#if UNITY_EDITOR
using Ameba;
using UnityEditor;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This custom editor will show the configuration found for the Scene based on LevelService, the overrides done in the specific SceneController, and the effective values, to validate the results
  /// </summary>
  [CustomEditor(typeof(SceneController), true)]
  public class SceneControllerEditor : Editor {

    private SerializedProperty _configSourceProp;
    private SerializedProperty _sceneNameProp;
    private SerializedProperty _manualConfigProp;
    private SerializedProperty _effectiveConfigProp;
    private SerializedProperty _onSceneLoadEventsProp;
    private SerializedProperty _enableDebugLoggingProp;
    private SerializedProperty _autoScanProp;

    private LevelService _levelService;
    private string[] _presetNames;
    private int _selectedPresetIndex = 0;

    private void OnEnable() {
      _configSourceProp = serializedObject.FindProperty("ConfigurationSource");
      _sceneNameProp = serializedObject.FindProperty("SceneName");
      _manualConfigProp = serializedObject.FindProperty("_manualConfig");
      _effectiveConfigProp = serializedObject.FindProperty("_effectiveConfig");
      _onSceneLoadEventsProp = serializedObject.FindProperty("OnSceneLoadEvents");
      _enableDebugLoggingProp = serializedObject.FindProperty("EnableDebugLogging");
      _autoScanProp = serializedObject.FindProperty("AutoScanForHandlers");

      _levelService = Services.Get<LevelService>();
      if (_levelService != null && _levelService.Levels != null) {
        _presetNames = new string[_levelService.Levels.Length];
        for (int i = 0; i < _levelService.Levels.Length; i++) {
          var cfg = _levelService.Levels[i];
          _presetNames[i] = $"{cfg.DisplayName} ({cfg.SceneName})";
        }
      }
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      EditorGUILayout.PropertyField(_configSourceProp, new GUIContent("Configuration Source"));
      EditorGUILayout.PropertyField(_sceneNameProp, new GUIContent("Scene Name"));

      var controller = (SceneController)target;
      // detect if config is manual or preset. By default, if scene name matches a preset, it's preset
      //var onGUIConfig = _manualConfigProp;

      //draws the popup with available preset LevelConfigs, and updates the editor fields if changed
      DrawLevelConfigPreset();

      EditorGUILayout.PropertyField(_effectiveConfigProp, new GUIContent("Config"), true);
      EditorGUILayout.Space();

      EditorGUILayout.PropertyField(_autoScanProp, new GUIContent("Scan for GameStateHandlers"), true);
      EditorGUILayout.PropertyField(_onSceneLoadEventsProp, new GUIContent("Raise GameEvent"), true);
      EditorGUILayout.Space();

      EditorGUILayout.PropertyField(_enableDebugLoggingProp, new GUIContent("Enable Logging"));

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawLevelConfigPreset() {
      if ((SceneController.ConfigSource)_configSourceProp.enumValueIndex == SceneController.ConfigSource.Preset) {
        // Preset selection
        if (_levelService != null && _levelService.Levels != null && _levelService.Levels.Length > 0) {
          int currentIndex = Mathf.Max(0, System.Array.FindIndex(_levelService.Levels, l => l.SceneName == _sceneNameProp.stringValue));
          _selectedPresetIndex = EditorGUILayout.Popup("Preset", currentIndex, _presetNames);
          if (currentIndex != _selectedPresetIndex) {
            UpdateSelectedConfig(_selectedPresetIndex);
          }
        }
        else {
          EditorGUILayout.HelpBox("No LevelService or LevelConfigs found.", MessageType.Warning);
        }
      }
    }

    private void UpdateSelectedConfig(int _selectedIndex) {
      if (_selectedIndex >= 0 && _selectedIndex < _levelService.Levels.Length) {
        var selectedConfig = _levelService.Levels[_selectedIndex];
        if (selectedConfig.SceneName != _sceneNameProp.stringValue) {
          _sceneNameProp.stringValue = selectedConfig.SceneName;

          var so = new SerializedObject(_levelService);
          var levelsProp = so.FindProperty("Levels");
          if (levelsProp != null && levelsProp.arraySize > _selectedIndex) {
            var selectedConfigProp = levelsProp.GetArrayElementAtIndex(_selectedIndex);
            _effectiveConfigProp.boxedValue = selectedConfigProp.boxedValue;
          }
        }
      }
    }

  }
}
#endif