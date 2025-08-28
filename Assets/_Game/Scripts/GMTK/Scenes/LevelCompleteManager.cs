using UnityEngine;
using UnityEngine.SceneManagement;
using Ameba;

namespace GMTK {
  public class LevelCompleteManager : MonoBehaviour {

    public TMPro.TMP_Text ScoreText;
    
    private Animator _animator;
    
    protected int _currentScore = 0;
    protected LevelSequence _sequence;

    private void Awake() {
      InitializationManager.WaitForInitialization(this, OnReady);
    }

    private void OnReady() {
      if (!TryGetComponent(out _animator)) {
        Debug.LogWarning("No Animator component found on LevelCompleteManager.");
      }
      _currentScore = Services.Get<ScoreGateKeeper>().GetScore();
      if (_sequence == null) {
        _sequence = Services.Get<LevelSequence>();
      }
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
      string currentScene = _sequence.CurrentScene;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in _levelSequence.");
        return;
      }
      Debug.Log($"Retrying level: {currentScene}");
      UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
    }

    public void NextLevel() {
      string currentScene = _sequence.CurrentScene;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in _levelSequence.");
        return;
      }
      string nextLevel = _sequence.GetNextLevel(currentScene);
      if (!string.IsNullOrEmpty(nextLevel)) {
        Debug.Log($"Loading next level: {nextLevel}");
        _sequence.SetCurrentScene(nextLevel);
        SceneManager.LoadScene(nextLevel);
      }
      else {
        Debug.Log("No more levels. Game complete!");
        // Handle end of game logic here, e.g., show credits or restart
      }
    }

  }

}