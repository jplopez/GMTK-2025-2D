using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK {
  [CreateAssetMenu(fileName = "SceneLoader", menuName = "GMTK/Scene Loader")]
  public class SceneLoaderSO : ScriptableObject {

    [SerializeField] private string sceneName;

    public void LoadScene() {
      if (!string.IsNullOrEmpty(sceneName)) {
        SceneManager.LoadScene(sceneName);
      }
      else {
        Debug.LogWarning("Scene name is empty!");
      }
    }
  }
}