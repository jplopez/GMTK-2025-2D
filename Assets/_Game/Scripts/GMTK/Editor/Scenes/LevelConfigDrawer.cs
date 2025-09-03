#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GMTK {

  [CustomPropertyDrawer(typeof(LevelConfig))]
  public class LevelConfigDrawer : PropertyDrawer {

    private SerializedProperty sceneNameProp;
    private SerializedProperty displayNameProp;
    private SerializedProperty setStateOnLoadProp;
    private SerializedProperty initialGameStateProp;
    private SerializedProperty canRestartProp;
    private SerializedProperty canSkipProp;
    private SerializedProperty nextSceneNameProp;
    private SerializedProperty previousSceneNameProp;
    private SerializedProperty loadDelayProp;

    private static readonly GUIStyle _labelHeaderStyle = new() {
      fontSize = 12,
      fontStyle = FontStyle.Bold,
      normal = { textColor = EditorGUIUtility.isProSkin ? Color.lightCyan : Color.black },
    };


    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      LoadSerializedProperties(property);

      label ??= new GUIContent();
      label.text = NormalizeLabel(label);

      Rect pos = new(position.x, position.y + 5, position.width, position.height);
      EditorGUI.BeginProperty(pos, label, property);
      // Begin a vertical layout group to let Unity handle spacing
      EditorGUILayout.BeginVertical();
      EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

      // Draw all fields using EditorGUILayout
      EditorGUILayout.LabelField("Basic Info", _labelHeaderStyle);
      using (new EditorGUI.IndentLevelScope()) {
        EditorGUILayout.PropertyField(property.FindPropertyRelative("SceneName"));
        EditorGUILayout.PropertyField(property.FindPropertyRelative("DisplayName"));
        var sceneTypeProp = property.FindPropertyRelative("Type");
        SceneType selectedSceneType = (SceneType)EditorGUILayout.EnumPopup("Scene Type", (SceneType)property.FindPropertyRelative("Type").enumValueIndex);
        sceneTypeProp.enumValueIndex = (int)selectedSceneType;
      }

      EditorGUILayout.LabelField("Initial GameState", _labelHeaderStyle);
      using (new EditorGUI.IndentLevelScope()) {
        var setStateOnLoadToggle = EditorGUILayout.Toggle("Set GameState on Load", setStateOnLoadProp.boolValue);
        setStateOnLoadProp.boolValue = setStateOnLoadToggle;
        if (setStateOnLoadToggle) EditorGUILayout.PropertyField(initialGameStateProp);
      }

      EditorGUILayout.LabelField("Scene Management", _labelHeaderStyle);
      using (new EditorGUI.IndentLevelScope()) {
        canRestartProp.boolValue = EditorGUILayout.Toggle("Can Restart", canRestartProp.boolValue, GUILayout.Width(110f));
        canSkipProp.boolValue = EditorGUILayout.Toggle("Can Skip", canSkipProp.boolValue, GUILayout.Width(100f));
        DrawSceneDropdown(previousSceneNameProp, "Previous Scene");
        DrawSceneDropdown(nextSceneNameProp, "Next Scene");
        //EditorGUILayout.PropertyField(nextSceneNameProp);
        //EditorGUILayout.PropertyField(previousSceneNameProp);
        EditorGUILayout.PropertyField(loadDelayProp);
      }
      EditorGUILayout.EndVertical();
      EditorGUI.EndProperty();
    }

    private void LoadSerializedProperties(SerializedProperty property) {
      sceneNameProp = (property.FindPropertyRelative("SceneName"));
      displayNameProp = (property.FindPropertyRelative("DisplayName"));
      setStateOnLoadProp = property.FindPropertyRelative("SetStateOnLoad");
      initialGameStateProp = (property.FindPropertyRelative("InitialGameState"));
      canRestartProp = (property.FindPropertyRelative("CanRestart"));
      canSkipProp = (property.FindPropertyRelative("CanSkip"));
      nextSceneNameProp = (property.FindPropertyRelative("NextSceneName"));
      previousSceneNameProp = (property.FindPropertyRelative("PreviousSceneName"));
      loadDelayProp = (property.FindPropertyRelative("LoadDelay"));
      //useCustomLoadMethodProp = (property.FindPropertyRelative("UseCustomLoadMethod"));
      //customLoadMethodProp = (property.FindPropertyRelative("CustomLoadMethod"));
      //unlockConditionsProp = (property.FindPropertyRelative("UnlockConditions"));
      //isUnlockedProp = (property.FindPropertyRelative("IsUnlocked"));
    }

    private string NormalizeLabel(GUIContent label) {
      var nLabel = "";
      nLabel += string.IsNullOrEmpty(displayNameProp.stringValue) ? "LevelConfig " : $"{displayNameProp.stringValue} ";
      nLabel += string.IsNullOrEmpty(sceneNameProp.stringValue) ? $"(Scene missing)" : $"(Scene: {sceneNameProp.stringValue}) ";
      return nLabel;
    }

    private void DrawSceneDropdown(SerializedProperty sceneNameProp, string label) {

      // Get all scenes in build settings
      var scenes = EditorBuildSettings.scenes
        .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
        .ToList();
      // Insert "None" at the start
      scenes.Insert(0, "None");
      var scenesArray = scenes.ToArray();

      label = string.IsNullOrEmpty(label) ? "Scene Name" : label;

      // Determine current index (empty string means "None")
      int currentIndex = 0;
      if (!string.IsNullOrEmpty(sceneNameProp.stringValue)) {
        int foundIndex = scenes.FindIndex(s => s == sceneNameProp.stringValue);
        currentIndex = foundIndex >= 0 ? foundIndex : 0;
      }

      int newIndex = EditorGUILayout.Popup(label, currentIndex, scenesArray);

      if (newIndex == 0) {
        sceneNameProp.stringValue = string.Empty;
      }
      else if (newIndex >= 0 && newIndex < scenesArray.Length) {
        sceneNameProp.stringValue = scenesArray[newIndex];
      }
    }
  }
}
#endif
