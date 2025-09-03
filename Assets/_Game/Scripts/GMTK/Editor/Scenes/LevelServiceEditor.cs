#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GMTK {

  [CustomEditor(typeof(LevelService))]
  public class LevelServiceEditor : Editor {

    private SerializedProperty _levelsProperty;
    private ReorderableList _levelsList;
    private readonly Dictionary<string, bool> _foldoutStates = new();

    // Cached enums from services
    private string[] _gameStateNames;
    private string[] _gameEventTypeNames;
    private string[] _sceneTypeNames;

    private void OnEnable() {
      _levelsProperty = serializedObject.FindProperty("Levels");

      // Cache enum values
      CacheEnumValues();

      SetupReorderableList();
    }

    private void CacheEnumValues() {
      // Get GameStates from enum
      _gameStateNames = System.Enum.GetNames(typeof(GameStates));

      // Get GameEventType from enum  
      _gameEventTypeNames = System.Enum.GetNames(typeof(GameEventType));

      // Get SceneType from LevelService
      _sceneTypeNames = System.Enum.GetNames(typeof(SceneType));
    }

    private void SetupReorderableList() {
      _levelsList = new ReorderableList(serializedObject, _levelsProperty, true, true, true, true) {
        drawHeaderCallback = DrawListHeader,
        drawElementCallback = DrawListElement,
        elementHeightCallback = GetElementHeight,
        onAddCallback = OnAddElement,
        onRemoveCallback = OnRemoveElement
      };
    }

    public override void OnInspectorGUI() {
      serializedObject.Update();

      EditorGUILayout.Space();
      DrawServiceInfo();

      EditorGUILayout.Space();
      DrawStartEndInfo();


      EditorGUILayout.Space();
      DrawLevelManagement();

      EditorGUILayout.Space();
      _levelsList.DoLayoutList();

      EditorGUILayout.Space();
      DrawUtilityButtons();

      serializedObject.ApplyModifiedProperties();
    }

    private void DrawServiceInfo() {
      EditorGUILayout.LabelField("Level Service", EditorStyles.boldLabel);

      var levelService = target as LevelService;
      var levelCount = levelService.Levels?.Length ?? 0;
      var currentLevel = levelService.CurrentLevel;

      using (new EditorGUI.DisabledScope(true)) {
        EditorGUILayout.IntField("Total Levels", levelCount);
        EditorGUILayout.TextField("Current Scene", levelService.CurrentSceneName ?? "None");
        EditorGUILayout.IntField("Current Index", levelService.CurrentLevelIndex);
      }
        EditorGUILayout.LabelField("Current Level Configuration", EditorStyles.boldLabel);
      using (new EditorGUI.DisabledScope(true)) {
        if (currentLevel != null) {
          EditorGUILayout.TextField("Type", currentLevel.Type.ToString());
          EditorGUILayout.TextField("Initial State", currentLevel.InitialGameState.ToString());
        }
      }
    }

    private void DrawStartEndInfo() {
      var levelService = target as LevelService;
      var startLevelType = levelService.startLevelType;
      var startLevelProp = serializedObject.FindProperty("startLevelType");
      //EditorGUILayout.LabelField("Start Level", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(startLevelProp, new GUIContent("Source"));
      //start config
      switch (startLevelType) {
        case StartLevelTypes.SceneName:
          var startSceneNameProp = serializedObject.FindProperty("_startSceneName");
          EditorGUILayout.PropertyField(startSceneNameProp, new GUIContent("Scene Name"));
          break;
        case StartLevelTypes.FirstLevel:
          EditorGUILayout.LabelField("Scene Name", levelService.GetLevelConfig(0).SceneName);
          break;
        case StartLevelTypes.Config:
          var startLevelConfigProp = serializedObject.FindProperty("_startLevelconfig");
          EditorGUILayout.PropertyField(startLevelConfigProp, new GUIContent("LevelConfig"));
          var startLevelConfig = levelService.StartLevel;
          startLevelConfig = DrawLevelsDropDown("Level Config", SceneType.Start, startLevelConfig?.SceneName);
          if (startLevelConfig != null)
            levelService.SetStartLevel(startLevelConfig);
          break;      
      }

      var endLevelType = levelService.endLevelType;
      var endLevelProp = serializedObject.FindProperty("endLevelType");
      //EditorGUILayout.LabelField("Start Level", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(endLevelProp, new GUIContent("Source"));
      //start config
      switch (endLevelType) {
        case EndLevelTypes.SceneName:
          var endSceneNameProp = serializedObject.FindProperty("_endSceneName");
          EditorGUILayout.PropertyField(endSceneNameProp, new GUIContent("Scene Name"));
          break;
        case EndLevelTypes.LastLevel:
          int lastIndex = levelService.Levels.Length - 1;
          EditorGUILayout.LabelField("Scene Name", levelService.GetLevelConfig(lastIndex).SceneName);
          break;
        case EndLevelTypes.Config:
          var endLevelConfig = levelService.EndLevel;
          endLevelConfig = DrawLevelsDropDown("Level Config",SceneType.End, endLevelConfig?.SceneName);
          if(endLevelConfig != null)
            levelService.SetEndLevel(endLevelConfig);
          break;
      }
    }

    private LevelConfig DrawLevelsDropDown(string label, SceneType type, string currentSelection=null) {
      var levelService = target as LevelService;
      var levelConfigs = levelService.Levels ?? new LevelConfig[0];
      var configNames = levelConfigs.Where(l => l.Type == type).Select(l => $"{l.DisplayName} ({l.SceneName})").ToArray();
      var currentIndex = 0;
      if(!string.IsNullOrEmpty(currentSelection))
        currentIndex = System.Array.FindIndex(levelConfigs, c => c.SceneName == currentSelection);

      EditorGUI.BeginChangeCheck();
      label = string.IsNullOrEmpty(label) ? "Select Configuration" : label;
      var newIndex = EditorGUILayout.Popup(label, currentIndex, configNames);
      if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < levelConfigs.Length) {
        currentSelection = levelConfigs[newIndex].SceneName;
        return RefreshSelectedConfig(levelService, currentSelection);
      }
      return levelService.GetLevelConfig(currentSelection);

    }

    private LevelConfig RefreshSelectedConfig(LevelService levelService, string currentSelection) {
      if (levelService != null && !string.IsNullOrEmpty(currentSelection)) {
       return levelService.GetLevelConfig(currentSelection);
      }
      return null;
    }

    private void DrawLevelManagement() {
      EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Auto-Detect Scenes", GUILayout.Height(25))) {
          AutoDetectScenes();
        }

        if (GUILayout.Button("Sort by Scene Name", GUILayout.Height(25))) {
          SortLevelsBySceneName();
        }

        if (GUILayout.Button("Validate All", GUILayout.Height(25))) {
          ValidateAllLevels();
        }
      }
    }

    private void DrawListHeader(Rect rect) {
      EditorGUI.LabelField(rect, "Level Configurations", EditorStyles.boldLabel);
    }

    private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused) {
      var element = _levelsProperty.GetArrayElementAtIndex(index);
      var foldoutKey = $"level_{index}";

      if (!_foldoutStates.ContainsKey(foldoutKey)) {
        _foldoutStates[foldoutKey] = false;
      }

      //padding the rectangle to avoid overlapping with reorder icon
      rect.x += 10;
      rect.y += 2;
      var headerRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);

      // Header with foldout
      var sceneName = element.FindPropertyRelative("SceneName").stringValue;
      var displayName = element.FindPropertyRelative("DisplayName").stringValue;
      var headerText = string.IsNullOrEmpty(displayName) ? $"Level {index}: {sceneName}" : $"Level {index}: {displayName}";

      _foldoutStates[foldoutKey] = EditorGUI.Foldout(headerRect, _foldoutStates[foldoutKey], headerText, true);

      if (_foldoutStates[foldoutKey]) {
        rect.y += EditorGUIUtility.singleLineHeight + 4;
        //DrawLevelConfigFields(rect, element, index);
        EditorGUI.PropertyField(rect, element, GUIContent.none);
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

    private float GetElementHeight(int index) {
      var foldoutKey = $"level_{index}";
      if (_foldoutStates.TryGetValue(foldoutKey, out var isExpanded) && isExpanded) {
        // Calculate height based on all fields
        var baseHeight = EditorGUIUtility.singleLineHeight + 4; // Header
        var fieldsHeight = 16 * EditorGUIUtility.singleLineHeight + 32; // All fields + spacing
        var sectionsHeight = 4 * EditorGUIUtility.singleLineHeight + 8; // Section headers
        return baseHeight + fieldsHeight + sectionsHeight;
      }
      return EditorGUIUtility.singleLineHeight + 4;
    }

    private void OnAddElement(ReorderableList list) {
      var index = list.serializedProperty.arraySize;
      list.serializedProperty.arraySize++;
      var element = list.serializedProperty.GetArrayElementAtIndex(index);

      // Set default values
      element.FindPropertyRelative("SceneName").stringValue = "";
      element.FindPropertyRelative("DisplayName").stringValue = $"New Level {index + 1}";
      element.FindPropertyRelative("Type").enumValueIndex = 1; // Level
      element.FindPropertyRelative("InitialGameState").enumValueIndex = 1; // Preparation
      element.FindPropertyRelative("SetStateOnLoad").boolValue = true;
      element.FindPropertyRelative("IsUnlocked").boolValue = true;
      element.FindPropertyRelative("CanRestart").boolValue = true;
      element.FindPropertyRelative("CanSkip").boolValue = false;
      element.FindPropertyRelative("LoadDelay").floatValue = 0f;
      element.FindPropertyRelative("UseCustomLoadMethod").boolValue = false;
      element.FindPropertyRelative("CustomLoadMethod").stringValue = "";
      element.FindPropertyRelative("NextSceneName").stringValue = "";
      element.FindPropertyRelative("PreviousSceneName").stringValue = "";
    }

    private void OnRemoveElement(ReorderableList list) {
      ReorderableList.defaultBehaviours.DoRemoveButton(list);
    }

    private void DrawUtilityButtons() {
      EditorGUILayout.LabelField("Development Tools", EditorStyles.boldLabel);

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Export Configuration", GUILayout.Height(30))) {
          ExportConfiguration();
        }

        if (GUILayout.Button("Import Configuration", GUILayout.Height(30))) {
          ImportConfiguration();
        }
      }

      using (new EditorGUILayout.HorizontalScope()) {
        if (GUILayout.Button("Reset All to Defaults", GUILayout.Height(25))) {
          if (EditorUtility.DisplayDialog("Reset Configuration",
              "This will reset all level configurations to default values. Are you sure?",
              "Reset", "Cancel")) {
            ResetAllToDefaults();
          }
        }
      }
    }

    private void AutoDetectScenes() {
      var levelService = target as LevelService;
      var scenes = GetAllSceneNames().Where(s => s != "None").ToArray();

      var newLevels = new List<LevelConfig>();

      // Keep existing configurations
      if (levelService.Levels != null) {
        newLevels.AddRange(levelService.Levels);
      }

      // Add new scenes
      foreach (var sceneName in scenes) {
        if (!newLevels.Any(l => l.SceneName == sceneName)) {
          newLevels.Add(new LevelConfig {
            SceneName = sceneName,
            DisplayName = sceneName,
            Type = SceneType.Level,
            InitialGameState = GameStates.Preparation,
            SetStateOnLoad = true,
            CanRestart = true,
            CanSkip = false
          });
        }
      }

      levelService.Levels = newLevels.ToArray();
      EditorUtility.SetDirty(target);

      Debug.Log($"[LevelServiceEditor] Auto-detected {scenes.Length} scenes, added {newLevels.Count - (levelService.Levels?.Length ?? 0)} new configurations");
    }

    private void SortLevelsBySceneName() {
      var levelService = target as LevelService;
      if (levelService.Levels != null) {
        System.Array.Sort(levelService.Levels, (a, b) => string.Compare(a.SceneName, b.SceneName));
        EditorUtility.SetDirty(target);
        Debug.Log("[LevelServiceEditor] Levels sorted by scene name");
      }
    }

    private void ValidateAllLevels() {
      var levelService = target as LevelService;
      var allScenes = GetAllSceneNames();
      var issues = new List<string>();

      if (levelService.Levels != null) {
        for (int i = 0; i < levelService.Levels.Length; i++) {
          var level = levelService.Levels[i];

          // Check if scene exists
          if (!allScenes.Contains(level.SceneName)) {
            issues.Add($"Level {i}: Scene '{level.SceneName}' not found");
          }

          // Check next scene reference
          if (!string.IsNullOrEmpty(level.NextSceneName) && !allScenes.Contains(level.NextSceneName)) {
            issues.Add($"Level {i}: Next scene '{level.NextSceneName}' not found");
          }

          // Check previous scene reference
          if (!string.IsNullOrEmpty(level.PreviousSceneName) && !allScenes.Contains(level.PreviousSceneName)) {
            issues.Add($"Level {i}: Previous scene '{level.PreviousSceneName}' not found");
          }
        }
      }

      if (issues.Count > 0) {
        var message = "Validation Issues Found:\n" + string.Join("\n", issues);
        EditorUtility.DisplayDialog("Validation Results", message, "OK");
        Debug.LogWarning($"[LevelServiceEditor] {issues.Count} validation issues found");
      }
      else {
        EditorUtility.DisplayDialog("Validation Results", "All levels validated successfully!", "OK");
        Debug.Log("[LevelServiceEditor] All levels validated successfully");
      }
    }

    private void ExportConfiguration() {
      // Implementation for exporting configuration to JSON/XML
      Debug.Log("[LevelServiceEditor] Export configuration - implement as needed");
    }

    private void ImportConfiguration() {
      // Implementation for importing configuration from JSON/XML
      Debug.Log("[LevelServiceEditor] Import configuration - implement as needed");
    }

    private void ResetAllToDefaults() {
      var levelService = target as LevelService;
      if (levelService.Levels != null) {
        foreach (var level in levelService.Levels) {
          level.InitialGameState = GameStates.Preparation;
          level.SetStateOnLoad = true;
          level.CanRestart = true;
          level.CanSkip = false;
          level.LoadDelay = 0f;
        }
        EditorUtility.SetDirty(target);
        Debug.Log("[LevelServiceEditor] All levels reset to defaults");
      }
    }
  }
}
#endif
