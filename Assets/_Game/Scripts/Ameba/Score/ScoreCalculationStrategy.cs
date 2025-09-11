
using UnityEngine;

namespace Ameba {

  public abstract class ScoreCalculationStrategy : MonoBehaviour {

    public int Score { get; protected set; } = 0;

    public abstract void Initialize(Transform playerTransform);

    public abstract int CalculateDelta(float deltaTime);

    private void Update() {
      int points = CalculateDelta(Time.deltaTime);
      if(points != 0) {
        Score += points;
      }
    }

  }
}