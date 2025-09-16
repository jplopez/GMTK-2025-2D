#if UNITY_EDITOR
using UnityEditor;

namespace GMTK {
  [CustomEditor(typeof(PlayableElement))]
  public class PlayableElementEditor : Editor {

    public override void OnInspectorGUI() {
      PlayableElement playableElement = (PlayableElement)target;

      // Get the physics component if it exists
      PlayableElementPhysics physicsComponent = playableElement.GetComponent<PlayableElementPhysics>();
      bool hasRotationOverride = physicsComponent != null && physicsComponent.ChangeRotationOnCollision;

      // Draw default inspector first
      serializedObject.Update();

      // Draw all properties except CanRotate
      DrawPropertiesExcluding(serializedObject, "CanRotate");

      // Draw CanRotate with special handling
      EditorGUI.BeginDisabledGroup(hasRotationOverride);

      SerializedProperty canRotateProp = serializedObject.FindProperty("CanRotate");
      EditorGUILayout.PropertyField(canRotateProp);

      EditorGUI.EndDisabledGroup();

      // Show info message if rotation is overridden
      if (hasRotationOverride) {
        EditorGUILayout.HelpBox(
          "Rotation is controlled by the PlayableElementPhysics component because 'ChangeRotationOnCollision' is enabled. " +
          "Adjust rotation settings in the Physics component instead.",
          MessageType.Info
        );
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
#endif