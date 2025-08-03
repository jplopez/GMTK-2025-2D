using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK {
  public class LevelCompleteManager : MonoBehaviour {


    public LevelSequence LevelSequence;

    public IntVariable ScoreData;

    public TMPro.TMP_Text ScoreText;


    private Animator _animator;

    private void Start() {
      if (LevelSequence == null) {
        Debug.LogError("LevelSequence is not assigned in LevelCompleteManager.");
      }
      if (ScoreData == null) {
        Debug.LogError("ScoreData is not assigned in LevelCompleteManager.");
      }

      if (!TryGetComponent(out _animator)) {
        Debug.LogWarning("No Animator component found on LevelCompleteManager.");
      }

      if (ScoreText != null && ScoreData != null) {
        ScoreText.gameObject.SetActive(false);
      }
    }

    public void OnIntroFinished() {
      if (ScoreText != null && ScoreData != null) {
        ScoreText.text = $"{ScoreData.Value}";
        ScoreText.gameObject.SetActive(true);
      }
      //TODO Trigger score animation , not implemented yet
      //if (_animator != null) {
      //  _animator.SetTrigger("ShowScore");
      //}
    }

    public void RetryLevel() {
      string currentScene = LevelSequence.CurrentScene;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in LevelSequence.");
        return;
      }
      Debug.Log($"Retrying level: {currentScene}");
      SceneManager.LoadScene(currentScene);
    }

    public void NextLevel() {
      string currentScene = LevelSequence.CurrentScene;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in LevelSequence.");
        return;
      }
      string nextLevel = LevelSequence.GetNextLevel(currentScene);
      if (!string.IsNullOrEmpty(nextLevel)) {
        Debug.Log($"Loading next level: {nextLevel}");
        LevelSequence.SetCurrentScene(nextLevel);
        SceneManager.LoadScene(nextLevel);
      }
      else {
        Debug.Log("No more levels. Game complete!");
        // Handle end of game logic here, e.g., show credits or restart
      }
    }

  }

}