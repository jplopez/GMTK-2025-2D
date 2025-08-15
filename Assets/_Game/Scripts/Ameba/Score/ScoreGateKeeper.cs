using UnityEngine;

namespace Ameba {

  [CreateAssetMenu(menuName = "Ameba/ScoreGateKeeper")]
  public class ScoreGateKeeper : ScriptableObject {

    [SerializeField] protected int totalScore;
    [SerializeField] protected ScoreCalculationStrategy strategy;

    public void SetStrategy(ScoreCalculationStrategy newStrategy, Transform playerTransform) {
      strategy = newStrategy;
      strategy.Initialize(playerTransform);
    }

    [ContextMenu("ResetToStartingState Score")]
    public void ResetScore() => totalScore = 0;

    public void Tick(float deltaTime) {
      if (strategy == null) return;
      totalScore += strategy.CalculateScore(deltaTime);
    }
    public void SetScore(int amount) => totalScore = amount;

    public int GetScore() => totalScore;

  }
}