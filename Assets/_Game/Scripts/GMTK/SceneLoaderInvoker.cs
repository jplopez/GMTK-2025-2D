using UnityEngine;

namespace GMTK {

  public class SceneLoaderInvoker : MonoBehaviour {

    [SerializeField] private SceneLoaderSO sceneLoader;

    public void InvokeLoad() {
      if(sceneLoader != null) sceneLoader.LoadScene();
    }

    public void QuitGame() {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }
  }
}