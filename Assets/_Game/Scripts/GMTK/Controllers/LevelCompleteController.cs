using UnityEngine;
using Ameba;

namespace GMTK {

  /// <summary>
  /// Handles the level complete screen, displaying the player's score and managing animations.
  /// </summary>
  public class LevelCompleteController : MonoBehaviour {

    public TMPro.TMP_Text ScoreText;
    
    private Animator _animator;
    
    protected int _currentScore = 0;

    private void Awake() {
      
      if (!TryGetComponent(out _animator)) {
        this.LogWarning("No Animator component found on LevelCompleteController.");
      }
      _currentScore = ServiceLocator.Get<ScoreGateKeeper>().GetScore();
    }

    public void OnIntroFinished() {
      if (ScoreText != null) {
        //ScoreText.text = $"{_currentScore:D5}";
        ScoreText.gameObject.SetActive(true);
      }
    }

  }

}