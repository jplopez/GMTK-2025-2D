using Ameba;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GMTK {

  /// <summary>
  /// MonoBehaviour to control the display and update of the HUD elements (score, playback buttons)
  /// Raised Events
  ///  - OnLevelPlay when Play control button is clicked
  ///  - OnLevelReset when Reset control button is clicked
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

      _eventChannel.AddListener(GameEventType.ShowPlaybackControls, ShowPlayback);
      _eventChannel.AddListener(GameEventType.EnablePlaybackControls, EnablePlayback);
      PlayButton.onClick.AddListener(PlaybuttonFeedbacks);
      ResetButton.onClick.AddListener(ResetbuttonFeedbacks);
    }

    private void OnDestroy() {
      PlayButton.onClick.RemoveListener(PlaybuttonFeedbacks);
      ResetButton.onClick.RemoveListener(ResetbuttonFeedbacks);

      _eventChannel.RemoveListener(GameEventType.ShowPlaybackControls, ShowPlayback);
      _eventChannel.RemoveListener(GameEventType.EnablePlaybackControls, EnablePlayback);
    }

    private void Update() {
      if (hudSO != null) {
        ShowPlayback(hudSO.ShowPlaybackButtons);
        EnablePlayback(hudSO.ShowPlaybackButtons);
      }
      UpdateScoreText(hudSO.MarbleScoreKeeper.GetScore());
    }
    public void ShowPlayback(bool show) {
      PlayButton.gameObject.SetActive(show);
      ResetButton.gameObject.SetActive(show);
    }
    public void EnablePlayback(bool enable) {
      PlayButton.enabled = enable;
      ResetButton.enabled = enable;
    }

    private void PlaybuttonFeedbacks() {
      _eventChannel.Raise(GameEventType.OnLevelPlay);
      // Optional: local feedback
    }

    private void ResetbuttonFeedbacks() {
      _eventChannel.Raise(GameEventType.OnLevelReset);
      // Optional: local feedback
    }
    private void UpdateScoreText(int newScore) => scoreText.text = $"{newScore:D5}";
  }
}
