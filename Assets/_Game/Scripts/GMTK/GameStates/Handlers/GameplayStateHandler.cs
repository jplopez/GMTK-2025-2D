using Ameba;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
 
  public class GameplayStateHandler : GameStateHandler {

    [Header("Gameplay References")]
    [Tooltip("Reference to the playable marble in the scene")]
    public PlayableMarbleController marble;
    [Tooltip("Reference to the LevelManager in the scene")]
    public LevelManager SceneLevelManager;
    [Tooltip("Reference to the SceneController in the scene")]
    public SceneController CurrentSceneController;

    protected List<GridSnappable> _draggableSnappables = new();
    protected List<GridSnappable> _staticSnappables = new();
    private void OnEnable() {
      Priority = 200;
      HandlerName = name;
    }

    protected override void Init() {
      Priority = (Priority == 0) ? 200 : Priority;

      if (marble == null) {
        marble = FindFirstObjectByType<PlayableMarbleController>();
      }
      
      if (SceneLevelManager == null) {
        SceneLevelManager = FindFirstObjectByType<LevelManager>();
      }

      if (CurrentSceneController == null) {
        CurrentSceneController = FindFirstObjectByType<SceneController>();
      }

      //if (snappables == null || snappables.Count == 0) {
      var snappables = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None).ToList();
      _draggableSnappables = snappables.Where(s => s.Draggable).ToList();
      _staticSnappables = snappables.Where(s => !s.Draggable).ToList();
      //}

      //Ensure the marble has a place to start
      if(marble.SpawnTransform == null) {
        marble.SpawnTransform = SceneLevelManager.StartLevelCheckpoint.transform;
      }
      if (marble.InitialForce == null || marble.InitialForce == Vector2.zero) {
        marble.InitialForce = SceneLevelManager.MarbleInitialForce;
      }

    }
    protected override void ToPreparation() {
      //place marble in starting point
      marble.Spawn();
      SetDraggableSnappables(true);
    }

    protected override void ToPlaying() {
      SetDraggableSnappables(false);
      SceneLevelManager.StartLevel();
      marble.Launch();
    }

    protected override void ToReset() {
      marble.Spawn();
      marble.Model.SetActive(false);
      SceneLevelManager.ResetLevel();
      ResetAllSnappables();
    }

    /// <summary>
    /// Prepares the game to load the victory scene
    /// </summary>
    protected override void ToLevelComplete() {
      marble.Spawn();
      marble.Model.SetActive(false);
      SceneLevelManager.EndLevel();
      //ResetAllSnappables();
      //TODO: trigger effects of slowing down and victory music
      //TODO: fadeout 
      CurrentSceneController.LoadNextScene();
    }

    private void SetDraggableSnappables(bool enabled) => _draggableSnappables.ForEach(s => s.Draggable = enabled);

    private void ResetAllSnappables() {
      _draggableSnappables.ForEach(s => s.ResetSnappable());
      _staticSnappables.ForEach(s => s.ResetSnappable());
    }
  }
}
