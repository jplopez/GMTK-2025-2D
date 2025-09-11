using Ameba;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
 
  public class GameplayStateHandler : GameStateHandler {

    [Header("Gameplay References")]
    [Tooltip("Reference to the playable Marble in the scene")]
    public PlayableMarbleController Marble;
    [Tooltip("Reference to the LevelManager in the scene")]
    public LevelManager SceneLevelManager;
    [Tooltip("Reference to the SceneController in the scene")]
    public SceneController CurrentSceneController;

    //snappables the player can interact with
    protected List<GridSnappable> _draggableSnappables = new();
    //snappables already in the level
    protected List<GridSnappable> _staticSnappables = new();
    private void OnEnable() {
      Priority = 200;
      HandlerName = name;
    }

    protected override void Init() {
      Priority = (Priority == 0) ? 200 : Priority;

      InitServices();

      //find all snappables and assigned them if they're draggables or not
      var snappables = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None).ToList();
      _draggableSnappables = snappables.Where(s => s.Draggable).ToList();
      _staticSnappables = snappables.Where(s => !s.Draggable).ToList();

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
      Marble.Spawn();
      SetDraggableSnappables(true);
    }

    protected override void ToPlaying() {
      this.Log("ToPlaying: starting level");
      SetDraggableSnappables(false);
      SceneLevelManager.StartLevel();
      Marble.Launch();
    }

    protected override void ToReset() {
      this.Log("ToReset: resetting up level");
      Marble.Spawn();
      Marble.Model.SetActive(false);
      SceneLevelManager.ResetLevel();
      ResetSnappables(draggables:false);
    }

    /// <summary>
    /// Prepares the game to load the victory scene
    /// </summary>
    protected override void ToLevelComplete() {
      this.Log("ToLevelComplete: level complete");
      Marble.Spawn();
      Marble.Model.SetActive(false);
      SceneLevelManager.EndLevel();
      //ResetSnappables();
      //TODO: trigger effects of slowing down and victory music
      //TODO: fadeout 
      CurrentSceneController.LoadNextScene();
    }

    private void SetDraggableSnappables(bool enabled) => _draggableSnappables.ForEach(s => s.Draggable = enabled);

    private void ResetSnappables(bool draggables=true, bool nonDraggables=true) {
      this.Log("ResetSnappables");
      if (draggables) {
        _draggableSnappables.ForEach(s => s.ResetSnappable());
      }
      if (nonDraggables) {
        _staticSnappables.ForEach(s => s.ResetSnappable());
      }
    }
  }
}
