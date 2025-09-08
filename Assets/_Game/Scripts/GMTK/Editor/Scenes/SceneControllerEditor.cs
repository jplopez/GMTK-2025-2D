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
    private SerializedProperty _manualConfigProp;
    private SerializedProperty _effectiveConfigProp;
    private SerializedProperty _onSceneLoadEventsProp;
    private SerializedProperty _enableDebugLoggingProp;

    private LevelService _levelService;
    private string[] _presetNames;
    private int _selectedPresetIndex = 0;

    private void OnEnable() {
      _configSourceProp = serializedObject.FindProperty("ConfigurationSource");
      _manualConfigProp = serializedObject.FindProperty("_manualConfig");
      _effectiveConfigProp = serializedObject.FindProperty("_effectiveConfig");
      _onSceneLoadEventsProp = serializedObject.FindProperty("OnSceneLoadEvents");
      _enableDebugLoggingProp = serializedObject.FindProperty("EnableDebugLogging");

      _levelService = GetLevelService();
      if (_levelService != null && _levelService.Configurations != null) {
        _presetNames = new string[_levelService.Configurations.Length];
        for (int i = 0; i < _levelService.Configurations.Length; i++) {
          var cfg = _levelService.Configurations[i];
          _presetNames[i] = $"{cfg.ConfigName} ({cfg.SceneName})";
        }
      }
    }

    private LevelService GetLevelService() {
      if (ServiceLocator.IsInitialized) return ServiceLocator.Get<LevelService>();
      return Resources.Load<LevelService>("LevelService");
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      // Display current scene info
      DrawCurrentSceneInfo();

      EditorGUILayout.PropertyField(_configSourceProp, new GUIContent("Configuration Source"));

      var controller = (SceneController)target;

      //draws the popup with available preset LevelConfigs, and updates the editor fields if changed
      DrawLevelConfigPreset();

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

      // Button to auto-select config for current scene
      if (GUILayout.Button("Auto-Select", GUILayout.Width(80))) {
        AutoSelectConfigForCurrentScene(activeSceneName);
      }
      EditorGUILayout.EndHorizontal();

      // Show if there's a matching configuration
      var matchingConfig = _levelService?.GetConfigBySceneName(activeSceneName);
      if (matchingConfig != null) {
        EditorGUILayout.HelpBox($"Found configuration: {matchingConfig.ConfigName}", MessageType.Info);
      }
      else {
        EditorGUILayout.HelpBox("No configuration found for current scene", MessageType.Warning);
      }

      EditorGUILayout.Space();
    }

    private void AutoSelectConfigForCurrentScene(string sceneName) {
      if (_levelService?.Configurations != null) {
        for (int i = 0; i < _levelService.Configurations.Length; i++) {
          if (_levelService.Configurations[i].SceneName == sceneName) {
            _selectedPresetIndex = i;
            _configSourceProp.enumValueIndex = (int)SceneController.ConfigSource.Preset;
            UpdateSelectedConfig(i);
            serializedObject.ApplyModifiedProperties();
            break;
          }
        }
      }
    }

    private void DrawLevelConfigPreset() {
      if ((SceneController.ConfigSource)_configSourceProp.enumValueIndex == SceneController.ConfigSource.Preset) {
        // Preset selection
        if (_levelService != null && _levelService.Configurations != null && _levelService.Configurations.Length > 0) {
          // Get current scene name to find the matching config
          string activeSceneName = SceneManager.GetActiveScene().name;
          int currentIndex = System.Array.FindIndex(_levelService.Configurations, l => l.SceneName == activeSceneName);
          if (currentIndex < 0) currentIndex = 0;

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

    private void UpdateSelectedConfig(int selectedIndex) {
      if (selectedIndex >= 0 && selectedIndex < _levelService.Configurations.Length) {
        var selectedConfig = _levelService.Configurations[selectedIndex];

        // Update the effective config
        var so = new SerializedObject(_levelService);
        var levelsProp = so.FindProperty("Configurations");
        if (levelsProp != null && levelsProp.arraySize > selectedIndex) {
          var selectedConfigProp = levelsProp.GetArrayElementAtIndex(selectedIndex);
          _effectiveConfigProp.boxedValue = selectedConfigProp.boxedValue;
        }
      }
    }
  }
}
#endif