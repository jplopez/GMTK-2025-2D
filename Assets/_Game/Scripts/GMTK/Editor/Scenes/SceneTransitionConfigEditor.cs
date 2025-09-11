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
    //private int _selectedPresetIndex = 0;

    private void OnEnable() {
      _transitionConfigProp = serializedObject.FindProperty("TransitionConfig");
      _transitionSceneNameProp = serializedObject.FindProperty("TransitionSceneName");

      _levelService = GetLevelService();
      if (_levelService != null && _levelService.Configurations != null) {
        _presetNames = new string[_levelService.Configurations.Length];
        for (int i = 0; i < _levelService.Configurations.Length; i++) {
          var cfg = _levelService.Configurations[i];
          _presetNames[i] = $"{cfg.ConfigName}";
        }
      }
    }

    private LevelService GetLevelService() {
      if (ServiceLocator.IsInitialized) return ServiceLocator.Get<LevelService>();
      return Resources.Load<LevelService>("LevelService");
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      EditorGUILayout.PropertyField(_transitionSceneNameProp, new GUIContent("Transition Scene Name"));
      // Preset selection
      //if (EnsureLevelService()) {
      //  int currentIndex = ResolveCurrentConfigIndex();
      //  _selectedPresetIndex = EditorGUILayout.Popup("Preset", currentIndex, _presetNames);

      //  if (_selectedPresetIndex >= 0 && _selectedPresetIndex < _levelService.Configurations.Length) {
      //    // Obtain selected preset as SerializedProperty to draw in editor
      //    var selectedConfig = _levelService.Configurations[_selectedPresetIndex];
      //    _transitionSceneNameProp.stringValue = selectedConfig.SceneName;
      //    var so = new SerializedObject(_levelService);
      //    var levelsProp = so.FindProperty("Configurations");

      //    if (levelsProp != null && levelsProp.arraySize > _selectedPresetIndex) {
      //      _transitionConfigProp = levelsProp.GetArrayElementAtIndex(_selectedPresetIndex);
      //    }
      //  }
      //}
      //else {
      //  EditorGUILayout.HelpBox("No LevelService or LevelConfigs found.", MessageType.Warning);
      //}

      if (_transitionConfigProp != null) {
        using (new EditorGUI.IndentLevelScope()) {
          EditorGUILayout.PropertyField(_transitionConfigProp, true);
        }
      }

      serializedObject.ApplyModifiedProperties();
    }

    private bool EnsureLevelService() {
      return _levelService != null && _levelService.Configurations != null && _levelService.Configurations.Length > 0;
    }

    private int ResolveCurrentConfigIndex() {
      return Mathf.Max(0, System.Array.FindIndex(_levelService.Configurations, l => l.ConfigName == _transitionSceneNameProp.stringValue));
    }
  }



}
#endif