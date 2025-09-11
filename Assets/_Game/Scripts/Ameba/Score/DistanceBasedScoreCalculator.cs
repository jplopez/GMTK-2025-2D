
using UnityEngine;

namespace Ameba {
  [AddComponentMenu("Ameba/Score Strategies/Distance Based")]
  public class DistanceBasedScoreCalculator : ScoreCalculationStrategy {

    [Tooltip("The distance needed to score points")]
    public float PointsScoredInterval = 0.5f;
    [Tooltip("The minimum distance to be considered a movement. Must be less or equal than PointsScoredInterval")]
    public float MinimalDistanceThreshold = 0.01f;
    [Tooltip("Points given every time the player meets the distance interval")]
    public float PointsPerUnit = 10f;
    
    private Vector3 lastPosition;
    private float accumulator = 0f;
    
    public override void Initialize(Transform playerTransform) {
      lastPosition = playerTransform.position;
    }
    public override int CalculateDelta(float deltaTime) {
      Vector3 currentPosition = transform.position;
      float distance = Vector3.Distance(currentPosition, lastPosition);
      accumulator += distance * PointsPerUnit;
      lastPosition = currentPosition;

      int points = Mathf.FloorToInt(accumulator);
      accumulator -= points;
      return points;
    }
  }
}