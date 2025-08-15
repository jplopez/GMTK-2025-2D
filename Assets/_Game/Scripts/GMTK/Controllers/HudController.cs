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

    [SerializeField] private HUD hudSO;

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

    protected GameEventChannel _eventChannel;
    private void Awake() {

      if (_eventChannel == null) {
        _eventChannel = Resources.Load<GameEventChannel>("GameEventChannel");
      }
      if (hudSO == null) {
        hudSO = Resources.Load<HUD>("HUD");
      }
      var scoreStrategy = gameObject.AddComponent<TimeBasedScoreCalculator>();
      hudSO.MarbleScoreKeeper.SetStrategy(scoreStrategy, transform);
      UpdateScoreText(hudSO.MarbleScoreKeeper.GetScore());

      PlayButton.onClick.AddListener(HandlePlayButtonClick);
      ResetButton.onClick.AddListener(HandleResetButtonClick);
      Game.Context.StateMachine.AddListener(HandleChangeState);
    }

    private void OnDestroy() {
      PlayButton.onClick.RemoveListener(HandlePlayButtonClick);
      ResetButton.onClick.RemoveListener(HandleResetButtonClick);
      Game.Context.RemoveStateChangeListener(HandleChangeState);
    }

    private void Update() {
      if (hudSO != null) {
        ShowPlayback(hudSO.ShowPlaybackButtons);
        EnablePlayback(hudSO.ShowPlaybackButtons);
      }
      UpdateScoreText(hudSO.MarbleScoreKeeper.GetScore());
    }

    public void UpdateUI(GameStates gameState) {
      switch (gameState) {
        case GameStates.Preparation:
          ShowPlayback(true);
          EnablePlayback(true);
          break;
        case GameStates.Playing:
        case GameStates.Reset:
          ShowPlayback(true);
          EnablePlayback(false);
          break;
      }
    }

    public void ShowPlayback(bool show) {
      PlayButton.gameObject.SetActive(show);
      ResetButton.gameObject.SetActive(show);
    }
    public void EnablePlayback(bool enable) {
      PlayButton.interactable = enable;
      ResetButton.interactable = enable;
    }

    public void HandleChangeState(StateMachineEventArg<GameStates> eventArg) {
      if (eventArg == null) return;
      UpdateUI(eventArg.ToState);
    }

    private void HandlePlayButtonClick() {
      //ensure the button only triggers logic if in the correct state.
      if (Game.Context.CanTransitionTo(GameStates.Playing)) {
        _eventChannel.Raise(GameEventType.LevelPlay);
        // Optional: local feedback
      }
    }

    private void HandleResetButtonClick() {
      //ensure the button only triggers logic if in the correct state.
      if (Game.Context.CanTransitionTo(GameStates.Reset)) {
        _eventChannel.Raise(GameEventType.LevelReset);
        // Optional: local feedback
      }
    }
    private void UpdateScoreText(int newScore) => scoreText.text = $"{newScore:D5}";
  }
}
