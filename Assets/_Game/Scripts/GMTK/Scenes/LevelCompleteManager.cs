using UnityEngine;
using UnityEngine.SceneManagement;
using Ameba;

namespace GMTK {
  public class LevelCompleteManager : MonoBehaviour {

    public TMPro.TMP_Text ScoreText;
    
    private Animator _animator;
    
    protected int _currentScore = 0;
    protected LevelService _levelService;

    private void Awake() {
      
      if (!TryGetComponent(out _animator)) {
        Debug.LogWarning("No Animator component found on LevelCompleteManager.");
      }
      _currentScore = ServiceLocator.Get<ScoreGateKeeper>().GetScore();
      if (_levelService == null) {
        _levelService = ServiceLocator.Get<LevelService>();
      }
    }

    public void OnIntroFinished() {
      if (ScoreText != null) {
        //ScoreText.text = $"{_currentScore:D5}";
        ScoreText.gameObject.SetActive(true);
      }
    }

    public void RetryLevel() {
      string currentScene = _levelService.CurrentSceneName;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in _levelSequence.");
        return;
      }
      Debug.Log($"Retrying level: {currentScene}");
      UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
    }

    public void NextLevel() {
      string currentScene = _levelService.CurrentSceneName;
      if (string.IsNullOrEmpty(currentScene)) {
        Debug.LogError("Current scene is not set in _levelSequence.");
        return;
      }
      string nextLevel = _levelService.CurrentLevel?.NextSceneName;
      if (!string.IsNullOrEmpty(nextLevel)) {
        Debug.Log($"Loading next level: {nextLevel}");
        //_levelService.SetCurrentScene(nextLevel);
        _levelService.SetCurrentLevel(nextLevel);
        SceneManager.LoadScene(nextLevel);
      }
      else {
        Debug.Log("No more levels. Game complete!");
        // Handle end of game logic here, e.g., show credits or restart
      }
    }

  }

}