using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Ameba {

  [CustomEditor(typeof(AmebaStateMachine))]
  public class AmebaStateMachineEditor : Editor {

    public VisualTreeAsset m_InspectorUXML;

    public override VisualElement CreateInspectorGUI() {
      // Load the reference UXML.
      if (m_InspectorUXML == null)
        m_InspectorUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Game/Editor/AmebaStateMachine_UXML.uxml");

      // Instantiate the UXML.
      VisualElement myInspector = m_InspectorUXML.Instantiate();

      //// Get a reference to the default Inspector Foldout control.
      //VisualElement InspectorFoldout = myInspector.Q("Default_Inspector");

      //// Attach a default Inspector to the Foldout.
      //InspectorElement.FillDefaultInspector(InspectorFoldout, serializedObject, this);

      // Return the finished Inspector UI.
      return myInspector;
    }
  }
}