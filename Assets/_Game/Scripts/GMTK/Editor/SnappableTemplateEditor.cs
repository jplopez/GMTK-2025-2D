using GMTK;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SnappableTemplate))]
public class SnappableTemplateEditor : Editor {
  //private static readonly PivotDirectionTypes[,] grid = new PivotDirectionTypes[3, 3] {
  //      { PivotDirectionTypes.NorthWest, PivotDirectionTypes.North, PivotDirectionTypes.NorthEast },
  //      { PivotDirectionTypes.West,      PivotDirectionTypes.Center, PivotDirectionTypes.East },
  //      { PivotDirectionTypes.SouthWest, PivotDirectionTypes.South, PivotDirectionTypes.SouthEast }
  //  };

  public override void OnInspectorGUI() {
    SnappableTemplate template = (SnappableTemplate)target;

    //GUILayout.Label("PivotDirection Direction", EditorStyles.boldLabel);

    //for (int y = 0; y < 3; y++) {
    //  GUILayout.BeginHorizontal();
    //  for (int x = 0; x < 3; x++) {
    //    PivotDirectionTypes dir = grid[y, x];
    //    GUIStyle style = (template.PivotDirection == dir) ? EditorStyles.miniButtonMid : EditorStyles.miniButton;

    //    if (GUILayout.Button(dir.ToString().Substring(0, 2), style, GUILayout.Width(40), GUILayout.Height(40))) {
    //      template.PivotDirection = dir;
    //      EditorUtility.SetDirty(template);
    //    }
    //  }
    //  GUILayout.EndHorizontal();
    //}

    DrawDefaultInspector(); // Optional: draw other fields
  }

}