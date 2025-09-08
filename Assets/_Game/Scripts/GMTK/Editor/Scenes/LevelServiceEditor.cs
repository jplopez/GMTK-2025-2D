#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GMTK {

  [CustomEditor(typeof(LevelService))]
  public class LevelServiceEditor : Editor {

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

    private void OnEnable() {
      _configurationsProperty = serializedObject.FindProperty("Configurations");
      _startSceneConfigSourceProp = serializedObject.FindProperty("startSceneConfigSource");
      _startSceneNameProp = serializedObject.FindProperty("_startSceneName");
      _startSceneConfigProp = serializedObject.FindProperty("_startSceneConfig");
      _endSceneConfigSourceProp = serializedObject.FindProperty("endSceneConfigSource");
      _endSceneNameProp = serializedObject.FindProperty("_endSceneName");
      _endSceneConfigProp = serializedObject.FindProperty("_endSceneConfig");
      _gameLevelSceneNamesProp = serializedObject.FindProperty("_gameLevelSceneNames");
      _currentLevelIndexProp = serializedObject.FindProperty("_currentLevelIndex");

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

    public override void OnInspectorGUI() {
      serializedObject.Update();

      EditorGUILayout.Space();
      DrawServiceInfo();

      EditorGUILayout.Space();
      DrawStartEndScenes();

      EditorGUILayout.Space();
      DrawCurrentLevelInfo();

      EditorGUILayout.Space();
      _configurationsList.DoLayoutList();

      EditorGUILayout.Space();
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

    private void DrawStartEndScenes() {
      EditorGUILayout.LabelField("Start & End Scenes", EditorStyles.boldLabel);

      // Start Scene
      EditorGUILayout.LabelField("Start Scene", EditorStyles.miniBoldLabel);
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_startSceneConfigSourceProp, new GUIContent("Source"));

        switch ((SceneConfigSource)_startSceneConfigSourceProp.enumValueIndex) {
          case SceneConfigSource.ConfigName:
            DrawSceneNameField(_startSceneNameProp, "Scene Name");
            break;
          case SceneConfigSource.Manual:
            EditorGUILayout.PropertyField(_startSceneConfigProp, new GUIContent("Config"));
            break;
        }
      }

      EditorGUILayout.Space();

      // End Scene
      EditorGUILayout.LabelField("End Scene", EditorStyles.miniBoldLabel);
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(_endSceneConfigSourceProp, new GUIContent("Source"));

        switch ((SceneConfigSource)_endSceneConfigSourceProp.enumValueIndex) {
          case SceneConfigSource.ConfigName:
            DrawSceneNameField(_endSceneNameProp, "Scene Name");
            break;
          case SceneConfigSource.Manual:
            EditorGUILayout.PropertyField(_endSceneConfigProp, new GUIContent("Config"));
            break;
        }
      }
    }

    private void DrawCurrentLevelInfo() {
      EditorGUILayout.LabelField("Current Level State", EditorStyles.boldLabel);

      var levelService = target as LevelService;

      EditorGUILayout.PropertyField(_currentLevelIndexProp, new GUIContent("Current Level Index"));

      using (new EditorGUI.DisabledScope(true)) {
        EditorGUILayout.TextField("Current Scene", levelService.CurrentLevelSceneName ?? "None");

        var currentConfig = levelService.CurrentLevelConfig;
        if (currentConfig != null) {
          EditorGUILayout.TextField("Config Name", currentConfig.ConfigName);
          EditorGUILayout.EnumPopup("Initial State", currentConfig.InitialGameState);
        }
      }
    }

    private void DrawConfigurationElement(Rect rect, int index, bool isActive, bool isFocused) {
      var element = _configurationsProperty.GetArrayElementAtIndex(index);

      rect.x += 10;
      rect.y -= 4;

      // Header with foldout
      var configName = element.FindPropertyRelative("ConfigName").stringValue;
      var sceneName = element.FindPropertyRelative("SceneName").stringValue;
      var headerText = string.IsNullOrEmpty(configName) ? $"Config {index}: {sceneName}" : $"{configName} ({sceneName})";
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
      var levelService = target as LevelService;

      using (new EditorGUILayout.HorizontalScope()) {
        EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width - 80, rect.height), element, GUIContent.none);

        // Show if this scene has a configuration
        var hasConfig = levelService.Configurations?.Any(c => c.SceneName == element.stringValue) ?? false;
        var configStatus = hasConfig ? "✓" : "✗";
        var configColor = hasConfig ? Color.green : Color.red;

        var oldColor = GUI.color;
        GUI.color = configColor;
        EditorGUI.LabelField(new Rect(rect.x + rect.width - 75, rect.y, 20, rect.height), configStatus);
        GUI.color = oldColor;

        EditorGUI.LabelField(new Rect(rect.x + rect.width - 55, rect.y, 55, rect.height), "Config", EditorStyles.miniLabel);
      }
    }

    private void DrawSceneNameField(SerializedProperty sceneNameProp, string label) {
      var scenes = GetAllSceneNames();
      var currentIndex = System.Array.IndexOf(scenes, sceneNameProp.stringValue);
      if (currentIndex < 0) currentIndex = 0;

      var newIndex = EditorGUILayout.Popup(label, currentIndex, scenes);
      if (newIndex > 0 && newIndex < scenes.Length) {
        sceneNameProp.stringValue = scenes[newIndex];
      }
      else {
        sceneNameProp.stringValue = "";
      }
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

    private void OnAddConfiguration(ReorderableList list) {
      var index = list.serializedProperty.arraySize;
      list.serializedProperty.arraySize++;
      var element = list.serializedProperty.GetArrayElementAtIndex(index);

      // Set default values
      element.FindPropertyRelative("ConfigName").stringValue = $"New Config {index + 1}";
      element.FindPropertyRelative("SceneName").stringValue = "";
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

    private void DrawUtilityButtons() {
      EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Auto-Detect Scenes", GUILayout.Height(25))) {
          AutoDetectScenes();
        }

        if (GUILayout.Button("Sort by Scene Name", GUILayout.Height(25))) {
          SortConfigurationsBySceneName();
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
      var scenes = GetAllSceneNames().Where(s => s != "None").ToArray();
      var newConfigs = new List<LevelConfig>();

      // Keep existing configurations
      if (levelService.Configurations != null) {
        newConfigs.AddRange(levelService.Configurations);
      }

      // Add new scenes
      foreach (var sceneName in scenes) {
        if (!newConfigs.Any(l => l.SceneName == sceneName)) {
          newConfigs.Add(new LevelConfig {
            ConfigName = sceneName,
            SceneName = sceneName,
            SetStateOnLoad = true,
            InitialGameState = GameStates.Preparation,
            CanRestart = true,
            CanSkip = false,
            LoadDelay = 0f,
            HasLevelCompleteScene = true,
            LevelCompleteSceneName = "LevelComplete"
          });
        }
      }

      levelService.Configurations = newConfigs.ToArray();
      EditorUtility.SetDirty(target);

      Debug.Log($"[LevelServiceEditor] Auto-detected {scenes.Length} scenes, added {newConfigs.Count - (levelService.Configurations?.Length ?? 0)} new configurations");
    }

    private void SortConfigurationsBySceneName() {
      var levelService = target as LevelService;
      if (levelService.Configurations != null) {
        System.Array.Sort(levelService.Configurations, (a, b) => string.Compare(a.SceneName, b.SceneName));
        EditorUtility.SetDirty(target);
        Debug.Log("[LevelServiceEditor] Configurations sorted by scene name");
      }
    }

    private void SyncGameLevelsWithConfigurations() {
      var levelService = target as LevelService;
      if (levelService.Configurations != null) {
        var gameScenes = levelService.Configurations
          .Where(c => !string.IsNullOrEmpty(c.SceneName))
          .Select(c => c.SceneName)
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
      var allScenes = GetAllSceneNames();
      var issues = new List<string>();

      // Validate configurations
      if (levelService.Configurations != null) {
        for (int i = 0; i < levelService.Configurations.Length; i++) {
          var config = levelService.Configurations[i];

          if (string.IsNullOrEmpty(config.SceneName)) {
            issues.Add($"Config {i}: Missing scene name");
          }
          else if (!allScenes.Contains(config.SceneName)) {
            issues.Add($"Config {i}: Scene '{config.SceneName}' not found");
          }

          if (string.IsNullOrEmpty(config.ConfigName)) {
            issues.Add($"Config {i}: Missing config name");
          }
        }
      }

      // Validate game levels
      for (int i = 0; i < _gameLevelSceneNamesProp.arraySize; i++) {
        var sceneName = _gameLevelSceneNamesProp.GetArrayElementAtIndex(i).stringValue;
        if (string.IsNullOrEmpty(sceneName)) {
          issues.Add($"Game Level {i}: Empty scene name");
        }
        else if (!allScenes.Contains(sceneName)) {
          issues.Add($"Game Level {i}: Scene '{sceneName}' not found");
        }
        else if (levelService.Configurations?.All(c => c.SceneName != sceneName) ?? true) {
          issues.Add($"Game Level {i}: No configuration found for scene '{sceneName}'");
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
  }
}
#endif