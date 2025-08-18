using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GMTK {
  public class GameplayStateHandler : GameStateHandler {

    [Header("Gameplay References")]
    public PlayableMarbleController marble;
    public LevelManager levelManager;

    protected List<GridSnappable> snappables = new();

    private void OnEnable() {
      Priority = 200;
      HandlerName = nameof(GameplayStateHandler);
    }

    public void Awake() {
      Priority = (Priority == 0) ? 200 : Priority;

      if (marble == null) {
        marble = FindFirstObjectByType<PlayableMarbleController>();
      }
      if (levelManager == null) {
        levelManager = FindFirstObjectByType<LevelManager>();
      }

      if (snappables == null || snappables.Count == 0) {
        snappables = FindObjectsByType<GridSnappable>(FindObjectsSortMode.None).ToList();
      }
    }

    protected override void ToPreparation() {
      marble.Spawn();
      marble.Model.SetActive(true);
      EnableSnappableDragging(true);
    }

    protected override void ToPlaying() {
      marble.Model.SetActive(true);
      marble.Launch();
      EnableSnappableDragging(false);
      levelManager.StartLevel();
    }

    protected override void ToReset() {
      marble.Spawn();
      levelManager.ResetLevel();
      ResetAllSnappables();
    }

    private void EnableSnappableDragging(bool enabled) => snappables.ForEach(s => s.Draggable = enabled);

    private void ResetAllSnappables() => snappables.ForEach(s => s.ResetSnappable());
  }


}
