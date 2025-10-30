#if UNITY_EDITOR
using DG.Tweening.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GMTK {

  [CustomEditor(typeof(LevelService))]
  public class LevelServiceEditor : UnityEditor.Editor {

    protected LevelService _targetService;

    private SerializedProperty _configurationsProperty;

    private SerializedProperty _startSceneConfigSourceProp;
    private SerializedProperty _startSceneNameProp;
    private SerializedProperty _startSceneConfigProp;

    private SerializedProperty _endSceneConfigSourceProp;
    private SerializedProperty _endSceneNameProp;
    private SerializedProperty _endSceneConfigProp;

    private SerializedProperty _gameLevelSceneNamesProp;
    private SerializedProperty _currentLevelIndexProp;

    private ReorderableList _configurationsList;
    private ReorderableList _gameLevelsList;
    private readonly Dictionary<string, bool> _foldoutStates = new();

    private int _startSceneIndex = 0;
    private int _endSceneIndex = 0;
    private int _startScenePresetIndex = 0;
    private int _endScenePresetIndex = 0;

    //cached lists for dropdowns
    private string[] _presetConfigNames;
    private string[] _sceneNames;

    private void OnEnable() {

      _targetService = target as LevelService;

      SetupProperties();

      SetupReorderableLists();
    }

    private void SetupReorderableLists() {
      // Configurations list
      _configurationsList = new ReorderableList(serializedObject, _configurationsProperty, true, true, true, true) {
        drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Level Configurations", EditorStyles.boldLabel),
        drawElementCallback = DrawConfigurationElement,
        elementHeightCallback = GetConfigurationElementHeight,
        onAddCallback = OnAddConfiguration,
        onRemoveCallback = OnRemoveConfiguration
      };

      // Game levels list
      _gameLevelsList = new ReorderableList(serializedObject, _gameLevelSceneNamesProp, true, true, true, true) {
        drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Game Level Order", EditorStyles.boldLabel),
        drawElementCallback = DrawGameLevelElement,
        elementHeightCallback = (index) => EditorGUIUtility.singleLineHeight + 4,
        onAddCallback = OnAddGameLevel,
        onRemoveCallback = OnRemoveGameLevel
      };
    }

    private void SetupProperties() {
      _configurationsProperty = serializedObject.FindProperty("Configurations");
      _startSceneConfigSourceProp = serializedObject.FindProperty("startSceneConfigSource");
      _startSceneNameProp = serializedObject.FindProperty("StartSceneName");
      _startSceneConfigProp = serializedObject.FindProperty("StartSceneConfig");
      _endSceneConfigSourceProp = serializedObject.FindProperty("endSceneConfigSource");
      _endSceneNameProp = serializedObject.FindProperty("EndSceneName");
      _endSceneConfigProp = serializedObject.FindProperty("EndSceneConfig");
      _gameLevelSceneNamesProp = serializedObject.FindProperty("_gameLevelSceneNames");
      _currentLevelIndexProp = serializedObject.FindProperty("_currentLevelIndex");

    }

    public override void OnInspectorGUI() {

      //these lists are used in dropdowns
      //so we populate them after all SerializedProperties are obtained
      _presetConfigNames = GetPresetConfigNames(_targetService, true);
      _sceneNames = EditorUtils.GetAllSceneNamesArray(true);

      serializedObject.Update();

      EditorGUILayout.Space();
      DrawServiceInfo();

      EditorGUILayout.Space();
      EditorGUILayout.HelpBox("Level configurations define the behaviour of the loaded scene (initial game state, load delay, etc). The SceneController instance in every scene declares what configuration is in use. Level configurations can be used in multiple scenes", MessageType.Info);
      _configurationsList.DoLayoutList();

      EditorGUILayout.Space();
      DrawStartEndScenes();

      EditorGUILayout.Space();
      DrawCurrentLevelInfo();

      EditorGUILayout.Space();
      EditorGUILayout.HelpBox("Game Level Order establishes the order of gameplay levels by specifying the Scene name. Scenes can repeat", MessageType.Info);
      _gameLevelsList.DoLayoutList();

      EditorGUILayout.Space();
      DrawUtilityButtons();

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawServiceInfo() {
      EditorGUILayout.LabelField("Level Service Overview", EditorStyles.boldLabel);

      var levelService = target as LevelService;
      var configCount = levelService.Configurations?.Length ?? 0;
      var gameLevelCount = _gameLevelSceneNamesProp.arraySize;

      using (new EditorGUI.DisabledScope(true)) {
        EditorGUILayout.IntField("Total Configurations", configCount);
        EditorGUILayout.IntField("Game Levels Count", gameLevelCount);
      }
    }

    #region Start/End Scenes

    public const string SCENE_NAME_LABEL = "Scene Name";
    public const string CONFIG_SOURCE_LABEL = "Config Source";
    public const string PRESET_LABEL = "Preset";
    public const string CONFIG_LABEL = "Config";

    private void DrawStartEndScenes() {
      UpdateSceneNameIndexes();
      UpdateConfigPresetIndexes();


      EditorGUILayout.LabelField("Start & End Scenes", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox("Define the scenes that will act as Start and End of the game. For each, you will also define the default level config", MessageType.Info);
      using (new EditorGUI.IndentLevelScope()) {

        // Start Scene
        DrawSpecialScene("Start Scene", 
          _startSceneConfigSourceProp, _startSceneNameProp, _startSceneConfigProp, 
          ref _startSceneIndex, ref _startScenePresetIndex);
        EditorGUILayout.Space(6);

        // End Scene
        DrawSpecialScene("End Scene", 
              _endSceneConfigSourceProp, _endSceneNameProp, _endSceneConfigProp, 
              ref _endSceneIndex, ref _endScenePresetIndex);
      } //indent
      serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpecialScene(string headerLabel, SerializedProperty configSourceProp, SerializedProperty sceneNameProp, SerializedProperty presetConfigProp, ref int currentSceneIndex, ref int currentPresetIndex) {
      EditorGUILayout.LabelField(headerLabel, EditorStyles.boldLabel);

      currentSceneIndex = DrawSceneNamePopup(sceneNameProp, SCENE_NAME_LABEL, currentSceneIndex);
      EditorGUILayout.PropertyField(configSourceProp, new GUIContent(CONFIG_SOURCE_LABEL));

      switch ((SceneConfigSource)configSourceProp.enumValueIndex) {
        case SceneConfigSource.Preset:
          currentPresetIndex = DrawPresetConfigurationsPopup(presetConfigProp, PRESET_LABEL, currentPresetIndex);
          EditorGUILayout.PropertyField(presetConfigProp, new GUIContent(CONFIG_LABEL));
          break;
        case SceneConfigSource.Manual:
          EditorGUILayout.PropertyField(presetConfigProp, new GUIContent(CONFIG_LABEL), true);
          break;
      }
      EditorGUILayout.Space();
    }

    private void UpdateConfigPresetIndexes() {
      if (_startSceneConfigProp != null) {
        _startScenePresetIndex = Array.IndexOf(_presetConfigNames, ((LevelConfig)_startSceneConfigProp.boxedValue).ConfigName);
      }
      if (_endSceneConfigProp != null) {
        _endScenePresetIndex = Array.IndexOf(_presetConfigNames, ((LevelConfig)_endSceneConfigProp.boxedValue).ConfigName);
      }
    }

    private void UpdateSceneNameIndexes() {
      if (_startSceneNameProp != null) {
        _startSceneIndex = Array.IndexOf(_sceneNames, _startSceneNameProp.stringValue);
      }
      if (_endSceneNameProp != null) {
        _endSceneIndex = Array.IndexOf(_sceneNames, _endSceneNameProp.stringValue);
      }
    }

    #endregion

    #region Level Order

    private void DrawCurrentLevelInfo() {
      var levelService = target as LevelService;
      EditorGUILayout.LabelField("Game Level", EditorStyles.boldLabel);

      using (new EditorGUI.DisabledScope(true)) {
        EditorGUILayout.LabelField("Current Level Scene", levelService.CurrentLevelSceneName ?? "None");
      }
    }

    private void DrawConfigurationElement(Rect rect, int index, bool isActive, bool isFocused) {
      var element = _configurationsProperty.GetArrayElementAtIndex(index);

      rect.x += 10;
      rect.y -= 4;

      // Header with foldout
      var configName = element.FindPropertyRelative("ConfigName").stringValue;
      //var sceneName = element.FindPropertyRelative("SceneName").stringValue;
      var headerText = string.IsNullOrEmpty(configName) ? $"Config {index}" : $"{configName}";
      EditorGUI.PropertyField(rect, element, new GUIContent(headerText), true);
    }

    private float GetConfigurationElementHeight(int index) {
      var element = _configurationsProperty.GetArrayElementAtIndex(index);
      var isExpandedProp = element.FindPropertyRelative("isExpanded");
      if (isExpandedProp != null && isExpandedProp.boolValue) {
        // If using the isExpanded property from LevelConfig
        var baseHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Header
        var fieldsHeight = 14 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing); // Estimated fields + spacing
        return baseHeight + fieldsHeight;
      }
      else {
        return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
      }
    }

    private void DrawGameLevelElement(Rect rect, int index, bool isActive, bool isFocused) {
      rect.x += 10;
      rect.y += 2;
      rect.height = EditorGUIUtility.singleLineHeight;

      var element = _gameLevelSceneNamesProp.GetArrayElementAtIndex(index);
      EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 80, rect.height), element, GUIContent.none);
    }

    #endregion

    #region Game Scenes

    private int DrawSceneNamePopup(SerializedProperty sceneNameProp, string label, int currentIndex) {
      var scenes = EditorUtils.GetAllSceneNamesArray();

      var selectedIndex = EditorGUILayout.Popup(label, currentIndex, scenes);
      if (selectedIndex > 0 && selectedIndex < scenes.Length) {
        sceneNameProp.boxedValue = scenes[selectedIndex];
      }
      serializedObject.ApplyModifiedProperties();
      return selectedIndex;
    }

    #endregion

    #region Preset Configs

    private int DrawPresetConfigurationsPopup(SerializedProperty levelConfigProp, string label, int currentPresetIndex) {

      //draw UI popup and capture selection
      var selectedIndex = EditorGUILayout.Popup(label, currentPresetIndex, _presetConfigNames);
      //Debug.Log($"currentIndex {currentPresetIndex}");
      //Debug.Log($"selectedIndex {selectedIndex}");
      if (selectedIndex >= 0 && selectedIndex < _presetConfigNames.Length) {
        //update levelConfigProp based on selection
        if (selectedIndex != currentPresetIndex) {

          //var newConfigName = _presetConfigNames[selectedIndex];
          UpdatePresetConfig(levelConfigProp, selectedIndex);
          serializedObject.ApplyModifiedProperties();
          return selectedIndex;
        }
      }
      return currentPresetIndex;
    }

    private void UpdatePresetConfig(SerializedProperty levelConfigProp, int presetIndex) {
      var configName = _presetConfigNames[presetIndex];
      if (_targetService.TryFindConfig(configName, out var config)) {
        levelConfigProp.boxedValue = config;
        //Debug.Log($"UpdatePresetConfig: config preset: '{configName}'");
        serializedObject.ApplyModifiedProperties();
      }
      else {
        //Debug.LogWarning($"UpdatePresetConfig: No config found with name '{configName}'");
      }
    }

    private string[] GetPresetConfigNames(LevelService levelService, bool forceLoad = false) {
      if (forceLoad || _presetConfigNames == null || _presetConfigNames.Length == 0) {
        _presetConfigNames = new string[levelService.ConfigCount];
        for (int i = 0; i < levelService.ConfigCount; i++) {
          var cfg = levelService.LevelConfigAtIndex(i);
          _presetConfigNames[i] = $"{cfg.ConfigName}";
        }
      }
      return _presetConfigNames;
    }

    #endregion

    #region Reordenable Lists Callbacks

    private void OnAddConfiguration(ReorderableList list) {
      var index = list.serializedProperty.arraySize;
      list.serializedProperty.arraySize++;
      var element = list.serializedProperty.GetArrayElementAtIndex(index);

      // Set default values
      element.FindPropertyRelative("ConfigName").stringValue = $"New Config {index + 1}";
      element.FindPropertyRelative("SetStateOnLoad").boolValue = true;
      element.FindPropertyRelative("InitialGameState").enumValueIndex = 1; // Preparation
      element.FindPropertyRelative("CanRestart").boolValue = true;
      element.FindPropertyRelative("CanSkip").boolValue = false;
      element.FindPropertyRelative("LoadDelay").floatValue = 0f;
      element.FindPropertyRelative("HasLevelCompleteScene").boolValue = true;
      element.FindPropertyRelative("LevelCompleteSceneName").stringValue = "LevelComplete";
    }

    private void OnRemoveConfiguration(ReorderableList list) {
      ReorderableList.defaultBehaviours.DoRemoveButton(list);
    }

    private void OnAddGameLevel(ReorderableList list) {
      var index = list.serializedProperty.arraySize;
      list.serializedProperty.arraySize++;
      var element = list.serializedProperty.GetArrayElementAtIndex(index);
      element.stringValue = "";
    }

    private void OnRemoveGameLevel(ReorderableList list) {
      ReorderableList.defaultBehaviours.DoRemoveButton(list);
    }

    #endregion

    #region Utility buttons

    private void DrawUtilityButtons() {
      EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Auto-Detect Scenes", GUILayout.Height(25))) {
          AutoDetectScenes();
        }

        if (GUILayout.Button("Sort by Scene Name", GUILayout.Height(25))) {
          SortConfigurationsByName();
        }

        if (GUILayout.Button("Validate All", GUILayout.Height(25))) {
          ValidateConfiguration();
        }
      }

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Sync Game Levels", GUILayout.Height(25))) {
          SyncGameLevelsWithConfigurations();
        }
      }
    }

    private void AutoDetectScenes() {
      var levelService = target as LevelService;
      string[] scenes = EditorUtils.GetAllSceneNamesArray(false);
      var newConfigs = new List<LevelConfig>();

      // Keep existing configurations
      if (levelService.Configurations != null) {
        newConfigs.AddRange(levelService.Configurations);
      }

      //// Add new scenes
      //foreach (var sceneName in scenes) {
      //  if (!newConfigs.Any(l => l.SceneName == sceneName)) {
      //    newConfigs.Add(new LevelConfig {
      //      Preset = sceneName,
      //      SceneName = sceneName,
      //      SetStateOnLoad = true,
      //      InitialGameState = GameStates.Preparation,
      //      CanRestart = true,
      //      CanSkip = false,
      //      LoadDelay = 0f,
      //      HasLevelCompleteScene = true,
      //      LevelCompleteSceneName = "LevelComplete"
      //    });
      //  }
      //}

      levelService.Configurations = newConfigs.ToArray();
      EditorUtility.SetDirty(target);

      Debug.Log($"[LevelServiceEditor] Auto-detected {scenes.Length} scenes, added {newConfigs.Count - (levelService.Configurations?.Length ?? 0)} new configurations");
    }

    private void SortConfigurationsByName() {
      var levelService = target as LevelService;
      if (levelService.Configurations != null) {
        System.Array.Sort(levelService.Configurations, (a, b) => string.Compare(a.ConfigName, b.ConfigName));
        EditorUtility.SetDirty(target);
        Debug.Log("[LevelServiceEditor] Configurations sorted by name");
      }
    }

    private void SyncGameLevelsWithConfigurations() {
      var levelService = target as LevelService;
      if (levelService.Configurations != null) {
        var gameScenes = levelService.Configurations
          .Where(c => !string.IsNullOrEmpty(c.ConfigName))
          .Select(c => c.ConfigName)
          .ToList();

        _gameLevelSceneNamesProp.ClearArray();
        for (int i = 0; i < gameScenes.Count; i++) {
          _gameLevelSceneNamesProp.InsertArrayElementAtIndex(i);
          _gameLevelSceneNamesProp.GetArrayElementAtIndex(i).stringValue = gameScenes[i];
        }

        EditorUtility.SetDirty(target);
        Debug.Log($"[LevelServiceEditor] Synced {gameScenes.Count} game levels");
      }
    }

    private void ValidateConfiguration() {
      var levelService = target as LevelService;
      var allScenes = EditorUtils.GetAllSceneNamesArray();
      var issues = new List<string>();

      // Validate configurations
      if (!levelService.EmptyConfigs) {
        for (int i = 0; i < levelService.ConfigCount; i++) {
          var config = levelService.Configurations[i];
          if (string.IsNullOrEmpty(config.ConfigName)) {
            issues.Add($"Config {i}: Missing config name");
          }
          if (config.HasLevelCompleteScene && string.IsNullOrEmpty(config.LevelCompleteSceneName)) {
            issues.Add($"Config {i} ({config.ConfigName}): HasLevelCompleteScene is true but LevelCompleteSceneName is empty");
          }
          if (config.LoadDelay < 0f) {
            issues.Add($"Config {i} ({config.ConfigName}): LoadDelay is negative");
          }
        }
      }

      // Validate game levels
      for (int i = 0; i < _gameLevelSceneNamesProp.arraySize; i++) {
        var sceneName = _gameLevelSceneNamesProp.GetArrayElementAtIndex(i).stringValue;
        if (string.IsNullOrEmpty(sceneName)) {
          issues.Add($"Game Level {i}: missing scene name");
        }
        else if (!allScenes.Contains(sceneName)) {
          issues.Add($"Game Level {i}: Scene '{sceneName}' not found");
        }
      }

      if (issues.Count > 0) {
        var message = "Validation Issues Found:\n" + string.Join("\n", issues);
        EditorUtility.DisplayDialog("Validation Results", message, "OK");
        Debug.LogWarning($"[LevelServiceEditor] {issues.Count} validation issues found");
      }
      else {
        EditorUtility.DisplayDialog("Validation Results", "All configurations validated successfully!", "OK");
        Debug.Log("[LevelServiceEditor] All configurations validated successfully");
      }
    }

    #endregion

  }
}
#endif