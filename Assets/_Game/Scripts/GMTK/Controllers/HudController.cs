using Ameba;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour to control the display and update of the HUD elements (score, playback buttons)
  /// Raised Events
  ///  - LevelPlay when Play control button is clicked
  ///  - LevelReset when ResetToStartingState control button is clicked
  /// Listener
  /// - ShowPlaybackControls(bool) to show/hide the play and reset buttons
  /// - EnablePlaybackControls(bool) to enable/disable the play and reset buttons
  /// </summary>
  public class HudController : MonoBehaviour {

    [Header("Score")]
    [Tooltip("The TMP text field to display the score value")]
    [SerializeField] protected TMP_Text scoreText;

    [Header("Playback")]
    [Tooltip("if true, the playback buttons aren't displayed")]
    public bool ShowPlaybackButtons = true;
    [Tooltip("if true, the playback buttons are shown as disabled")]
    public bool DisablePlaybackButtons = false;

    [Tooltip("The UI object for the play button")]
    public Button PlayButton;
    [Tooltip("The UI object for the reset button")]
    public Button ResetButton;

    [Tooltip("WIP - pending Feel integration")]
    public GameObject PlayFeedback;
    [Tooltip("WIP - pending Feel integration")]
    public GameObject ResetFeedback;

    protected HUD _hud;
    
    private void Awake() {

      if (_hud == null) {
        _hud = Game.Context.Hud;
      }
      var scoreStrategy = gameObject.AddComponent<TimeBasedScoreCalculator>();
      _hud.MarbleScoreKeeper.SetStrategy(scoreStrategy, transform);
      UpdateScoreText(_hud.MarbleScoreKeeper.GetScore());

      PlayButton.onClick.AddListener(HandlePlayButtonClick);
      ResetButton.onClick.AddListener(HandleResetButtonClick);
    }

    private void OnDestroy() {
      PlayButton.onClick.RemoveListener(HandlePlayButtonClick);
      ResetButton.onClick.RemoveListener(HandleResetButtonClick);
    }

    private void Update() {
      UpdateScoreText(_hud.MarbleScoreKeeper.GetScore());
    }

    public void UpdateUIFromGameState(GameStates gameState) {
      switch (gameState) {
        case GameStates.Preparation:
          //ShowPlayback(true);
          PlayButton.enabled = true;
          PlayButton.interactable = true;
          ResetButton.enabled = false;
          ResetButton.interactable = false;
          break;
        case GameStates.Playing:
          //ShowPlayback(true);
          PlayButton.enabled = false;
          PlayButton.interactable = false;
          ResetButton.enabled = true;
          ResetButton.interactable = true;
          break;
        case GameStates.Reset:
          PlayButton.enabled = true;
          PlayButton.interactable = true;
          ResetButton.enabled = true;
          ResetButton.interactable = true;
          
          //Uncomment the LevelStart event for testing only.
          //For now, reset only moves elements back to its initial place in the level
          //and moves marble back to starting point. In the future, reset 
          //could trigger animations or other feedbacks.

          Game.Context.EventsChannel.Raise(GameEventType.LevelStart);
          break;
        case GameStates.Pause:
        case GameStates.Options:
          PlayButton.enabled = false;
          PlayButton.interactable = false;
          ResetButton.enabled = false;
          ResetButton.interactable = false;
          break;
      }
    }

    private void HandlePlayButtonClick() {
      //ensure the button only triggers logic if in the correct state.
      if (Game.Context.CanTransitionTo(GameStates.Playing)) {
        Game.Context.EventsChannel.Raise(GameEventType.LevelPlay);
        // Optional: local feedback
      }
    }

    private void HandleResetButtonClick() {
      //ensure the button only triggers logic if in the correct state.
      if (Game.Context.CanTransitionTo(GameStates.Reset)) {
        Game.Context.EventsChannel.Raise(GameEventType.LevelReset);
        // Optional: local feedback
      }
    }
    private void UpdateScoreText(int newScore) => scoreText.text = $"{newScore:D5}";
  }
}