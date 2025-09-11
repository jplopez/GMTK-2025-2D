
using UnityEngine;

namespace Ameba {
  [AddComponentMenu("Ameba/Score Strategies/Time Based")]
  public class TimeBasedScoreCalculator : ScoreCalculationStrategy {

    [Tooltip("The frequency the score calculator grants points. This number represents the fraction of one second")]
    [Range(0.1f,1.0f)]
    public float PointsScoredInterval = 0.3f;
    [Tooltip("The points scored for every interval")]
    public float PointsPerInterval = 10f;

    private float accumulator = 0f;

    public override void Initialize(Transform playerTransform) { }

    public override int CalculateDelta(float deltaTime) {
      accumulator += deltaTime;
      int points = 0;
      if(accumulator >= PointsScoredInterval) {
        points = Mathf.FloorToInt(PointsPerInterval);
        accumulator = 0f;
      }
      return points;
    }
  }
}