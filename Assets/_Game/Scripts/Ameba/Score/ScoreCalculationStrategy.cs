
using UnityEngine;

namespace Ameba {

  public abstract class ScoreCalculationStrategy : MonoBehaviour {

    public abstract void Initialize(Transform playerTransform);

    public abstract int CalculateScore(float deltaTime);

  }

  public class TimeBasedScoreCalculator : ScoreCalculationStrategy {

    [Tooltip("The frequency the score calculator grants points. This number represents the fraction of one second")]
    [Range(0.1f,1.0f)]
    public float PointsScoredInterval = 0.3f;
    [Tooltip("The points scored for every interval")]
    public float PointsPerInterval = 10f;

    private float accumulator = 0f;

    public override void Initialize(Transform playerTransform) { }

    public override int CalculateScore(float deltaTime) {
      accumulator += deltaTime;
      int points = 0;
      if(accumulator >= PointsScoredInterval) {
        points = Mathf.FloorToInt(PointsPerInterval);
        accumulator = 0f;
      }
      return points;
    }
  }

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
    public override int CalculateScore(float deltaTime) {
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