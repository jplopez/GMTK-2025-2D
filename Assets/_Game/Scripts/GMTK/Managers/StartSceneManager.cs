using UnityEngine;

namespace GMTK {

  public class StartSceneManager : MonoBehaviour {

    public LevelSequence LevelSequence;

    private void Start() {
      // Optionally, you could load the first level automatically here
      // Uncomment the line below to load the first level when the start scene loads
      // UnityEngine.SceneManagement.RollnSnapController.LoadScene(FirstLevelSceneName);
      LevelSequence.CurrentScene = null; // Reset current scene on start


    }
    public void StartGame() {
      if (LevelSequence == null) {
        Debug.LogError("_levelSequence is not properly configured in StartSceneManager.");
        return;
      }
      string firstLevel = LevelSequence.LevelSceneNames[0];
      if (string.IsNullOrEmpty(firstLevel)) {
        Debug.LogError("First level scene name is empty in _levelSequence.");
        return;
      }
      LevelSequence.SetCurrentScene(firstLevel);
      Debug.Log($"Starting game. Loading first level: {firstLevel}");
      UnityEngine.SceneManagement.SceneManager.LoadScene(firstLevel);
    }
  }
}