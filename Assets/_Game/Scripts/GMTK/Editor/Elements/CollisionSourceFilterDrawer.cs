#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace GMTK {

  [CustomPropertyDrawer(typeof(PhysicsElementComponent.CollisionSourceFilter))]
  public class CollisionSourceFilterDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      EditorGUI.BeginProperty(position, label, property);

      // Get the enum value
      PhysicsElementComponent.CollisionSourceFilter currentValue =
        (PhysicsElementComponent.CollisionSourceFilter)property.enumValueIndex;

      // Create display names for the dropdown
      string[] displayNames = new string[] {
        "Everything",
        "Marble Only",
        "Elements Only"
      };

      // Show the dropdown
      int selectedIndex = EditorGUI.Popup(position, label.text, property.enumValueIndex, displayNames);
      property.enumValueIndex = selectedIndex;

      EditorGUI.EndProperty();
    }
  }
}
#endif