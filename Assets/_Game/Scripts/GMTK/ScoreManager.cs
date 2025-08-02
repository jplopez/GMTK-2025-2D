using UnityEngine;
using TMPro;

namespace GMTK {
  public class ScoreManager : MonoBehaviour {

    [SerializeField] private IntVariable scoreData;
    [SerializeField] private EventChannel scoreEvents;
    [SerializeField] private TMP_Text scoreText;

    private void OnEnable() {
      if (scoreEvents != null)
        scoreEvents.OnIntRaised += HandleScoreAdded;

      if (scoreData != null)
        scoreData.OnValueChanged.AddListener(UpdateScoreText);

      UpdateScoreText(scoreData != null ? scoreData.Value : 0);
    }

    private void OnDisable() {
      if (scoreEvents != null)
        scoreEvents.OnIntRaised -= HandleScoreAdded;

      if (scoreData != null)
        scoreData.OnValueChanged.RemoveListener(UpdateScoreText);
    }

    private void HandleScoreAdded(int amount) {
      scoreData.Add(amount);
      Debug.Log($"ScoreData updated: {scoreData.Value}");
    }

    private void UpdateScoreText(int newScore) {
      scoreText.text = $"{newScore:N0}";
      Debug.Log($"ScoreText updated: {scoreText.text}");
    }

  }
}
