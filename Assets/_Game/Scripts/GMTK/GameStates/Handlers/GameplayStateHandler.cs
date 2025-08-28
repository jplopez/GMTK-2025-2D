using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
  public class GameplayStateHandler : GameStateHandler {

    [Header("Gameplay References")]
    public PlayableMarbleController marble;
    public LevelManager levelManager;

    protected List<GridSnappable> _draggableSnappables = new();
    protected List<GridSnappable> _staticSnappables = new();
    private void OnEnable() {
      Priority = 200;
      HandlerName = nameof(GameplayStateHandler);
    }

    protected override void Init() {
      Priority = (Priority == 0) ? 200 : Priority;

      if (marble == null) {
        marble = FindFirstObjectByType<PlayableMarbleController>();
      }
      
      if (levelManager == null) {
        levelManager = FindFirstObjectByType<LevelManager>();
      }

      //if (snappables == null || snappables.Count == 0) {
      var snappables = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None).ToList();
      _draggableSnappables = snappables.Where(s => s.Draggable).ToList();
      _staticSnappables = snappables.Where(s => !s.Draggable).ToList();
      //}

      //Ensure the marble has a place to start
      if(marble.SpawnTransform == null) {
        marble.SpawnTransform = levelManager.StartLevelCheckpoint.transform;
      }
      if (marble.InitialForce == null || marble.InitialForce == Vector2.zero) {
        marble.InitialForce = levelManager.MarbleInitialForce;
      }
    }
    protected override void ToPreparation() {
      //place marble in starting point
      marble.Spawn();
      SetDraggableSnappables(true);
    }

    protected override void ToPlaying() {
      SetDraggableSnappables(false);
      levelManager.StartLevel();
      marble.Launch();
    }

    protected override void ToReset() {
      marble.Spawn();
      marble.Model.SetActive(false);
      levelManager.ResetLevel();
      ResetAllSnappables();
    }

    /// <summary>
    /// Prepares the game to load the victory scene
    /// </summary>
    protected override void ToLevelComplete() {
      marble.Spawn();
      marble.Model.SetActive(false);
      levelManager.EndLevel();
      //ResetAllSnappables();
      //TODO: trigger effects of slowing down and victory music
      //TODO: fadeout 
      Game.Context.LoadNextScene();
    }

    private void SetDraggableSnappables(bool enabled) => _draggableSnappables.ForEach(s => s.Draggable = enabled);

    private void ResetAllSnappables() {
      _draggableSnappables.ForEach(s => s.ResetSnappable());
      _staticSnappables.ForEach(s => s.ResetSnappable());
    }
  }
}
