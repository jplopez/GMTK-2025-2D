#if UNITY_EDITOR
using GMTK;

[UnityEditor.CustomEditor(typeof(GridManager))]
public class GridManagerEditor : UnityEditor.Editor {
  public override void OnInspectorGUI() {
    DrawDefaultInspector();

    GridManager manager = (GridManager)target;

    UnityEditor.EditorGUILayout.Space();
    UnityEditor.EditorGUILayout.LabelField("Occupied Grid Cells", UnityEditor.EditorStyles.boldLabel);

    foreach (var view in manager.EditorGridView) {
      if(view.Element != null && view.Element.isActiveAndEnabled)
        UnityEditor.EditorGUILayout.LabelField($"Coord: {view.Coord}", $"Element: {view.Element.name}");
    }
  }
}
#endif