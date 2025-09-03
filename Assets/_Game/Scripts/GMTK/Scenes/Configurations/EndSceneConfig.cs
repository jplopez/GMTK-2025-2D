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
      LogDebug("Cleaning up event listeners...");

      var eventChannel = Services.Get<GameEventChannel>();
      var stateMachine = Services.Get<GameStateMachine>();
      var handlerRegistry = Services.Get<GameStateHandlerRegistry>();

      // Clear event channel listeners
      if (eventChannel != null) {
        eventChannel.RemoveAllListeners();
        LogDebug("✓ GameEventChannel listeners cleared");
      }

      // Clear state machine event handlers
      if (stateMachine != null) {
        stateMachine.RemoveAllListeners();
        LogDebug("✓ StateMachine event handlers cleared");
      }

      // Clear handler registry
      if (handlerRegistry != null) {
        handlerRegistry.ClearAllHandlers();
        LogDebug("✓ HandlerRegistry cleared");
      }
    }

    private void ClearTransientGameData() {
      LogDebug("Clearing transient game data...");

      var scoreKeeper = Services.Get<ScoreGateKeeper>();
      if (scoreKeeper != null) {
        // Reset score if needed
        scoreKeeper.ResetScore();
        LogDebug("✓ Score data cleared");
      }

      var levelService = Services.Get<LevelService>();
      if (levelService != null) {
        //return levelService to the Scene marked as Start
        levelService.SetCurrentLevel(levelService.FirstStartConfig().SceneName);
        LogDebug("✓ Level progress cleared");
      }
    }

    private void ResetAllServices() {
      LogDebug("Resetting services...");
      Services.Clear();
      LogDebug("✓ Services reset");
    }

    private void LogDebug(string message) {
      if (EnableDebugLogging) {
        Debug.Log($"[EndScene] {message}");
      }
    }
  }

}