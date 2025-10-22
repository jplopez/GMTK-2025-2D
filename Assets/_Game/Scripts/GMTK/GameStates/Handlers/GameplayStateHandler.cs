using Ameba;
using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
 
  public class GameplayStateHandler : BaseGameStateHandler {

    [Header("Gameplay References")]
    [Tooltip("Reference to the playable Marble in the scene")]
    public PlayableMarbleController Marble;
    [Tooltip("Reference to the LevelManager in the scene")]
    public LevelManager SceneLevelManager;
    [Tooltip("Reference to the SceneController in the scene")]
    public SceneController CurrentSceneController;

    [Header("Feedbacks (optional)")]
    [Tooltip("Feedback played when the game state changes to Preparation")]
    public MMF_Player ToPreparationFeedback;
    [Tooltip("Feedback played when the game state changes to Playing")]
    public MMF_Player ToPlayingFeedback;
    [Tooltip("Feedback played when the game state changes to Reset")]
    public MMF_Player ToResetFeedback;
    [Tooltip("Feedback played when the game state changes to LevelComplete")]
    public MMF_Player ToLevelCompleteFeedback;

    //snappables the player can interact with
    protected List<PlayableElement> _draggableElements = new();
    //snappables already in the level
    protected List<PlayableElement> _staticElements = new();
    private void OnEnable() {
      Priority = 200;
      HandlerName = name;
    }

    protected override void Init() {
      Priority = (Priority == 0) ? 200 : Priority;

      InitServices();

      //find all snappables and assigned them if they're draggables or not
      var snappables = FindObjectsByType<PlayableElement>(FindObjectsSortMode.None).ToList();
      _draggableElements = snappables.Where(s => s.Draggable).ToList();
      _staticElements = snappables.Where(s => !s.Draggable).ToList();

      //Ensure the Marble has a place to start
      if (Marble.SpawnTransform == null) {
        Marble.SpawnTransform = SceneLevelManager.StartLevelCheckpoint.transform;
      }
      if (Marble.InitialForce == null || Marble.InitialForce == Vector2.zero) {
        Marble.InitialForce = SceneLevelManager.MarbleInitialForce;
      }

    }

    private void InitServices() {
      if (Marble == null) {
        Marble = FindFirstObjectByType<PlayableMarbleController>();
      }

      if (SceneLevelManager == null) {
        SceneLevelManager = FindFirstObjectByType<LevelManager>();
      }

      if (CurrentSceneController == null) {
        CurrentSceneController = FindFirstObjectByType<SceneController>();
      }
    }

    protected override void ToPreparation() {
      this.Log("ToPreparation: setting up level");

      //reset level elements
      //this is a temporary fix for the issue of level elements not resetting when going back to preparation
      //definitive solution relies on StateMachine queue system
      ToReset();

      //place Marble in starting point
      Marble.Spawn(hidden:true);
      SetDraggableSnappables(true);

      StartCoroutine(PlayFeedbackDelayed(ToPreparationFeedback));
    }

    protected override void ToPlaying() {
      this.LogDebug("ToPlaying: starting level");
      SetDraggableSnappables(false);
      SceneLevelManager.StartLevel();
      Marble.Launch();

      StartCoroutine(PlayFeedbackDelayed(ToPlayingFeedback));
    }

    protected override void ToReset() {
      this.LogDebug("ToReset: resetting up level");
      Marble.Spawn(hidden:true);
      //Marble.Model.SetActive(false);
      SceneLevelManager.ResetLevel();
      ResetSnappables(draggables:false);

      StartCoroutine(PlayFeedbackDelayed(ToResetFeedback));
    }

    /// <summary>
    /// Prepares the game to load the victory scene
    /// </summary>
    protected override void ToLevelComplete() {
      this.LogDebug("ToLevelComplete: level complete");

      StartCoroutine(ToLevelCompleteCoroutine());
      CurrentSceneController.LoadNextScene();

      StartCoroutine(PlayFeedbackDelayed(ToLevelCompleteFeedback));
    }

    private IEnumerator ToLevelCompleteCoroutine(float delay=0f) {
      yield return new WaitForSeconds(delay);

      Marble.Spawn(hidden: true);
      SceneLevelManager.EndLevel();

      yield return new WaitUntil(() => SceneLevelManager.IsLevelEnded);
    }

    private IEnumerator PlayFeedbackDelayed(MMF_Player feedback, float delay=0f) {
      yield return new WaitForSeconds(delay);
      if(feedback != null)
        yield return feedback.PlayFeedbacksCoroutine(transform.position);
    }


    private void SetDraggableSnappables(bool enabled) => _draggableElements.ForEach(elem => elem.Draggable = enabled);

    private void ResetSnappables(bool draggables=true, bool nonDraggables=true) {
      this.LogDebug("ResetSnappables");
      if (draggables) {
        _draggableElements.ForEach(elem => elem.ResetTransform());
      }
      if (nonDraggables) {
        _staticElements.ForEach(elem => elem.ResetTransform());
      }
    }
  }
}
