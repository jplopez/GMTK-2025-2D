#if UNITY_EDITOR
using Ameba;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement; // Add this import

namespace GMTK {

  /// <summary>
  /// This custom editor will show the configuration found for the Scene based on LevelService, the overrides done in the specific SceneController, and the effective values, to validate the results
  /// </summary>
  [CustomEditor(typeof(SceneController), true)]
  public class SceneControllerEditor : Editor {

    private SerializedProperty _configSourceProp;
    private SerializedProperty _selectedConfigNameProp;
    private SerializedProperty _manualConfigProp;
    private SerializedProperty _effectiveConfigProp;
    private SerializedProperty _onSceneLoadEventsProp;
    private SerializedProperty _enableDebugLoggingProp;

    private LevelService _levelService;
    private string[] _presetNames;
    private int _selectedPresetIndex = 0;

    private void OnEnable() {
      SetupProperties();
      LoadConfigNames();
    }

    private void LoadConfigNames() {
      if(_levelService == null) _levelService = GetLevelService();
      if (_levelService != null && !_levelService.EmptyConfigs) {
        _presetNames = new string[_levelService.ConfigCount];
        for (int i = 0; i < _levelService.ConfigCount; i++) {
          var cfg = _levelService.LevelConfigAtIndex(i);
          _presetNames[i] = $"{cfg.ConfigName}";
          if (cfg.ConfigName == _selectedConfigNameProp.stringValue) {
            _selectedPresetIndex = i;
          }
        }
      }
    }

    private void SetupProperties() {
      _configSourceProp = serializedObject.FindProperty("ConfigurationSource");
      _selectedConfigNameProp = serializedObject.FindProperty("SelectedConfigName");
      _manualConfigProp = serializedObject.FindProperty("_manualConfig");
      _effectiveConfigProp = serializedObject.FindProperty("_effectiveConfig");
      _onSceneLoadEventsProp = serializedObject.FindProperty("OnSceneLoadEvents");
      _enableDebugLoggingProp = serializedObject.FindProperty("EnableDebugLogging");
    }

    private LevelService GetLevelService() {
      if (ServiceLocator.IsInitialized) return ServiceLocator.Get<LevelService>();
      return Resources.Load<LevelService>("LevelService");
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      // Display current scene info
      DrawCurrentSceneInfo();
      EditorGUILayout.Space();

      //draws the popup with available preset LevelConfigs, and updates the editor fields if changed
      EditorGUILayout.PropertyField(_configSourceProp, new GUIContent("Configuration Source"));
      DrawLevelConfig();

      EditorGUILayout.PropertyField(_effectiveConfigProp, new GUIContent("Config"), true);
      EditorGUILayout.Space();

      EditorGUILayout.PropertyField(_onSceneLoadEventsProp, new GUIContent("Raise GameEvent"), true);
      EditorGUILayout.Space();

      EditorGUILayout.PropertyField(_enableDebugLoggingProp, new GUIContent("Enable Logging"));

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawCurrentSceneInfo() {
      // Get the active scene name
      string activeSceneName = SceneManager.GetActiveScene().name;
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Current Scene:", EditorStyles.boldLabel, GUILayout.Width(100));
      EditorGUILayout.LabelField(activeSceneName);
      EditorGUILayout.EndHorizontal();
    }

    private void DrawLevelConfig() {
      SceneController.ConfigSource configSource = (SceneController.ConfigSource)_configSourceProp.enumValueIndex;
      if(_levelService == null) _levelService = GetLevelService();
      switch (configSource) {
        // If current config source is Preset, try to auto-select based on _presetConfig
        case SceneController.ConfigSource.Preset:
          DrawPresetLevelConfig(); break;
        case SceneController.ConfigSource.Manual:
          // If current config source is Manual, try to auto-select based on _manualConfig
          using (new EditorGUI.ChangeCheckScope()) {
            EditorGUILayout.LabelField("Manual Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_manualConfigProp, new GUIContent("Manual Config"), true);
            _effectiveConfigProp.boxedValue = _manualConfigProp.boxedValue;
          }
          break;
      }
    }

    private void DrawPresetLevelConfig() {
      var currentIndex = _selectedPresetIndex;
      _selectedPresetIndex = EditorGUILayout.Popup("Preset", currentIndex, _presetNames);
      //Debug.Log($"selectedPresetIndex {_selectedPresetIndex} - '{_presetNames[_selectedPresetIndex]}'");
      if (_selectedPresetIndex >= 0
          && _selectedPresetIndex < _levelService.ConfigCount
          && currentIndex != _selectedPresetIndex) {
        UpdateSelectedConfig(_selectedPresetIndex);
      }
    }

    private void UpdateSelectedConfig(int index) {
      // Update the effective config
      var configName = _presetNames[index];
      if (_levelService.TryFindConfig(configName, out var config)) {
        _effectiveConfigProp.boxedValue = config;
        _selectedConfigNameProp.stringValue = configName;
      } else {
        Debug.LogWarning($"UpdateSelectedConfig: could not find config for '{configName}' ");
      }
    }
  }
}
#endif