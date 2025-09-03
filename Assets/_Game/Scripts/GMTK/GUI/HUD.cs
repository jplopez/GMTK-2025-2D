using Ameba;
using System;
using UnityEngine;

namespace GMTK {
  /// <summary>
  /// Handles the display and update of the HUD elements (score, playback buttons)
  /// Listens to RaiseScore and SetScoreValue from EventChannel
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

    private void Awake() {
      if (eventChannel == null) {
        eventChannel = Services.Get<GameEventChannel>();
      }
      if (MarbleScoreKeeper == null) {
        MarbleScoreKeeper = Services.Get<ScoreGateKeeper>();
      }
      
      // Add explicit type parameters
      eventChannel.AddListener<float>(GameEventType.RaiseScore, HandleScoreAdded);
      eventChannel.AddListener<int>(GameEventType.SetScoreValue, HandleScoreSet);
      eventChannel.AddListener(GameEventType.ResetScore, HandleResetScore); // void - no change needed
    }

    private void OnDisable() {
      if (eventChannel == null) return;
      
      eventChannel.RemoveListener<float>(GameEventType.RaiseScore, HandleScoreAdded);
      eventChannel.RemoveListener<int>(GameEventType.SetScoreValue, HandleScoreSet);
      eventChannel.RemoveListener(GameEventType.ResetScore, HandleResetScore); // void - no change needed
    }

    // Update method signatures to match expected types
    private void HandleScoreAdded(float amount) => MarbleScoreKeeper.Tick(amount);
    private void HandleScoreSet(int amount) => MarbleScoreKeeper.SetScore(amount);
    private void HandleResetScore() => MarbleScoreKeeper.ResetScore();

#if UNITY_EDITOR

    [ContextMenu("ResetToStartingState Score")]
    public void ResetScore() => MarbleScoreKeeper.ResetScore();
#endif
  }
}


