#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GMTK {

  [CustomPropertyDrawer(typeof(LevelConfig))]
  public class LevelConfigDrawer : PropertyDrawer {

    private SerializedProperty configNameProp;
    //private SerializedProperty sceneNameProp;
    private SerializedProperty setStateOnLoadProp;
    private SerializedProperty initialGameStateProp;
    private SerializedProperty canRestartProp;
    private SerializedProperty canSkipProp;
    private SerializedProperty loadDelayProp;
    private SerializedProperty hasLevelCompleteSceneProp;
    private SerializedProperty levelCompleteSceneNameProp;

    private static readonly GUIStyle _labelHeaderStyle = new() {
      fontSize = 12,
      fontStyle = FontStyle.Bold,
      normal = { textColor = EditorGUIUtility.isProSkin ? Color.lightCyan : Color.black },
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      using (new EditorGUI.PropertyScope(position, label, property)) {
        EditorGUI.BeginProperty(position, label, property);

        // Find the 'isExpanded' property
        SerializedProperty isExpandedProp = property.FindPropertyRelative("isExpanded");


        LoadSerializedProperties(property);

        label ??= new GUIContent();
        label.text = NormalizeLabel(label);

        Rect foldoutRect = new(position.x, position.y + 5, position.width, EditorGUIUtility.singleLineHeight);

        // Draw the foldout and update its state
        isExpandedProp.boolValue = EditorGUI.Foldout(foldoutRect, isExpandedProp.boolValue, label);

        if (isExpandedProp.boolValue) {

          using (new EditorGUI.IndentLevelScope()) {

            foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(foldoutRect, label, EditorStyles.boldLabel);

            // Basic Info Section
            foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(foldoutRect, "Basic Info", _labelHeaderStyle);
            using (new EditorGUI.IndentLevelScope()) {
              foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
              EditorGUI.PropertyField(foldoutRect, configNameProp);
              //foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
              //DrawSceneDropdown(foldoutRect, sceneNameProp, "Scene Name");
            }

            // Game State Section
            foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(foldoutRect, "Initial GameState", _labelHeaderStyle);
            using (new EditorGUI.IndentLevelScope()) {
              foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
              var setStateOnLoadToggle = EditorGUI.Toggle(foldoutRect, "Set GameState on Load", setStateOnLoadProp.boolValue);
              setStateOnLoadProp.boolValue = setStateOnLoadToggle;
              if (setStateOnLoadToggle) {
                foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(foldoutRect, initialGameStateProp);
              }
            }

            // Scene Management Section
            foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(foldoutRect, "Scene Management", _labelHeaderStyle);
            using (new EditorGUI.IndentLevelScope()) {
              foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
              EditorGUI.PropertyField(foldoutRect, canRestartProp);
              foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
              EditorGUI.PropertyField(foldoutRect, canSkipProp);
              foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
              EditorGUI.PropertyField(foldoutRect, loadDelayProp);
            }

            // Level Complete Section
            foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.LabelField(foldoutRect, "Level Complete", _labelHeaderStyle);
            using (new EditorGUI.IndentLevelScope()) {
              foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
              EditorGUI.PropertyField(foldoutRect, hasLevelCompleteSceneProp);
              if (hasLevelCompleteSceneProp.boolValue) {
                foldoutRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(foldoutRect, levelCompleteSceneNameProp);
              }
            }
          }

        }

        EditorGUI.EndProperty();
      }
    }

    private void LoadSerializedProperties(SerializedProperty property) {
      configNameProp = property.FindPropertyRelative("ConfigName");
      //sceneNameProp = property.FindPropertyRelative("SceneName");
      setStateOnLoadProp = property.FindPropertyRelative("SetStateOnLoad");
      initialGameStateProp = property.FindPropertyRelative("InitialGameState");
      canRestartProp = property.FindPropertyRelative("CanRestart");
      canSkipProp = property.FindPropertyRelative("CanSkip");
      loadDelayProp = property.FindPropertyRelative("LoadDelay");
      hasLevelCompleteSceneProp = property.FindPropertyRelative("HasLevelCompleteScene");
      levelCompleteSceneNameProp = property.FindPropertyRelative("LevelCompleteSceneName");
    }

    private string NormalizeLabel(GUIContent label) {
      var configName = configNameProp?.stringValue ?? "";
      //var sceneName = sceneNameProp?.stringValue ?? "";

      string displayText = string.IsNullOrEmpty(configName) ? "LevelConfig" : configName;
      //string sceneText = string.IsNullOrEmpty(sceneName) ? "(Scene missing)" : $"(Scene: {sceneName})";

      return $"{displayText}";
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
      // Calculate the height needed for the drawer
      SerializedProperty isExpandedProperty = property.FindPropertyRelative("isExpanded");
      if (isExpandedProperty.boolValue) {
        // Height for foldout + name + value
        return EditorGUIUtility.singleLineHeight * 15 + EditorGUIUtility.standardVerticalSpacing * 14;
      }
      else {
        // Height for foldout only
        return EditorGUIUtility.singleLineHeight;
      }
    }
  }
}
#endif