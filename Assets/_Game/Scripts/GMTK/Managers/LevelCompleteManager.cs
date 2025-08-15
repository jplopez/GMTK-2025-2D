using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK {
  public class LevelCompleteManager : MonoBehaviour {

    public GameContext Controller;

    public TMPro.TMP_Text ScoreText;

    private Animator _animator;

    protected int _currentScore = 0;

    private void Start() {
      if (Controller == null) {
        Debug.LogError("RollnSnap Controller is not assigned in LevelCompleteManager.");
      }
      if (!TryGetComponent(out _animator)) {
        Debug.LogWarning("No Animator component found on LevelCompleteManager.");
      }
      _currentScore = Controller.MarbleScoreKeeper.GetScore();
    }

    public void OnIntroFinished() {
      if (ScoreText != null) {
        //ScoreText.text = $"{_currentScore:D5}";
        ScoreText.gameObject.SetActive(true);
      }
      //TODO Trigger score animation , not implemented yet
      //if (_animator != null) {
      //  _animator.SetTrigger("ShowScore");
      //}
    }

    public void RetryLevel() {
      string currentScene = Controller.LevelSequence.CurrentScene;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in _levelSequence.");
        return;
      }
      Debug.Log($"Retrying level: {currentScene}");
      UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
    }

    public void NextLevel() {
      string currentScene = Controller.LevelSequence.CurrentScene;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in _levelSequence.");
        return;
      }
      string nextLevel = Controller.LevelSequence.GetNextLevel(currentScene);
      if (!string.IsNullOrEmpty(nextLevel)) {
        Debug.Log($"Loading next level: {nextLevel}");
        Controller.LevelSequence.SetCurrentScene(nextLevel);
        SceneManager.LoadScene(nextLevel);
      }
      else {
        Debug.Log("No more levels. Game complete!");
        // Handle end of game logic here, e.g., show credits or restart
      }
    }

  }

}