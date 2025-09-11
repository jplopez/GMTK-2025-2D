
using System.Collections.Generic;
using System.Linq;

namespace GMTK {

  public static class EditorUtils {

    public static string Path = "Assets/Scenes";

    public static string[] GetAllSceneNamesArray(bool includeNoneOption = true) {
      // Search only in the GMTK game folder
      var searchFolders = new[] { Path };
      var sceneGuids = UnityEditor.AssetDatabase.FindAssets("t:Scene", searchFolders);
      var sceneNames = new List<string>();
      if (includeNoneOption) sceneNames.Add("None");

      foreach (var guid in sceneGuids) {
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        sceneNames.Add(sceneName);
      }
      return sceneNames.ToArray();
    }

    public static List<string> GetAllSceneNamesList(bool includeNoneOption = true) => GetAllSceneNamesArray(includeNoneOption).ToList() as List<string>;

  }
}