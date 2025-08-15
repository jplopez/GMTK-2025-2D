using Ameba;
using System;
using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Handles the display and update of the HUD elements (score, playback buttons)
  /// Listens to ScoreRaised and ScoreChanged from EventChannel
  /// </summary>
  [CreateAssetMenu(menuName = "GMTK/HUD")]
  public class HUD : ScriptableObject {

    [Header("Score")]
    [Tooltip("The ScoreKeeper knows how to calculate the score.")]
    public ScoreGateKeeper MarbleScoreKeeper;

    [Header("Playback")]
    [Tooltip("if true, the playback buttons aren't displayed")]
    public bool ShowPlaybackButtons = true;
    [Tooltip("if true, the playback buttons are shown as disabled")]
    public bool DisablePlaybackButtons = false;

    protected GameEventChannel eventChannel;

    private void OnEnable() {

      if (eventChannel == null) {
        eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }
      if (MarbleScoreKeeper == null) {
        MarbleScoreKeeper = Resources.Load<ScoreGateKeeper>("MarbleScoreKeeper");
      }
      eventChannel.AddListener(GameEventType.ScoreRaised,
          (float amount) => HandleScoreAdded(amount));
      eventChannel.AddListener(GameEventType.ScoreChanged, HandleScoreSet);
    }

    private void OnDisable() {
      eventChannel.RemoveListener(GameEventType.ScoreRaised,
        (float amount) => HandleScoreAdded(amount));
      eventChannel.RemoveListener(GameEventType.ScoreChanged, HandleScoreSet);
    }

    private void HandleScoreAdded(float amount) => MarbleScoreKeeper.Tick(amount);
    private void HandleScoreSet(int amount) => MarbleScoreKeeper.SetScore(amount);

#if UNITY_EDITOR

    [ContextMenu("ResetToStartingState Score")]
    public void ResetScore() => MarbleScoreKeeper.ResetScore();
#endif
  }
}


