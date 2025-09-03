#if UNITY_EDITOR
using Ameba;
using UnityEditor;
using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This custom editor will show the configuration found for the Scene based on LevelService, the overrides done in the specific SceneController, and the effective values, to validate the results
  /// </summary>
  [CustomEditor(typeof(SceneTransitionConfig), true)]
  public class SceneTransitionConfigEditor : Editor {

    private SerializedProperty _transitionConfigProp;
    private SerializedProperty _transitionSceneNameProp;

    private LevelService _levelService;
    private string[] _presetNames;
    private int _selectedPresetIndex = 0;

    private void OnEnable() {
      _transitionConfigProp = serializedObject.FindProperty("TransitionConfig");
      _transitionSceneNameProp = serializedObject.FindProperty("TransitionSceneName");

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

      EditorGUILayout.PropertyField(_transitionSceneNameProp, new GUIContent("Transition Scene Name"));
      // Preset selection
      if (EnsureLevelService()) {
        int currentIndex = ResolveCurrentConfigIndex();
        _selectedPresetIndex = EditorGUILayout.Popup("Preset", currentIndex, _presetNames);

        if (_selectedPresetIndex >= 0 && _selectedPresetIndex < _levelService.Levels.Length) {
          // Obtain selected preset as SerializedProperty to draw in editor
          var selectedConfig = _levelService.Levels[_selectedPresetIndex];
          _transitionSceneNameProp.stringValue = selectedConfig.SceneName;
          var so = new SerializedObject(_levelService);
          var levelsProp = so.FindProperty("Levels");

          if (levelsProp != null && levelsProp.arraySize > _selectedPresetIndex) {
            _transitionConfigProp = levelsProp.GetArrayElementAtIndex(_selectedPresetIndex);
          }
        }
      }
      else {
        EditorGUILayout.HelpBox("No LevelService or LevelConfigs found.", MessageType.Warning);
      }

      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_transitionConfigProp, true);
      }

      serializedObject.ApplyModifiedProperties();
    }

    private bool EnsureLevelService() {
      return _levelService != null && _levelService.Levels != null && _levelService.Levels.Length > 0;
    }

    private int ResolveCurrentConfigIndex() {
      return Mathf.Max(0, System.Array.FindIndex(_levelService.Levels, l => l.SceneName == _transitionSceneNameProp.stringValue));
    }
  }



}
#endif