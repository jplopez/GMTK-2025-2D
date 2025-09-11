using Ameba;

using UnityEngine;

namespace GMTK {

  /// <summary>
  /// This Config component can be added to an End Scene to perform cleanup operations when the scene loads.
  /// </summary>
  [AddComponentMenu("GMTK/Scenes/End Scene Config")]
  public class EndSceneConfig : MonoBehaviour, ISceneConfigExtension {

    [Header("Cleanup Options")]
    [Tooltip("If enabled, removes all event listeners from the GameEventChannel and GameStateMachine to prevent memory leaks.")]
    public bool CleanupEventListeners = true;
    [Tooltip("If enabled, clears transient game data such as score and level progress.")]
    public bool ClearGameData = true;
    [Tooltip("If enabled, resets all registered services in the Service Locator.")]
    public bool ResetServices = false;
    [Tooltip("If enabled, logs debug information to the console during cleanup operations.")]
    public bool EnableDebugLogging = true;

    public void ApplyConfig(SceneController controller) => PerformCleanup();

    public bool CanApplyOnType(SceneType type) => type == SceneType.End;

    private void PerformCleanup() {
      if (CleanupEventListeners) {
        CleanupAllEventListeners();
      }

      if (ClearGameData) {
        ClearTransientGameData();
      }

      if (ResetServices) {
        ResetAllServices();
      }
    }

    private void CleanupAllEventListeners() {
      this.Log("Cleaning up event listeners...");

      var eventChannel = ServiceLocator.Get<GameEventChannel>();
      var stateMachine = ServiceLocator.Get<GameStateMachine>();

      // Clear event channel listeners
      if (eventChannel != null) {
        eventChannel.RemoveAllListeners();
        this.Log("✓ GameEventChannel listeners cleared");
      }

      // Clear state machine event handlers
      if (stateMachine != null) {
        stateMachine.RemoveAllListeners();
        this.Log("✓ StateMachine event handlers cleared");
      }
    }

    private void ClearTransientGameData() {
      this.Log("Clearing transient game data...");

      var scoreKeeper = ServiceLocator.Get<ScoreGateKeeper>();
      if (scoreKeeper != null) {
        // Reset score if needed
        scoreKeeper.ResetScore();
        this.Log("✓ Score data cleared");
      }

      var levelService = ServiceLocator.Get<LevelService>();
      if (levelService != null) {
        //return levelService to the Scene marked as Start
        levelService.MoveToFirstLevel();
        this.Log("✓ Level progress cleared");
      }
    }

    private void ResetAllServices() {
      this.Log("Resetting services...");
      ServiceLocator.Clear();
      this.Log("✓ ServiceLocator reset");
    }

  }

}