using UnityEngine;

namespace GMTK {
  [System.Serializable]

  /// <summary>
  /// Configuration data for a level in the game.
  /// </summary>
  public class LevelConfig {
    //"Basic Info"
    public string ConfigName;
    public string SceneName;

    //"Game State"
    [Tooltip("If true, the game state will be set to the InitialGameState when the scene is loaded.")]
    public bool SetStateOnLoad = true;
    [Tooltip("The game state to set when the scene is loaded, if SetStateOnLoad is true.")]
    public GameStates InitialGameState = GameStates.Preparation;

    //"Scene Management"
    [Tooltip("If true, this scene can be re-loaded. For example, the player can restart the level.")]
    public bool CanRestart = true;
    [Tooltip("If true, this scene can be skipped. For example, the player can skip a cutscene.")]
    public bool CanSkip = false;

    [Tooltip("If greater than 0, the scene will be loaded after a delay. This can be used to create a loading screen.")]
    public float LoadDelay = 0f;

    [Tooltip("If true, the level has a dedicated level complete scene to load when the level is finished.")]
    public bool HasLevelCompleteScene = true;
    [Tooltip("The name of the level complete scene to load when the level is finished. Default value is 'LevelComplete'")]
    public string LevelCompleteSceneName = "LevelComplete";

    // Custom Drawer properties
    [HideInInspector] public bool isExpanded = false;

    public LevelConfig() { }

    //Copy Constructor
    public LevelConfig(LevelConfig other) {
      if (other == null) return;
      SceneName = other.SceneName;
      ConfigName = other.ConfigName;
      SetStateOnLoad = other.SetStateOnLoad;
      InitialGameState = other.InitialGameState;
      CanRestart = other.CanRestart;
      CanSkip = other.CanSkip;
      LoadDelay = other.LoadDelay;
      HasLevelCompleteScene = other.HasLevelCompleteScene;
      LevelCompleteSceneName = other.LevelCompleteSceneName;
    }

    public static LevelConfig Clone(LevelConfig config) => new(config);
  }

}
