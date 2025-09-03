using UnityEngine;

namespace Ameba {

  [CreateAssetMenu(menuName = "Ameba/ScoreGateKeeper")]
  public class ScoreGateKeeper : ScriptableObject {

    [SerializeField] protected int totalScore;
    [SerializeField] protected ScoreCalculationStrategy strategy;
    [SerializeField] protected bool pauseScore = false;

    [Tooltip("If paused, the score strategy is not counting score")]
    public string ScoreState => pauseScore.ToString();

    public void SetStrategy(ScoreCalculationStrategy newStrategy, Transform playerTransform) {
      strategy = newStrategy;
      strategy.Initialize(playerTransform);
    }

    [ContextMenu("ResetToStartingState Score")]
    public void ResetScore() => totalScore = 0;

    public void Tick(float deltaTime) {
      if (strategy == null || pauseScore) return;
      totalScore += strategy.CalculateScore(deltaTime);
    }
    public void SetScore(int amount) => totalScore = amount;

    public int GetScore() => totalScore;

    public void PauseScore(bool pause=true) => pauseScore = pause; 
    public bool IsPaused() => pauseScore;

    public bool HasStrategy() => strategy != null;
#if UNITY_EDITOR
    public void Pause() => PauseScore(true);
    public void Unpause() => PauseScore(false);
#endif
  }
}