using System.Collections;
using UnityEngine;
using Ameba;

namespace GMTK {

  /// <summary>
  /// GMTK-specific initialization component that loads all required ScriptableObjects
  /// for the Roll n' Snap game before any GameObjects start their lifecycle.
  /// </summary>
  public class GameInitComponent : InitializationComponent {

    protected override IEnumerator InitializeAllScriptableObjects() {
      Debug.Log("Loading GMTK ScriptableObjects...");

      // Load all critical ScriptableObjects in dependency order
      yield return LoadGameStateMachine();
      yield return LoadEventChannels();
      yield return LoadLevelSequence();
      yield return LoadScoreKeeper();
      yield return LoadHUD();
      yield return LoadHandlerRegistry();

      // Mark initialization as complete
      InitializationManager.MarkInitializationComplete();

      // Self-destruct after initialization
      Destroy(gameObject);
    }

    private IEnumerator LoadGameStateMachine() {
      yield return LoadResourceWithRetry<GameStateMachine>("GameStateMachine",
          result => Game.SetGameStateMachine(result));
    }

    private IEnumerator LoadEventChannels() {
      yield return LoadResourceWithRetry<GameEventChannel>("GameEventChannel",
          result => Game.SetGameEventChannel(result));

      yield return LoadResourceWithRetry<InputActionEventChannel>("InputActionEventChannel",
          result => Game.SetInputEventChannel(result));
    }

    private IEnumerator LoadLevelSequence() {
      yield return LoadResourceWithRetry<LevelSequence>("LevelSequence",
          result => Game.SetLevelSequence(result));
    }

    private IEnumerator LoadScoreKeeper() {
      yield return LoadResourceWithRetry<ScoreGateKeeper>("MarbleScoreKeeper",
          result => Game.SetScoreKeeper(result));
    }

    private IEnumerator LoadHUD() {
      yield return LoadResourceWithRetry<HUD>("HUD",
          result => Game.SetHUD(result));
    }

    private IEnumerator LoadHandlerRegistry() {
      yield return LoadResourceWithRetry<GameStateHandlerRegistry>("GameStateHandlerRegistry",
          result => {
            Game.SetHandlerRegistry(result);
            if(result != null) result.Initialize();
          });
    }
  }
}
