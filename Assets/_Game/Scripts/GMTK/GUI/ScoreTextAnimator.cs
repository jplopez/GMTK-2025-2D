using Ameba;
using UnityEngine;

namespace GMTK {

  public class ScoreTextAnimator : MonoBehaviour {

    public TMPro.TMP_Text ScoreText;

    [Tooltip("Frequency the numbers should take next step. Lowest numbers make numbers go faster")]
    [Range(1,30)]
    public int TextFrameSpeed = 5;

    public int ScoreStep = 10;

    protected int _tickCount = 0;
    protected int _currentDisplayedScore = 0;
    protected ScoreGateKeeper _scoreKeeper;
    private void Awake() {
      InitializationManager.WaitForInitialization(this, OnReady);
    }

    private void OnReady() {
      _scoreKeeper = Game.ScoreKeeper;
      _currentDisplayedScore = 0;
    }
    public void Update() {
      if (ScoreText != null && ScoreText.gameObject.activeSelf && ScoreText.enabled) {
        _tickCount++;
        if (_tickCount % TextFrameSpeed == 0) {
          _currentDisplayedScore = Mathf.Min(_currentDisplayedScore + ScoreStep, _scoreKeeper.GetScore());
        }
        ScoreText.text = $"{_currentDisplayedScore:D6}";
      }
    }
  }
}